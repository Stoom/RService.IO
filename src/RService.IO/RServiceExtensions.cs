using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RService.IO
{
    public static class RServiceExtensions
    {
        public static IApplicationBuilder UseRServiceIo(
            this IApplicationBuilder builder, 
            Action<IRouteBuilder> configureRoutes)
        {
            var service = builder.ApplicationServices.GetService<RService>();
            var options = builder.ApplicationServices.GetRequiredService<IOptions<RServiceOptions>>().Value;

            var routes = new RouteBuilder(builder);

            foreach (var route in service.Routes)
            {
                routes.MapRServiceIoRoute(route.Value, options.RouteHanlder);
            }

            configureRoutes(routes);

            builder.UseRouter(routes.Build());
            return builder.UseMiddleware<RServiceMiddleware>();
        }

        public static IRouteBuilder MapRServiceIoRoute(this IRouteBuilder builder, RouteAttribute route, RequestDelegate handler)
        {
            var path = route.Path.Substring(1);

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (route.Verbs)
            {
                case RestVerbs.Any:
                    builder.MapRoute(path, handler);
                    break;
                default:
                    var verbs = route.Verbs.ToString().ToUpper()
                        .Split(',').Select(x => x.Trim())
                        .ToList();
                    verbs.ForEach(verb => builder.MapVerb(verb, path, handler));
                    break;
            }

            return builder;
        }
    }
}