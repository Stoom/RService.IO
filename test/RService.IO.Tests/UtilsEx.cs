using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace RService.IO.Tests
{
    public static class UtilsEx
    {
        public static IEnumerable<Type> GetRegisteredMiddleware<TMiddleware>(this IApplicationBuilder builder)
        {
            return builder.GetRegisteredMiddlewares().Where(x => x == typeof(TMiddleware)).ToList();
        }
        public static IEnumerable<Type> GetRegisteredMiddlewares(this IApplicationBuilder builder)
        {
            var componentInfo = typeof(ApplicationBuilder).GetField("_components",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var components = componentInfo.GetValue(builder) as List<Func<RequestDelegate, RequestDelegate>>;
            return components.Select(x =>
            {
                var target = x.Target;
                var middlewareInfo = target.GetType().GetField("middleware");
                var middleware = middlewareInfo?.GetValue(target) as Type;

                return middleware;
            }).ToList();
        }
    }
}