using System;
using System.IO;
using System.Linq;
using System.Net;
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
using Xunit;
using Delegate = RService.IO.Abstractions.Delegate;
using IServiceProvider = RService.IO.Abstractions.IServiceProvider;
using IRoutingFeature = Microsoft.AspNetCore.Routing.IRoutingFeature;

namespace RService.IO.Tests
{
    public class RServiceMiddlewareTests
    {
        private readonly IOptions<RServiceOptions> _options;

        public RServiceMiddlewareTests()
        {
            _options = BuildRServiceOptions(opt => { });
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
        public async void Invoke__SetsContextOfService()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var serviceMock = new Mock<IService>().SetupAllProperties();

            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                    .Returns(Task.FromResult(0));

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0), service: serviceMock.Object);
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, 
                serviceProvider: provider.Object, service: serviceMock.Object);

            await middleware.Invoke(context);

            serviceMock.Object.Context.Should().Be(context);
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
        public async void Invoke_CallsProviderIfActivatorFound()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                    .Returns(Task.FromResult(0));

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0));
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, serviceProvider: provider.Object);

            await middleware.Invoke(context);

            provider.Verify(x => x.Invoke(It.IsAny<HttpContext>()));
        }

        [Fact]
        public async void Invoke__ApiExceptionSetsStatusCodeAndBody()
        {
            const string expectedBody = "FizzBuzz";
            const HttpStatusCode expectedStatusCode = HttpStatusCode.BadRequest;

            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Throws(new ApiException(expectedBody, expectedStatusCode));

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0));
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, serviceProvider: provider.Object);
            var body = context.Response.Body;

            await middleware.Invoke(context);

            body.Position = 0L;
            using (var response = new StreamReader(body))
                response.ReadToEnd().Should().Be(expectedBody);
            context.Response.StatusCode.Should().Be((int)expectedStatusCode);
        }

        [Fact]
        public async void Invoke__ExceptionSetsStatusCode()
        {
            const HttpStatusCode expectedStatusCode = HttpStatusCode.InternalServerError;

            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Throws(new ApiException(string.Empty, expectedStatusCode));

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0));
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, serviceProvider: provider.Object);
            var body = context.Response.Body;

            await middleware.Invoke(context);

            body.Position = 0L;
            using (var response = new StreamReader(body))
                response.ReadToEnd().Should().BeEmpty();
            context.Response.StatusCode.Should().Be((int)expectedStatusCode);
        }

        [Fact]
        public async void Invoke__CallsGlobalExceptionHandlerOnException()
        {
            var hasHandledException = false;

            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Throws<Exception>();

            var exceptionFilter = new Mock<IExceptionFilter>().SetupAllProperties();
            exceptionFilter.Setup(x => x.OnException(It.IsAny<HttpContext>(), It.IsAny<Exception>()))
                .Callback(() => hasHandledException = true);

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0), globalExceptionFilter: exceptionFilter.Object);
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, serviceProvider: provider.Object);

            await middleware.Invoke(context);

            hasHandledException.Should().BeTrue();
        }

        [Fact]
        public void Invoke__DoesNotCallGlobalExceptionFilterIfDebugging()
        {
            var hasHandledException = false;

            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = new TestSink(
               TestSink.EnableWithTypeName<RServiceMiddleware>,
               TestSink.EnableWithTypeName<RServiceMiddleware>);

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Throws<Exception>();

            var exceptionFilter = new Mock<IExceptionFilter>().SetupAllProperties();
            exceptionFilter.Setup(x => x.OnException(It.IsAny<HttpContext>(), It.IsAny<Exception>()))
                .Callback(() => hasHandledException = true);

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0), globalExceptionFilter: exceptionFilter.Object);

            var options = BuildRServiceOptions(opts => { opts.EnableDebugging = true; });
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator,
                options: options, serviceProvider: provider.Object);

            Func<Task> act = async () =>
            {
                await middleware.Invoke(context);
            };

            act.ShouldThrow<Exception>();
            hasHandledException.Should().BeFalse();
        }

        [Fact]
        public void Invoke__ThrowsExceptionIfDebugging()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RServiceMiddleware>,
                TestSink.EnableWithTypeName<RServiceMiddleware>);

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Throws<Exception>();

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0));

            var options = BuildRServiceOptions(opts => { opts.EnableDebugging = true; });
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator,
                options: options, serviceProvider: provider.Object);

            Func<Task> act = async () =>
            {
                await middleware.Invoke(context);
            };

            act.ShouldThrow<Exception>();
        }

        [Fact]
        public void Invoke__ThrowsApiExceptionIfDebugging()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RServiceMiddleware>,
                TestSink.EnableWithTypeName<RServiceMiddleware>);

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Throws<ApiException>();

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0));

            var options = BuildRServiceOptions(opts => { opts.EnableDebugging = true; });
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator,
                options: options, serviceProvider: provider.Object);

            Func<Task> act = async () =>
            {
                await middleware.Invoke(context);
            };

            act.ShouldThrow<ApiException>();
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

        private static IOptions<RServiceOptions> BuildRServiceOptions(Action<RServiceOptions> options)
        {
            return new OptionsManager<RServiceOptions>(new[]
            {
                new ConfigureOptions<RServiceOptions>(options)
            });
        }

        private static HttpContext BuildContext(RouteData routeData, RequestDelegate handler = null, string method = "GET",
            IExceptionFilter globalExceptionFilter = null, IService service = null)
        {
            var context = new DefaultHttpContext();
            var ioc = new Mock<System.IServiceProvider>().SetupAllProperties();

            context.Features[typeof(IRoutingFeature)] = new RoutingFeature
            {
                RouteData = routeData,
                RouteHandler = handler ?? (c => Task.FromResult(0))
            };

            ioc.Setup(x => x.GetService(It.IsAny<Type>())).Returns((IService)null);
            if (globalExceptionFilter != null)
                ioc.Setup(x => x.GetService(typeof(IExceptionFilter))).Returns(globalExceptionFilter);
            if (service != null)
                ioc.Setup(x => x.GetService(service.GetType())).Returns(service);
            else
                ioc.Setup(x => x.GetService(typeof(IService))).Returns(new Mock<IService>().SetupAllProperties().Object);

            context.RequestServices = ioc.Object;
            context.Request.Method = method;
            context.Response.Body = new MemoryStream();

            return context;
        }

        private RServiceMiddleware BuildMiddleware(TestSink sink, string routePath = null, Delegate.Activator routeActivator = null,
            RequestDelegate handler = null, IOptions<RServiceOptions> options = null, IServiceProvider serviceProvider = null,
            IService service = null)
        {
            options = options ?? _options;

            var loggerFactory = new TestLoggerFactory(sink, true);
            var next = handler ?? (c => Task.FromResult<object>(null));
            var rservice = new RService(options);

            serviceProvider = serviceProvider ?? new Mock<IServiceProvider>()
                .SetupAllProperties()
                .Object;

            if (!routePath.IsNullOrEmpty() && routeActivator != null)
                rservice.Routes.Add(routePath, new ServiceDef
                {
                    ServiceMethod = routeActivator,
                    ServiceType = service?.GetType() ?? typeof(IService)
                });

            return new RServiceMiddleware(next, loggerFactory, rservice, serviceProvider, options);
        }
    }
}