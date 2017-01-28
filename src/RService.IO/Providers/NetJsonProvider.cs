using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RService.IO.Abstractions;
using Delegate = RService.IO.Abstractions.Delegate;

namespace RService.IO.Providers
{
    /// <summary>
    /// Adapter for the NetJson serialization.
    /// </summary>
    public class NetJsonProvider : ISerializationProvider
    {
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> CachedDtoProps = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        private static readonly Dictionary<Type, Delegate.DtoCtor> CachedDtoCtors = new Dictionary<Type, Delegate.DtoCtor>();
        private static readonly Regex JsonNoQuotes = new Regex(@"(^-?\d+[.\d]*)|(^true)|(^false)|(^null)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <inheritdoc/>
        public string ContentType { get; } = HttpContentTypes.ApplicationJson;

        /// <inheritdoc/>
        /// <remarks>
        /// Priority:
        /// 1) Route templated variables
        /// 2) Query string variables
        /// 3) Request JSON body
        /// </remarks>
        public object HydrateRequest(HttpContext ctx, Type dtoType)
        {
            var req = ctx.Request;

            if (dtoType == null)
                return null;

            string body;
            using (var reader = new StreamReader(req.Body))
            {
                body = reader.ReadToEnd().Trim();
            }

            if (body.Length > 0
                && (req.ContentType == null
                || !req.ContentType.Equals(ContentType, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new NotImplementedException(
                    $"{req.ContentType ?? "Missing content type"} is currently not supported.");
            }

            var reqBodyBuilder = new StringBuilder((body.Length > 0) ? body.Remove(body.Length - 1) : "{");

            if (!CachedDtoProps.ContainsKey(dtoType))
            {
                CachedDtoProps.Add(dtoType, dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => x.CanWrite).ToDictionary(k => k.Name, v => v));
            }

            AddQueryStringParams(req, CachedDtoProps[dtoType], ref reqBodyBuilder);
            AddRouteParams(ctx, CachedDtoProps[dtoType], ref reqBodyBuilder);

            reqBodyBuilder.AppendLine("}");

            return GetDtoCtorDelegate(dtoType).Invoke(reqBodyBuilder.ToString());
        }

        /// <inheritdoc/>
        public string DehydrateResponse(object resDto)
        {
            return NetJSON.NetJSON.Serialize(resDto.GetType(), resDto);
        }

        private static void AddQueryStringParams(HttpRequest req, Dictionary<string, PropertyInfo> dtoProps, ref StringBuilder reqBodyBuilder)
        {
            var queryData = req.Query;
            if (queryData == null)
                return;

            foreach (var key in queryData.Keys.Intersect(dtoProps.Keys))
            {
                var seperator = (reqBodyBuilder.Length > 1) ? "," : string.Empty;

                var value = queryData[key];
                reqBodyBuilder.AppendLine(!JsonNoQuotes.IsMatch(value)
                    ? $"{seperator}\"{key}\": \"{value}\""
                    : $"{seperator}\"{key}\": {value}");
            }
        }

        private static void AddRouteParams(HttpContext ctx, Dictionary<string, PropertyInfo> dtoProps, ref StringBuilder reqBodyBuilder)
        {
            var routeData = ctx.GetRouteData()?.Values;
            if (routeData == null)
                return;

            foreach (var key in routeData.Keys.Intersect(dtoProps.Keys))
            {
                var seperator = (reqBodyBuilder.Length > 1) ? "," : string.Empty;

                var value = routeData[key];
                var valueType = value.GetType();
                if (valueType == typeof(string))
                {
                    reqBodyBuilder.AppendLine($"{seperator}\"{key}\": \"{value}\"");
                }
                else if (valueType == typeof(decimal) || valueType == typeof(float) || valueType == typeof(double) ||
                         valueType == typeof(int) || valueType == typeof(long) || valueType == typeof(bool))
                {
                    reqBodyBuilder.AppendLine($"{seperator}\"{key}\": {value}");
                }
            }
        }

        private static Delegate.DtoCtor GetDtoCtorDelegate(Type dtoType)
        {
            if (!CachedDtoCtors.ContainsKey(dtoType))
                CachedDtoCtors.Add(dtoType, DelegateFactory.GenerateDtoCtor(dtoType));

            return CachedDtoCtors[dtoType];
        }
    }
}