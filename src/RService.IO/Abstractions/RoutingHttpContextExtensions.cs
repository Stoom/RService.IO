using System;
using Microsoft.AspNetCore.Http;
using RService.IO.Router;

namespace RService.IO.Abstractions
{
    public static class RoutingHttpContextExtensions
    {
        public static RequestDelegate GetRouteHandler(this HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var routingFeature = context.Features[typeof(IRoutingFeature)] as RoutingFeature;
            return routingFeature?.RouteHandler;
        }
    }
}