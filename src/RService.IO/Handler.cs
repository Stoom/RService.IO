using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

            var responseType = res?.GetType();
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
            using (var reader = new StreamReader(context.Request.Body))
            {
                reqBody = reader.ReadToEnd();
            }


            var method = typeof(NetJSON.NetJSON)
                .GetMethod("Deserialize", new[] { typeof(string) })
                .MakeGenericMethod(dtoType);
            var dtoCtor = dtoType.GetConstructors().First();

            var json = Expression.Parameter(typeof(string), "json");
            var reqDto = Expression.Variable(dtoType, "requestDto");
            var returnTarget = Expression.Label(dtoType);
            var returnExp = Expression.Return(returnTarget, reqDto, dtoType);
            var returnLable = Expression.Label(returnTarget,
                Expression.Convert(Expression.Constant(null), dtoType));
            var callExpressions = new List<Expression>
            {
                Expression.Assign(reqDto, Expression.Call(method, json)),
                Expression.IfThen(
                    Expression.Equal(reqDto, Expression.Constant(null)),
                    Expression.Assign(reqDto, Expression.New(dtoCtor))
                )

            };

            var dtoProps = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanWrite).ToDictionary(k => k.Name, v => v);
            var routeData = context.GetRouteData()?.Values;

            callExpressions.AddRange(
                from prop
                in context.Request.Query?.Keys.Intersect(dtoProps.Keys) ?? EmptyKeys
                let setterMethod = dtoProps[prop].GetSetMethod()
                let value = context.Request.Query[prop]
                select Expression.Call(reqDto, setterMethod,
                    Expression.Convert(Expression.Constant(value), typeof(string))));

            callExpressions.AddRange(
                from prop in routeData?.Keys.Intersect(dtoProps.Keys) ?? EmptyKeys
                let value = routeData?[prop]
                let setterMethod = dtoProps[prop].GetSetMethod()
                select Expression.Call(reqDto, setterMethod, Expression.Constant(value)));

            callExpressions.Add(returnExp);
            callExpressions.Add(returnLable);
            var call = Expression.Block(new[] { reqDto }, callExpressions);

            dynamic lambda = Expression.Lambda<Func<string, object>>(
                Expression.Convert(call, typeof(object)),
                json);

            var dto = lambda.Compile().DynamicInvoke(reqBody);

            return dto;
        }
    }
}