using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RService.IO.Abstractions;

namespace RService.IO
{
    /// <summary>
    /// The RServiceIO route handlers.
    /// </summary>
    public class Handler
    {
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
            if (dtoType != null)
            {
                using (var reader = new StreamReader(context.Request.Body))
                {
                    var dto = NetJSON.NetJSON.Deserialize(dtoType, reader.ReadToEnd());
                    args.Add(dto);
                }
            }

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

        protected static object HydrateRequestDto(HttpContext context, Type dtoType)
        {
            throw new NotImplementedException();
        }
    }
}