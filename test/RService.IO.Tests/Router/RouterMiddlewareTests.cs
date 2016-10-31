using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using RService.IO.Router;
using Xunit;

namespace RService.IO.Tests.Router
{
    public class RouterMiddlewareTest
    {
        [Fact]
        public async void Invoke_LogsCorrectValuesWhenNotHandled()
        {
            // Arrange
            const string expectedMessage = "Request did not match any routes.";
            const bool isHandled = false;
            const bool isRService = false;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RServiceRouterMiddleware>,
                TestSink.EnableWithTypeName<RServiceRouterMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, true);

            var reqServices = new Mock<IServiceProvider>().SetupAllProperties().Object;
            var httpContext = new DefaultHttpContext {RequestServices = reqServices};

            RequestDelegate next = (c) => Task.FromResult<object>(null);

            var router = new TestRouter(isHandled, isRService);
            var middleware = new RServiceRouterMiddleware(next, loggerFactory, router);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.Empty(sink.Scopes);
            Assert.Single(sink.Writes);
            Assert.Equal(expectedMessage, sink.Writes[0].State?.ToString());
        }

        [Fact]
        public async void Invoke_DoesNotLogWhenHandled()
        {
            // Arrange
            const bool isHandled = true;
            const bool isRService = false;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RServiceRouterMiddleware>,
                TestSink.EnableWithTypeName<RServiceRouterMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, true);

            var reqServices = new Mock<IServiceProvider>().SetupAllProperties().Object;
            var httpContext = new DefaultHttpContext { RequestServices = reqServices };

            RequestDelegate next = (c) => Task.FromResult<object>(null);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var router = new TestRouter(isHandled, isRService);
            var middleware = new RServiceRouterMiddleware(next, loggerFactory, router);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.Empty(sink.Scopes);
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public async void Invoke__DoesNotInvokesNextIfNotRService()
        {
            // Arrange
            const bool isHandled = true;
            const bool isRService = false;
            var wasCalled = false;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RServiceRouterMiddleware>,
                TestSink.EnableWithTypeName<RServiceRouterMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, true);

            var reqServices = new Mock<IServiceProvider>().SetupAllProperties().Object;
            var httpContext = new DefaultHttpContext { RequestServices = reqServices };

            RequestDelegate next = (c) =>
            {
                wasCalled = true;
                return Task.FromResult<object>(null);
            };

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var router = new TestRouter(isHandled, isRService);
            var middleware = new RServiceRouterMiddleware(next, loggerFactory, router);

            // Act
            await middleware.Invoke(httpContext);

            Assert.False(wasCalled);
        }

        [Fact]
        public async void Invoke__InvokesNextIfRService()
        {
            // Arrange
            const bool isHandled = false;
            const bool isRService = true;
            var wasCalled = false;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RServiceRouterMiddleware>,
                TestSink.EnableWithTypeName<RServiceRouterMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, true);

            var reqServices = new Mock<IServiceProvider>().SetupAllProperties().Object;
            var httpContext = new DefaultHttpContext { RequestServices = reqServices };

            RequestDelegate next = (c) =>
            {
                wasCalled = true;
                return Task.FromResult<object>(null);
            };

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var router = new TestRouter(isHandled, isRService);
            var middleware = new RServiceRouterMiddleware(next, loggerFactory, router);

            // Act
            await middleware.Invoke(httpContext);

            Assert.True(wasCalled);
        }

        private class TestRouter : IRouter
        {
            private readonly bool _isHandled;
            private readonly bool _isRService;

            public TestRouter(bool isHandled, bool isRService)
            {
                _isHandled = isHandled;
                _isRService = isRService;
            }

            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                return new VirtualPathData(this, "");
            }

            public Task RouteAsync(RouteContext context)
            {
                if (_isHandled)
                    context.Handler = c => Task.FromResult(0);
                else if (_isRService)
                    context.Handler = RServiceTagHandler.Tag;
                else
                    context.Handler = null;
                return Task.FromResult<object>(null);
            }
        }
    }
}