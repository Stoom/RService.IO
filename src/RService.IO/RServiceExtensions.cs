using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RService.IO
{
    public static class RServiceExtensions
    {
        public static IServiceCollection AddRServiceIo(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            return AddRServiceIo(services, options => {});
        }

        public static IServiceCollection AddRServiceIo(
            this IServiceCollection services, 
            Action<RouteOptions> routeOptions)
        {
            services.AddRouting(routeOptions);

            services.AddSingleton<RService>();

            return services;
        }

        public static IApplicationBuilder UseRServiceIo(
            this IApplicationBuilder builder, 
            Action<IRouteBuilder> configureRoutes)
        {
            var service = builder.ApplicationServices.GetService<RService>();

            var routes = new RouteBuilder(builder);

            foreach (var route in service.Routes)
            {
                routes.MapRServiceIoRoute(route.Value, service.RouteHanlder);
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