using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using RService.IO.Abstractions;

namespace RService.IO
{
    public class RServiceMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly RService _service;

        public RServiceMiddleware(RequestDelegate next, ILoggerFactory logFactory, RService service)
        {
            _next = next;
            _logger = logFactory.CreateLogger<RServiceMiddleware>();
            _service = service;
        }

        public async Task Invoke(HttpContext context)
        {
            var route = (context.GetRouteData()?.Routers[1] as Route)?.RouteTemplate;
            var handler = context.GetRouteHandler();
            var activator = _service.Routes.FirstOrDefault(x => x.Key == route).Value.ServiceMethod;

            if (handler == null || activator == null)
            {
                await _next.Invoke(context);
            }
            else
            {
                context.Features[typeof(IRServiceFeature)] = new RServiceFeature
                {
                    MethodActivator = activator
                };
            }
        }
    }
}