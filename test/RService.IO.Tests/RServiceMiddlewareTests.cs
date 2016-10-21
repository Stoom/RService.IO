using System;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Internal;
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
using Delegate = RService.IO.Abstractions.Delegate;
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
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator);

            await middleware.Invoke(context);

            context.Features.ToDictionary(x => x.Key, x => x.Value)
                .Should().ContainKey(typeof(IRServiceFeature))
                .WhichValue.Should().NotBeNull();
            (context.Features[typeof(IRServiceFeature)] as IRServiceFeature)?
                .MethodActivator.Should().Be(expectedFeature.Object.MethodActivator);
        }

        [Fact]
        public async void Invoke__InvokesNextIfRouteHanlderNotSet()
        {
            var hasNextInvoked = false;
            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var context = BuildContext(new RouteData());
            var middleware = BuildMiddleware(sink, handler: ctx =>
            {
                hasNextInvoked = true;
                return Task.FromResult(0);
            });

            await middleware.Invoke(context);

            hasNextInvoked.Should().BeTrue();
        }

        [Fact]
        public async void Invoke__InvokesNextIfActivatorNotSet()
        {
            var hasNextInvoked = false;
            var routePath = "/Foobar".Substring(1);
            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var context = BuildContext(new RouteData());
            var middleware = BuildMiddleware(sink, routePath, handler: ctx =>
            {
                hasNextInvoked = true;
                return Task.FromResult(0);
            });

            await middleware.Invoke(context);

            hasNextInvoked.Should().BeTrue();
        }

        [Fact]
        public async void Invoke__LogsWhenFeatureNotAdded()
        {
            const string expectedMessage = "Request did not match any services.";
            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var context = BuildContext(new RouteData());
            var middleware = BuildMiddleware(sink, handler: ctx => Task.FromResult(0));

            await middleware.Invoke(context);

            sink.Scopes.Should().BeEmpty();
            sink.Writes.Count.Should().Be(1);
            sink.Writes[0].State?.ToString().Should().Be(expectedMessage);
        }

        [Fact]
        public async void Invoke_CallsHandlerIfActivatorFound()
        {
            var hasHandlerInvoked = false;


            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx =>
            {
                hasHandlerInvoked = true;
                return Task.FromResult(0);
            });
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator);

            await middleware.Invoke(context);

            hasHandlerInvoked.Should().BeTrue();
        }

        private static RouteData BuildRouteData(string path)
        {
            var routeTarget = new Mock<IRouter>();
            var resolver = new Mock<IInlineConstraintResolver>();
            var route = new Route(routeTarget.Object, path, resolver.Object);
            var routeData = new RouteData();
            routeData.Routers.AddRange(new[] { null, route, null });

            return routeData;
        }

        private static HttpContext BuildContext(RouteData routeData, RequestDelegate handler = null, string method = "GET")
        {
            var context = new DefaultHttpContext();
            var ioc = new Mock<IServiceProvider>().SetupAllProperties();
            context.Features[typeof(IRoutingFeature)] = new RoutingFeature
            {
                RouteData = routeData,
                RouteHandler = handler ?? (c => Task.FromResult(0))
            };
            ioc.Setup(x => x.GetService(It.IsAny<Type>())).Returns((IService) null);
            context.RequestServices = ioc.Object;
            context.Request.Method = method;

            return context;
        }

        private RServiceMiddleware BuildMiddleware(TestSink sink, string routePath = null, Delegate.Activator routeActivator = null, RequestDelegate handler = null)
        {

            var loggerFactory = new TestLoggerFactory(sink, true);
            var next = handler ?? (c => Task.FromResult<object>(null));
            var service = new RService(_options);
            if (!routePath.IsNullOrEmpty() && routeActivator != null)
                service.Routes.Add(routePath, new ServiceDef { ServiceMethod = routeActivator });

            return new RServiceMiddleware(next, loggerFactory, service);
        }
    }
}