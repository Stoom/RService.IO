using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RService.IO.Tests;
using Xunit;
using StartupBase = RService.IO.StartupBase;

namespace Rservice.IO.Tests.Integration
{
    public class SetupBaseTests
    {
        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        private readonly TestServer _server;
        private readonly HttpClient _client;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        public SetupBaseTests()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
            _client = _server.CreateClient();
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
            var response = await _client.GetAsync(path);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedPath, responseString);
        }

        #region Classes for testing
        // ReSharper disable once ClassNeverInstantiated.Local
        private class Startup : StartupBase
        {
            public Startup() : base(GetAsmFromType(typeof(SvcWithMethodRoute)))
            {
                RouteHanlder = context =>
                {
                    var route = context.GetRouteData().Routers[1] as RouteBase;
                    var body = route?.ParsedTemplate.TemplateText ?? "";
                    return context.Response.WriteAsync(body);
                };
            }
        }

        private static Assembly GetAsmFromType(Type type)
        {
            return type.GetTypeInfo().Assembly;
        }
        #endregion
    }
}
