using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;
using NuGet.Packaging;
using RService.IO.Abstractions;
using RService.IO.Router;
using Xunit;
using IRoutingFeature = Microsoft.AspNetCore.Routing.IRoutingFeature;

namespace RService.IO.Tests
{
    public class RServiceMiddlewareTests
    {
        private readonly IOptions<RServiceOptions> _options;

        public RServiceMiddlewareTests()
        {
            _options = new OptionsManager<RServiceOptions>(new[]
            {
                new ConfigureOptions<RServiceOptions>(opt => { })
            });
        }

        [Fact]
        public async void Invoke__AddsRServiceFeatureIfRouteHandlerSet()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData);
            var middleware = BuildMiddleware(sink, routePath, routeActivator);

            await middleware.Invoke(context);

            context.Features.ToDictionary(x => x.Key, x => x.Value)
                .Should().ContainKey(typeof(IRServiceFeature))
                .WhichValue.Should().NotBeNull();
            (context.Features[typeof(IRServiceFeature)] as IRServiceFeature)?
                .MethodActivator.Should().Be(expectedFeature.Object.MethodActivator);
        }

        private RouteData BuildRouteData(string path)
        {
            var routeTarget = new Mock<IRouter>();
            var resolver = new Mock<IInlineConstraintResolver>();
            var route = new Route(routeTarget.Object, path, resolver.Object);
            var routeData = new RouteData();
            routeData.Routers.AddRange(new[] { null, route, null });

            return routeData;
        }

        private HttpContext BuildContext(RouteData routeData)
        {
            var context = new DefaultHttpContext();
            context.Features[typeof(IRoutingFeature)] = new RoutingFeature
            {
                RouteData = routeData,
                RouteHandler = c => Task.FromResult(0)
            };

            return context;
        }

        private RServiceMiddleware BuildMiddleware(TestSink sink, string routePath, Delegate.Activator routeActivator)
        {

            var loggerFactory = new TestLoggerFactory(sink, true);
            RequestDelegate next = (c) => Task.FromResult<object>(null);
            var service = new RService(_options);
            service.Routes.Add(routePath, new ServiceDef { ServiceMethod = routeActivator });

            return new RServiceMiddleware(next, loggerFactory, service);
        }
    }
}