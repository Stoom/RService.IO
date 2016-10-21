using System;
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
            var route = (context.GetRouteData()?.Routers.Count >= 3) 
                ? (context.GetRouteData()?.Routers[1] as Route)?.RouteTemplate
                : null;
            var handler = context.GetRouteHandler();
            var serviceDef = _service.Routes.FirstOrDefault(x => x.Key.StartsWith(route, StringComparison.CurrentCultureIgnoreCase)).Value;

            if (handler == null || serviceDef.ServiceMethod == null)
            {
                _logger.RequestDidNotMatchServices();
                await _next.Invoke(context);
            }
            else
            {
                context.Features[typeof(IRServiceFeature)] = new RServiceFeature
                {
                    MethodActivator = serviceDef.ServiceMethod,
                    Service = context.RequestServices.GetService(serviceDef.ServiceType) as IService,
                    RequestDtoType = serviceDef.RequestDtoType,
                    ResponseDtoType = serviceDef.ResponseDtoType
                };

                await handler.Invoke(context);
            }
        }
    }
}