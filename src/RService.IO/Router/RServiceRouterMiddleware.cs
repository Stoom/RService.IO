using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using RService.IO.Abstractions;

namespace RService.IO.Router
{
    public class RServiceRouterMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly IRouter _router;

        public RServiceRouterMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IRouter router)
        {
            _next = next;
            _router = router;

            _logger = loggerFactory.CreateLogger<RServiceRouterMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var context = new RouteContext(httpContext);
            context.RouteData.Routers.Add(_router);

            await _router.RouteAsync(context);

            if (context.Handler == null)
            {
                _logger.RequestDidNotMatchRoutes();
                await _next.Invoke(httpContext);
            }
            else
            {
                httpContext.Features[typeof(IRoutingFeature)] = new RoutingFeature()
                {
                    RouteData = context.RouteData,
                    RouteHandler = context.Handler
                };

                if (context.Handler == Handler.ServiceHandler)
                    await _next.Invoke(httpContext);
                else
                    await context.Handler(context.HttpContext);
            }
        }
    }
}