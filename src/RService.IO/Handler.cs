using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RService.IO.Abstractions;
using Delegate = RService.IO.Abstractions.Delegate;

namespace RService.IO
{
    /// <summary>
    /// The RServiceIO route handlers.
    /// </summary>
    public class Handler
    {
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> CachedDtoProps = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        private static readonly Dictionary<Type, Delegate.DtoCtor> CachedDtoCtors = new Dictionary<Type, Delegate.DtoCtor>();
        private static readonly Regex JsonNoQuotes = new Regex(@"(^[\d.]+)|(^[Tt][Rr][Uu][Ee])|(^[Ff][Aa][Ll][Ss][Ee])|(^[Nn][Uu][Ll]{2})", RegexOptions.Compiled);

        /// <summary>
        /// The default route handler that must be used with RService.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the request and response.</param>
        /// <returns>A <see cref="Task"/> for async calls.</returns>
        /// <remarks>
        /// Pre/post routing tasks can be expanded by extending 
        /// the <b>BeforeHandler</b> and <b>AfterHandler</b> methods.
        /// </remarks>
        public static Task ServiceHandler(HttpContext context)
        {
            var service = context.GetServiceInstance();
            var activator = context.GetServiceMethodActivator();
            var dtoReqType = context.GetRequestDtoType();
            var dtoResType = context.GetResponseDtoType();
            var args = new List<object>();

            var dto = HydrateRequestDto(context, dtoReqType);
            if (dto != null)
                args.Add(dto);

            var res = activator.Invoke(service, args.ToArray());

            if (ReferenceEquals(null, res))
                return context.Response.WriteAsync(string.Empty);
            if (res.IsSimple())
                return context.Response.WriteAsync(res.ToString());

            return WriteJsonResponse(context, dtoResType, res);
        }

        /// <summary>
        /// Hydrates a request DTO.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> including the request.</param>
        /// <param name="dtoType">The type of for the request DTO.</param>
        /// <returns>The populated request DTO.</returns>
        /// <remarks>
        /// Priority:
        /// 1) Route templated variables
        /// 2) Query string variables
        /// 3) Request JSON body
        /// </remarks>
        protected static object HydrateRequestDto(HttpContext context, Type dtoType)
        {
            if (dtoType == null)
                return null;

            if (context.Request.ContentType == null && context.Request.Body.Length == 0)
            {
                return GetDtoCtorDelegate(dtoType).Invoke(string.Empty);
            }

            if (context.Request.Body.Length > 0
                && (context.Request.ContentType == null
                || !context.Request.ContentType.Equals(HttpContentTypes.ApplicationJson,StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new NotImplementedException(
                    $"{context.Request.ContentType ?? "Missing content type"} is currently not supported.");
            }

            var reqBodyBuilder = new StringBuilder();
            using (var reader = new StreamReader(context.Request.Body))
            {
                var body = reader.ReadToEnd().Trim();
                body = body.Length > 0
                    ? body.Remove(body.Length - 1)
                    : "{";

                reqBodyBuilder.Append(body);

                if (!CachedDtoProps.ContainsKey(dtoType))
                    CachedDtoProps.Add(dtoType, dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => x.CanWrite).ToDictionary(k => k.Name, v => v));

                AddQueryStringParams(context, CachedDtoProps[dtoType], ref reqBodyBuilder);
                AddRouteParams(context, CachedDtoProps[dtoType], ref reqBodyBuilder);
                
                reqBodyBuilder.AppendLine("}");
            }

            return GetDtoCtorDelegate(dtoType).Invoke(reqBodyBuilder.ToString());
        }

        private static void AddQueryStringParams(HttpContext context, Dictionary<string, PropertyInfo> dtoProps, ref StringBuilder reqBodyBuilder)
        {
            var queryData = context.Request.Query;
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

        private static void AddRouteParams(HttpContext context, Dictionary<string, PropertyInfo> dtoProps, ref StringBuilder reqBodyBuilder)
        {
            var routeData = context.GetRouteData()?.Values;
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

        private static Task WriteJsonResponse(HttpContext context, Type dtoResType, object res)
        {
            // TODO: Possibly convert this to the generic serialize function if this takes too long
            var response = NetJSON.NetJSON.Serialize(dtoResType, res);
            context.Response.ContentType = HttpContentTypes.ApplicationJson;
            return context.Response.WriteAsync(response);
        }
    }
}