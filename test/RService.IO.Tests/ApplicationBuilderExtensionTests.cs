using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RService.IO.DependencyIngection;
using Xunit;

namespace RService.IO.Tests
{
    public class ApplicationBuilderExtensionTests
    {
        private static readonly RequestDelegate EmptyHandler = context => Task.FromResult(0);
        private static readonly Action<RouteOptions> EmptyRouteOptions = opt => { }; 

        [Fact]
        public void UserRServiceIo__AddsRoutesToRouting()
        {
            IRouteBuilder routeBuilder = null;
            var route = new RouteAttribute("/Foobar");
            var def = new ServiceDef { Route = route };

            var services = new ServiceCollection();
            services.AddRServiceIo(sOps => { sOps.RouteHanlder = EmptyHandler; }, EmptyRouteOptions);

            var builder = BuildApplicationBuilder(services);
            var service = builder.ApplicationServices.GetService<RService>();
            service?.Routes.Add(route.Path, def);

            builder.UseRServiceIo(x =>
            {
                routeBuilder = x;
            });

            routeBuilder.Should().NotBeNull();
            var actualRoutes = GetRouteTemplates((RouteBuilder) routeBuilder)
                    .FirstOrDefault(x => x.RouteTemplate.Equals(route.Path.Substring(1)));
            actualRoutes.Should().NotBeNull();
        }

        [Fact]
        public void UserRServiceIo__AddsCustomeRoutesToRouting()
        {
            IRouteBuilder routeBuilder = null;
            const string expectedPath = "Llamas/Eat/Hands";

            var services = new ServiceCollection();
            services.AddRServiceIo(sOps => { sOps.RouteHanlder = EmptyHandler; }, EmptyRouteOptions);

            var builder = BuildApplicationBuilder(services);

            builder.UseRServiceIo(x =>
            {
                routeBuilder = x;

                x.MapRoute(expectedPath, EmptyHandler);
            });

            routeBuilder.Should().NotBeNull();
            var actualRoutes = GetRouteTemplates((RouteBuilder) routeBuilder)
                    .FirstOrDefault(x => x.RouteTemplate.Equals(expectedPath));
            actualRoutes.Should().NotBeNull();
        }

        private static IEnumerable<Route> GetRouteTemplates(IRouteBuilder builder)
        {
            return builder.Routes
                .Cast<Route>()
                .Where(r => r.Constraints.All(c => c.Key.Equals("httpMethod", StringComparison.CurrentCultureIgnoreCase)));
        }

        private static IApplicationBuilder BuildApplicationBuilder(IServiceCollection services)
        {
            var builder = new Mock<IApplicationBuilder>();
            builder.SetupAllProperties();
            builder.Object.ApplicationServices = services.BuildServiceProvider();

            return builder.Object;
        }
    }
}