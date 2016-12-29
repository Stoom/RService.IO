using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RService.IO.Abstractions;
using IServiceProvider = RService.IO.Abstractions.IServiceProvider;

namespace RService.IO
{
    public class RServiceMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly RService _service;
        private readonly IServiceProvider _serviceProvider;
        private readonly RServiceOptions _options;

        public RServiceMiddleware(RequestDelegate next, ILoggerFactory logFactory, RService service, IServiceProvider serviceProvider, IOptions<RServiceOptions> options)
        {
            _next = next;
            _logger = logFactory.CreateLogger<RServiceMiddleware>();
            _service = service;
            _serviceProvider = serviceProvider;
            _options = options?.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            var exceptionFilter = context.RequestServices.GetService<IExceptionFilter>();
            
            var routeData = (context.GetRouteData()?.Routers.Count >= 3)
                ? (context.GetRouteData()?.Routers[1] as Route)
                : null;
            var route = $"{routeData?.RouteTemplate}:{context.Request.Method}";

            ServiceDef serviceDef;
            _service.Routes.TryGetValue(route, out serviceDef);

            if (serviceDef.ServiceMethod == null)
            {
                _logger.RequestDidNotMatchServices();
                await _next.Invoke(context);
            }
            else
            {
                context.Features[typeof(IRServiceFeature)] = new RServiceFeature
                {
                    Metadata = serviceDef.Metadata,
                    MethodActivator = serviceDef.ServiceMethod,
                    Service = context.RequestServices.GetService(serviceDef.ServiceType) as IService,
                    RequestDtoType = serviceDef.RequestDtoType,
                    ResponseDtoType = serviceDef.ResponseDtoType
                };

                try
                {
                    await _serviceProvider.Invoke(context);
                }
                catch (Exception exc)
                {
                    if (_options?.EnableDebugging ?? false)
                        throw;

                    context.Response.Clear();
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    // ReSharper disable once CanBeReplacedWithTryCastAndCheckForNull
                    if (exc is ApiExceptions)
                    {
                        context.Response.StatusCode = (int) ((ApiExceptions) exc).StatusCode;
                        await context.Response.WriteAsync(exc.Message);
                    }

                    exceptionFilter?.OnException(context, exc);
                }
            }
        }
    }
}