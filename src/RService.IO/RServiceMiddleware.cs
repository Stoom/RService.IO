using System;
using System.Net;
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
            var routeData = (context.GetRouteData()?.Routers.Count >= 3)
                ? (context.GetRouteData()?.Routers[1] as Route)
                : null;
            var route = $"{routeData?.RouteTemplate}:{context.Request.Method}";

            var handler = context.GetRouteHandler();
            ServiceDef serviceDef;
            _service.Routes.TryGetValue(route, out serviceDef);

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

                try
                {
                    await handler.Invoke(context);
                }
                catch (ApiExceptions exc)
                {
                    if (_service.IsDebugEnabled)
                        throw;

                    context.Response.Clear();
                    context.Response.StatusCode = (int) exc.StatusCode;
                    await context.Response.WriteAsync(exc.Message);
                }
                catch (Exception exc)
                {
                    if (_service.IsDebugEnabled)
                        throw;

                    context.Response.Clear();
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
            }
        }
    }
}