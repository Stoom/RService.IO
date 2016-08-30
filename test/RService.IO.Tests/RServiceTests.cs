using System;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RService.IO.Abstractions;
using Xunit;

namespace RService.IO.Tests
{
    public class RServiceTests
    {
        private Assembly CurrentAssembly => GetType().GetTypeInfo().Assembly;
        private readonly IOptions<RServiceOptions> _options;

        public RServiceTests()
        {
            _options = new OptionsManager<RServiceOptions>(new[]
            {
                new ConfigureOptions<RServiceOptions>(opt =>
                {
                    opt.ServiceAssemblies.Add(CurrentAssembly);
                })
            });
        }

        [Fact]
        public void Ctor__ScansAssembliesForRoutesOnMethods()
        {
            var route = new RouteAttribute(SvcWithMethodRoute.RoutePath);

            var service = new RService(_options);

            service.Routes.Keys.Should().Contain(route.Path);
            service.Routes[route.Path].Route.Should().Be(route);
            service.Routes[route.Path].ServiceType.Should().Be(typeof(SvcWithMethodRoute));
            service.Routes[route.Path].ServiceMethod.Should().NotBeNull();
        }

        [Fact]
        public void Ctor__ScansAssembliesForRoutesOnMethodsFirstParam()
        {
            var route = new RouteAttribute(SvcWithParamRoute.RoutePath);

            var service = new RService(_options);

            service.Routes.Keys.Should().Contain(route.Path);
            service.Routes[route.Path].Route.Should().Be(route);
            service.Routes[route.Path].ServiceType.Should().Be(typeof(SvcWithParamRoute));
            service.Routes[route.Path].ServiceMethod.Should().NotBeNull();
        }

        [Fact]
        public void Ctor__ScansAssembliesForMultipleRoutesOnMethods()
        {
            var route1 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath1);
            var route2 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath2);

            var service = new RService(_options);

            service.Routes.Keys.Should().Contain(route1.Path);
            service.Routes[route1.Path].Route.Should().Be(route1);
            service.Routes[route1.Path].ServiceType.Should().Be(typeof(SvcWithMultMethodRoutes));
            service.Routes[route1.Path].ServiceMethod.Should().NotBeNull();
            service.Routes.Keys.Should().Contain(route2.Path);
            service.Routes[route2.Path].Route.Should().Be(route2);
            service.Routes[route2.Path].ServiceType.Should().Be(typeof(SvcWithMultMethodRoutes));
            service.Routes[route2.Path].ServiceMethod.Should().NotBeNull();
        }

        [Fact]
        public void Ctor__ScansAssembliesForMultipleRoutesOfParams()
        {
            var route1 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath1);
            var route2 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath2);

            var service = new RService(_options);


            service.Routes.Keys.Should().Contain(route1.Path);
            service.Routes[route1.Path].Route.Should().Be(route1);
            service.Routes[route1.Path].ServiceType.Should().Be(typeof(SvcWithMultParamRoutes));
            service.Routes[route1.Path].ServiceMethod.Should().NotBeNull();
            service.Routes.Keys.Should().Contain(route2.Path);
            service.Routes[route2.Path].Route.Should().Be(route2);
            service.Routes[route2.Path].ServiceType.Should().Be(typeof(SvcWithMultParamRoutes));
            service.Routes[route2.Path].ServiceMethod.Should().NotBeNull();
        }

        [Fact]
        public void Ctor__ScansAssembliesForSerivceTypes()
        {
            var expectedServiceType = typeof(SvcWithMethodRoute);

            var service = new RService(_options);

            service.ServiceTypes.Should().Contain(expectedServiceType);
        }

        [Fact]
        public void Ctor__ThrowsExceptionIfNullOptions()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action comparison = () => new RService(null);
            comparison.ShouldThrow<ArgumentNullException>();
        }
    }
}