using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Routing;
using RService.IO.Abstractions;

namespace RService.IO
{
    /// <summary>
    /// The RServiceIO route handlers.
    /// </summary>
    public class Handler
    {
        private static readonly QueryCollection EmptyQuery = new QueryCollection();
        private static readonly RouteValueDictionary EmptyRouteValues = new RouteValueDictionary();
        private static readonly List<string> EmptyKeys = new List<string>();

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
            var dtoType = context.GetRequestDtoType();
            var args = new List<object>();

            // TODO: This needs to be converted into lambda
            dynamic dto = HydrateRequestDto(context, dtoType);
            if (dto != null)
                args.Add(dto);

            var res = activator.Invoke(service, args.ToArray());
            if (ReferenceEquals(null, res))
                return context.Response.WriteAsync(string.Empty);

            var responseType = res.GetType();
            var response = Convert.ChangeType(res, responseType);

            if (response.IsSimple())
                return context.Response.WriteAsync(response.ToString());



            throw new NotImplementedException();
            // TODO: Possible make this an expression tree delegate?  Should have a standard pattern
            // And wouldn't need to do dynamic things.
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

            string reqBody;
            var reqBodyBuilder = new StringBuilder();
            using (var reader = new StreamReader(context.Request.Body))
            {
                var body = reader.ReadToEnd().Trim();
                body = body.Length > 0
                    ? body.Remove(body.Length - 1)
                    : "{";

                reqBodyBuilder.Append(body);

                var routeData = context.GetRouteData()?.Values;
                if (routeData != null)
                {
                    var dtoProps = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(x => x.CanWrite).ToDictionary(k => k.Name, v => v);
                    foreach (var key in routeData.Keys.Intersect(dtoProps.Keys))
                    {
                        var seperator = (reqBodyBuilder.Length > 1) ? "," : String.Empty;

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
                reqBodyBuilder.AppendLine("}");
                reqBody = reqBodyBuilder.ToString();
            }

            // Methods
            var deserializeMethod = typeof(NetJSON.NetJSON)
                .GetMethod("Deserialize", new[] { typeof(string) })
                .MakeGenericMethod(dtoType);
            var dtoCtor = dtoType.GetConstructors().First();

            // Properties and fields
            var jsonParam = Expression.Parameter(typeof(string), "Json Body");
            var reqDtoVar = Expression.Variable(dtoType, "Request Dto");

            // Return
            var returnTarget = Expression.Label(dtoType);
            var returnExp = Expression.Return(returnTarget, reqDtoVar, dtoType);
            var returnLable = Expression.Label(returnTarget,
                Expression.Convert(Expression.Constant(null), dtoType));

            // Constants
            var nullConst = Expression.Constant(null);

            var callExpressions = new List<Expression>
            {
                // Deserialize or ctor
                Expression.Assign(reqDtoVar, Expression.Call(deserializeMethod, jsonParam)),
                Expression.IfThen(
                    Expression.Equal(reqDtoVar, nullConst),
                    Expression.Assign(reqDtoVar, Expression.New(dtoCtor))
                ),
                returnExp,
                returnLable
            };

            //callExpressions.AddRange(
            //    from prop
            //    in context.Request.Query?.Keys.Intersect(dtoProps.Keys) ?? EmptyKeys
            //    let setterMethod = dtoProps[prop].GetSetMethod()
            //    let value = context.Request.Query[prop]
            //    select Expression.Call(reqDtoVar, setterMethod,
            //        Expression.Convert(Expression.Constant(value), typeof(string))));

            var call = Expression.Block(new[] { reqDtoVar }, callExpressions);

            dynamic lambda = Expression.Lambda<Func<string, object>>(
                Expression.Convert(call, typeof(object)),
                jsonParam);

            var dto = lambda.Compile().DynamicInvoke(reqBody);

            return dto;
        }
    }
}