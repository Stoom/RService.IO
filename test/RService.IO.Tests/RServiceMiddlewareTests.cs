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
using Microsoft.Extensions.Primitives;
using Moq;
using RService.IO.Abstractions;
using RService.IO.Abstractions.Providers;
using RService.IO.Providers;
using Xunit;
using Delegate = RService.IO.Abstractions.Delegate;
using IServiceProvider = RService.IO.Abstractions.Providers.IServiceProvider;
using IRoutingFeature = Microsoft.AspNetCore.Routing.IRoutingFeature;
using RoutingFeature = RService.IO.Abstractions.RoutingFeature;

namespace RService.IO.Tests
{
    public class RServiceMiddlewareTests
    {
        private readonly IOptions<RServiceOptions> _options;

        public RServiceMiddlewareTests()
        {
            _options = BuildRServiceOptions(opt =>
            {
                opt.DefaultSerializationProvider = new NetJsonProvider();
            });
        }

        [Fact]
        public async void Invoke__AddsRServiceFeatureIfRouteHandlerSet()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = GetTestSink();

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

            var sink = GetTestSink();

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
            var sink = GetTestSink();

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
            var sink = GetTestSink();

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
            var sink = GetTestSink();

            var context = BuildContext(new RouteData());
            var middleware = BuildMiddleware(sink, handler: ctx => Task.FromResult(0));

            await middleware.Invoke(context);

            sink.Scopes.Should().BeEmpty();
            sink.Writes.Count.Should().Be(1);
            sink.Writes[0].State?.ToString().Should().Be(expectedMessage);
        }

        [Fact]
        public async void Invoke__CallsProviderIfActivatorFound()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = GetTestSink();

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

            var sink = GetTestSink();

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

            var sink = GetTestSink();

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

            var sink = GetTestSink();

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

            var sink = GetTestSink();

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

            var sink = GetTestSink();

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

            var sink = GetTestSink();

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

