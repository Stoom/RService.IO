using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RService.IO;
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
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        public MiddlewareTests()
        {
            _routeServer = new TestServer(new WebHostBuilder()
                .UseStartup<RouteTestStartup>());
            _routeClient = _routeServer.CreateClient();
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

        #region Classes for testing
        // ReSharper disable once ClassNeverInstantiated.Local
        private class RouteTestStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddRServiceIo(options =>
                {
                    options.AddServiceAssembly(typeof(SvcWithMethodRoute));
                    options.RouteHanlder = context =>
                    {
                        var route = context.GetRouteData();
                        return context.Response.WriteAsync(route.Routers[1].ToString());
                    };
                });
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
            {
                app.UseRServiceIo(builder => { });
            }

            private static Assembly GetAsmFromType(Type type)
            {
                return type.GetTypeInfo().Assembly;
            }
        }

        #endregion
    }
}
