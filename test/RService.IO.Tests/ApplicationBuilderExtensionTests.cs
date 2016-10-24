using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RService.IO.Abstractions;
using RService.IO.DependencyIngection;
using RService.IO.Router;
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

        [Fact]
        public void UserRServiceIo__DefaultsEmptyRouteConfig()
        {
            IRouteBuilder routeBuilder = null;

            var services = new ServiceCollection();
            services.AddRServiceIo(sOps => { sOps.RouteHanlder = EmptyHandler; }, EmptyRouteOptions);

            var builder = BuildApplicationBuilder(services);


            var emptyRouteConfig = typeof(ApplicationBuilderExtensions).GetField("EmptyRouteConfigure",
                             BindingFlags.Static |
                             BindingFlags.NonPublic);

            emptyRouteConfig.SetValue(null, new Action<IRouteBuilder>(x => routeBuilder = x ));

            builder.UseRServiceIo();

            routeBuilder.Should().NotBeNull();
        }

        [Fact]
        public void UserRServiceIo__EnablesUseDeveloperExceptionPageIfDebugging()
        {
            var services = new ServiceCollection();
            services.AddRServiceIo(opts =>
            {
                opts.RouteHanlder = EmptyHandler;
                opts.EnableDebugging = true;
            }, EmptyRouteOptions);

            var builder = new ApplicationBuilder(services.BuildServiceProvider());

            builder.UseRServiceIo();

            var middlewares = builder.GetRegisteredMiddleware<DeveloperExceptionPageMiddleware>();

            middlewares.Should().HaveCount(1);
        }

        [Fact]
        public void UserRServiceIo__DisablesUseDeveloperExceptionPageIfNotDebugging()
        {
            var services = new ServiceCollection();
            services.AddRServiceIo(opts =>
            {
                opts.RouteHanlder = EmptyHandler;
                opts.EnableDebugging = false;
            }, EmptyRouteOptions);

            var builder = new ApplicationBuilder(services.BuildServiceProvider());

            builder.UseRServiceIo();

            var middlewares = builder.GetRegisteredMiddleware<DeveloperExceptionPageMiddleware>();

            middlewares.Should().HaveCount(0);
        }

        [Fact]
        public void UserRServiceIo__EnablesRouting()
        {
            var services = new ServiceCollection();
            services.AddRServiceIo(opts =>
            {
                opts.RouteHanlder = EmptyHandler;
            }, EmptyRouteOptions);

            var builder = new ApplicationBuilder(services.BuildServiceProvider());

            builder.UseRServiceIo();

            var middlewares = builder.GetRegisteredMiddleware<RServiceRouterMiddleware>();

            middlewares.Should().HaveCount(1);
        }

        [Fact]
        public void UserRServiceIo__EnablesRService()
        {
            var services = new ServiceCollection();
            services.AddRServiceIo(opts =>
            {
                opts.RouteHanlder = EmptyHandler;
            }, EmptyRouteOptions);

            var builder = new ApplicationBuilder(services.BuildServiceProvider());

            builder.UseRServiceIo();

            var middlewares = builder.GetRegisteredMiddleware<RServiceMiddleware>();

            middlewares.Should().HaveCount(1);
        }

        [Fact]
        public void UseRServiceIo__ThrowsExceptionIfBuilderIsNull()
        {
            Action comparison = () => ApplicationBuilderExtensions.UseRServiceIo(null, null);

            comparison.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void UseRServiceIo__ThrowsExceptionIfRouteConfigIsNull()
        {
            var builder = BuildApplicationBuilder(new ServiceCollection());

            Action comparison = () => builder.UseRServiceIo(null);

            comparison.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void UseRServiceIo__ThrowsExceptionIfRoutingNotAdded()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton<RService>();
            Action<RServiceOptions> opts = x => x.RouteHanlder = EmptyHandler;
            services.Configure(opts);

            var builder = BuildApplicationBuilder(services);

            Action comparison = () => builder.UseRServiceIo();

            comparison.ShouldThrow<InvalidOperationException>();
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