        [Fact]
        public async void Invoke__AcceptHeaderBlankReturnsOk()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = GetTestSink();

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Returns(Task.FromResult(0));

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0), accept: string.Empty);
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, serviceProvider: provider.Object);

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
        }

        [Fact]
        public async void Invoke__AcceptHeaderAnyWithQualityReturnsOk()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = GetTestSink();

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Returns(Task.FromResult(0));

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0), accept: "*/*;q=0.8");
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, serviceProvider: provider.Object);

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
        }

        [Fact]
        public async void Invoke__AcceptHeaderWithQualityReturnsOk()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = GetTestSink();

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Returns(Task.FromResult(0));

            var routeData = BuildRouteData(routePath);
            var options = BuildRServiceOptions(opt =>
            {
                var json = new NetJsonProvider();
                opt.DefaultSerializationProvider = json;
                opt.SerializationProviders.Add(json.ContentType, json);
            });
            var context = BuildContext(routeData, ctx => Task.FromResult(0),
                accept: $"{new NetJsonProvider().ContentType};q=0.8");
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, serviceProvider: provider.Object,
                options: options);

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
        }

        [Fact]
        public async void Invoke__AcceptHeaderWithMultipleMimeTypesReturnsOk()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = GetTestSink();

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Returns(Task.FromResult(0));

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0),
                accept: $"application/json?q=1,*/*?q=0.8");
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, serviceProvider: provider.Object);

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
        }

        [Fact]
        public async void Invoke__AcceptHeaderNotMachingSerialzaitonProviderReturnsNotAcceptable()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = GetTestSink();

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Returns(Task.FromResult(0));

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0), accept: "text/foobar");
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, serviceProvider: provider.Object);

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.NotAcceptable);
        }

        [Fact]
        public async void Invoke__AcceptHeaderAnyUsesDefaultProvider()
        {
            var routePath = "/Foobar".Substring(1);
            const string expectedResponse = "FizzBuzz";
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var mockProvider = new Mock<ISerializationProvider>().SetupAllProperties();
            _options.Value.DefaultSerializationProvider = mockProvider.Object;
            mockProvider.Setup(x => x.DehydrateResponse(It.IsAny<object>())).Returns(expectedResponse);

            var sink = GetTestSink();

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            var responseMessage = string.Empty;
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    var serializer = ctx.GetResponseSerializationProvider();
                    responseMessage = serializer.DehydrateResponse(null);
                })
                .Returns(Task.FromResult(0));

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0), accept: HttpContentTypes.Any);
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, serviceProvider: provider.Object);

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
            responseMessage.ShouldAllBeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async void Invoke__AcceptHeaderSecondaryUsesSecondaryProvider()
        {
            var routePath = "/Foobar".Substring(1);
            const string expectedResponse = "FizzBuzz";
            const string mimeType = "application/foobar";
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var mockProvider = new Mock<ISerializationProvider>().SetupAllProperties();
            _options.Value.DefaultSerializationProvider = mockProvider.Object;
            mockProvider.Setup(x => x.DehydrateResponse(It.IsAny<object>())).Returns(expectedResponse);

            var sink = GetTestSink();

            var provider = new Mock<IServiceProvider>()
                .SetupAllProperties();
            var responseMessage = string.Empty;
            provider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx =>
                {
                    var serializer = ctx.GetResponseSerializationProvider();
                    responseMessage = serializer.DehydrateResponse(null);
                })
                .Returns(Task.FromResult(0));

            var foobarProvider = new Mock<ISerializationProvider>().SetupAllProperties();
            foobarProvider.SetupGet(x => x.ContentType).Returns(mimeType);
            foobarProvider.Setup(x => x.DehydrateResponse(It.IsAny<object>())).Returns(expectedResponse);

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0), accept: mimeType);
            var options = BuildRServiceOptions(opt =>
            {
                opt.DefaultSerializationProvider = new NetJsonProvider();
                opt.SerializationProviders.Add(mimeType, foobarProvider.Object);
            });
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator,
                serviceProvider: provider.Object, options: options);

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.OK);
            responseMessage.ShouldAllBeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async void Invoke__MissingContenTypeReturnsUnsupportedMediaTypeWhenBodyHasContent()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = GetTestSink();

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0), contentType: string.Empty);
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator);

            using (context.Request.Body = new MemoryStream())
            using (var sw = new StreamWriter(context.Request.Body))
            {
                sw.Write("Foobar");
                sw.Flush();
                context.Request.Body.Position = 0L;
                context.Request.ContentLength = context.Request.Body.Length;

                await middleware.Invoke(context);

                context.Response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.UnsupportedMediaType);
            }
        }

        [Fact]
        public async void Invoke__UnknownContenTypeReturnsUnsupportedMediaTypeWhenBodyHasContent()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedFeature = new Mock<IRServiceFeature>();
            expectedFeature.SetupGet(x => x.MethodActivator).Returns(routeActivator);

            var sink = GetTestSink();

            var routeData = BuildRouteData(routePath);
            var context = BuildContext(routeData, ctx => Task.FromResult(0), contentType: "text/foobar");
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator);

            using (context.Request.Body = new MemoryStream())
            using (var sw = new StreamWriter(context.Request.Body))
            {
                sw.Write("Foobar");
                sw.Flush();
                context.Request.Body.Position = 0L;
                context.Request.ContentLength = context.Request.Body.Length;

                await middleware.Invoke(context);

                context.Response.StatusCode.ShouldBeEquivalentTo(HttpStatusCode.UnsupportedMediaType);
            }
        }

        [Fact]
        public async Task Invoke__EmptyBodyContentSetsContentTypeToDefaultProvider()
        {
            var routePath = "/Foobar".Substring(1);
            Delegate.Activator routeActivator = (target, args) => null;
            var expectedSerializationProvider = new Mock<ISerializationProvider>().SetupAllProperties();
            expectedSerializationProvider.SetupGet(x => x.ContentType).Returns("text/foobar");

            var sink = GetTestSink();

            var routeData = BuildRouteData(routePath);
            var options = BuildRServiceOptions(opts =>
            {
                opts.DefaultSerializationProvider = expectedSerializationProvider.Object;
                opts.SerializationProviders.Add(opts.DefaultSerializationProvider.ContentType, opts.DefaultSerializationProvider);
            });
            var context = BuildContext(routeData, ctx => Task.FromResult(0), contentType: "text/foobar");
            var middleware = BuildMiddleware(sink, $"{routePath}:GET", routeActivator, options: options);

            await middleware.Invoke(context);

            context.Features[typeof(IRServiceFeature)].As<IRServiceFeature>().RequestSerializer.ContentType
                .ShouldAllBeEquivalentTo(expectedSerializationProvider.Object.ContentType);
        }

        private static TestSink GetTestSink()
        {
            return new TestSink(
                TestSink.EnableWithTypeName<RServiceMiddleware>,
                TestSink.EnableWithTypeName<RServiceMiddleware>);
        }

        private static RouteData BuildRouteData(string path)
        {
            var routeTarget = new Mock<IRouter>();
            var resolver = new Mock<IInlineConstraintResolver>();
            var route = new Route(routeTarget.Object, path, resolver.Object);
            var routeData = new RouteData();
            routeData.Routers.Add(null);
            routeData.Routers.Add(route);
            routeData.Routers.Add(null);

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
            IExceptionFilter globalExceptionFilter = null, IService service = null, string accept = null, string contentType = null)
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
            context.Request.Headers.Add("Accept", new StringValues(accept ?? "*/*"));
            context.Request.ContentType = contentType ?? context.Request.ContentType;
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

            if (serviceProvider == null)
            {
                var mockServiceProvider = new Mock<IServiceProvider>().SetupAllProperties();
                mockServiceProvider.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
                    .Returns(Task.FromResult(0));

                serviceProvider = mockServiceProvider.Object;
            }

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