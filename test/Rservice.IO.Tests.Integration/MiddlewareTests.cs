using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RService.IO;
using RService.IO.Abstractions.Providers;
using RService.IO.Authorization.DependencyInjection;
using RService.IO.Tests;
using Xunit;
using RService.IO.DependencyIngection;

namespace Rservice.IO.Tests.Integration
{
    public class MiddlewareTests
    {
        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        private readonly TestServer _routeServer;
        private readonly HttpClient _routeClient;
        private readonly TestServer _rserviceServer;
        private readonly HttpClient _rserviceClient;
        private readonly TestServer _rserviceAuthServer;
        private readonly HttpClient _rserviceAuthClient;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        public MiddlewareTests()
        {
            _routeServer = new TestServer(new WebHostBuilder()
                .UseStartup<RouteTestStartup>());
            _routeClient = _routeServer.CreateClient();
            _rserviceServer = new TestServer(new WebHostBuilder()
                .UseStartup<RServiceStartup>());
            _rserviceClient = _rserviceServer.CreateClient();
            _rserviceAuthServer = new TestServer(new WebHostBuilder()
                .UseStartup<RServiceAuthStartup>());
            _rserviceAuthClient = _rserviceAuthServer.CreateClient();
        }

        [Theory]
        [InlineData(SvcWithMethodRoute.RoutePath)]
        [InlineData(SvcWithMultMethodRoutes.RoutePath1)]
        [InlineData(SvcWithMultMethodRoutes.RoutePath2)]
        [InlineData(SvcWithParamRoute.RoutePath)]
        [InlineData(SvcWithMultParamRoutes.RoutePath1)]
        [InlineData(SvcWithMultParamRoutes.RoutePath2)]
        public async Task Configure__AddsAllDiscoverdRoutes(string path)
        {
            var expectedPath = path.Substring(1);
            var response = await _routeClient.GetAsync(path);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            responseString.Should().Be(expectedPath);
        }

        [Fact]
        public async Task E2E__HandlesRequest()
        {
            var response = await _rserviceClient.GetAsync(SvcWithMethodRoute.RoutePath);
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async void E2E__HandlesAuthorizedRequest()
        {
            var response = await _rserviceAuthClient.GetAsync(SvcAuthRoutes.AuthorizedPath);
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async void E2E__HandlesUnauthorizedRequest()
        {
            var response = await _rserviceAuthClient.GetAsync(SvcAuthRoutes.UnauthorizedPath);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #region Classes for testing
        // ReSharper disable ClassNeverInstantiated.Local
        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedParameter.Local
        private class ServiceProvider : IServiceProvider
        {
            public Task Invoke(HttpContext context)
            {
                var route = context.GetRouteData();
                return context.Response.WriteAsync(route.Routers[1].ToString());
            }
        }

        private class RouteTestStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddTransient<IServiceProvider, ServiceProvider>();
                services.AddRServiceIo(options =>
                {
                    options.AddServiceAssembly(typeof(SvcWithMethodRoute));
                });
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
            {
                app.UseRServiceIo(builder => { });
            }
        }

        private class RServiceStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddRServiceIo(options =>
                {
                    options.AddServiceAssembly(typeof(SvcWithMethodRoute));
                });
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
            {
                app.UseRServiceIo();
            }
        }

        private class RServiceAuthStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddRServiceIo(options =>
                {
                    options.AddServiceAssembly(typeof(SvcWithMethodRoute));
                });

                var authorizationPolicyProviderMock = new Mock<IAuthorizationPolicyProvider>().SetupAllProperties();


                // ReSharper disable once RedundantTypeArgumentsOfMethod
                services.AddTransient<IAuthorizationPolicyProvider>(_ => authorizationPolicyProviderMock.Object )
                    .AddAuthorization()
                    .AddRServiceIoAuthorization();
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
            {
                app.Use(async (ctx, next) =>
                {
                    var user = new ClaimsPrincipal(
                        new ClaimsIdentity(new[]
                            {
                                new Claim("Permission", "CanViewPage"),
                                new Claim(ClaimTypes.Role, "Administrator"),
                                new Claim(ClaimTypes.Role, "User"),
                                new Claim(ClaimTypes.NameIdentifier, "John")
                            }));

                    ctx.User = user;

                    await next.Invoke();
                });

                app.UseRServiceIo();
            }
        }
        // ReSharper restore UnusedParameter.Local
        // ReSharper restore UnusedMember.Local
        // ReSharper restore ClassNeverInstantiated.Local
        #endregion
    }
}
