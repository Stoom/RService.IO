using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RService.IO.Router;

namespace RService.IO
{
    public static class ApplicationBuilderExtensions
    {
        private static readonly Action<IRouteBuilder> EmptyRouteConfigure = builder => { };

        public static IApplicationBuilder UseRServiceIo(this IApplicationBuilder builder)
        {
            return builder.UseRServiceIo(EmptyRouteConfigure);
        }

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
            builder.RegisterRoutes(routes.Build());

            return builder.UseMiddleware<RServiceMiddleware>();
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static IApplicationBuilder RegisterRoutes(this IApplicationBuilder builder, IRouter router)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (router == null)
                throw new ArgumentNullException(nameof(router));
            if (builder.ApplicationServices.GetService(typeof(RoutingMarkerService)) == null)
                throw new InvalidOperationException($"Unable to find service {nameof(RoutingMarkerService)}");

            return builder.UseMiddleware<RServiceRouterMiddleware>(router);
        }
    }
}