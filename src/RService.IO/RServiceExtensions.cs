using System;
using Microsoft.AspNetCore.Builder;
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
    }
}