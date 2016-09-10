using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RService.IO.Abstractions;
using Xunit;

namespace RService.IO.Tests
{
    public class RServiceTests
    {
        private readonly IOptions<RServiceOptions> _options;

        public RServiceTests()
        {
            _options = new OptionsManager<RServiceOptions>(new[]
            {
                new ConfigureOptions<RServiceOptions>(opt =>
                {
                    opt.ServiceAssemblies.Add(Utils.Instance.CurrentAssembly);
                })
            });
        }

        [Fact]
        public void Ctor__ScansAssembliesForRoutesOnMethods()
        {
            var route = new RouteAttribute(SvcWithMethodRoute.RoutePath);
            var expectedPath = route.Path.Substring(1);

            var service = new RService(_options);

            service.Routes.Keys.Should().Contain(expectedPath);
            service.Routes[expectedPath].Route.Should().Be(route);
            service.Routes[expectedPath].ServiceType.Should().Be(typeof(SvcWithMethodRoute));
            service.Routes[expectedPath].ServiceMethod.Should().NotBeNull();
        }

        [Fact]
        public void Ctor__ScansAssembliesForRoutesOnMethodsFirstParam()
        {
            var route = new RouteAttribute(SvcWithParamRoute.RoutePath);
            var expectedPath = route.Path.Substring(1);

            var service = new RService(_options);

            service.Routes.Keys.Should().Contain(expectedPath);
            service.Routes[expectedPath].Route.Should().Be(route);
            service.Routes[expectedPath].ServiceType.Should().Be(typeof(SvcWithParamRoute));
            service.Routes[expectedPath].ServiceMethod.Should().NotBeNull();
            service.Routes[expectedPath].RequestDtoType.Should().Be(typeof(DtoForParamRoute));
        }

        [Fact]
        public void Ctor__ScansAssembliesForMultipleRoutesOnMethods()
        {
            var route1 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath1);
            var route2 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath2);
            var expectedPath1 = route1.Path.Substring(1);
            var expectedPath2 = route2.Path.Substring(1);

            var service = new RService(_options);

            service.Routes.Keys.Should().Contain(expectedPath1);
            service.Routes[expectedPath1].Route.Should().Be(route1);
            service.Routes[expectedPath1].ServiceType.Should().Be(typeof(SvcWithMultMethodRoutes));
            service.Routes[expectedPath1].ServiceMethod.Should().NotBeNull();
            service.Routes.Keys.Should().Contain(expectedPath2);
            service.Routes[expectedPath2].Route.Should().Be(route2);
            service.Routes[expectedPath2].ServiceType.Should().Be(typeof(SvcWithMultMethodRoutes));
            service.Routes[expectedPath2].ServiceMethod.Should().NotBeNull();
        }

        [Fact]
        public void Ctor__ScansAssembliesForMultipleRoutesOfParams()
        {
            var route1 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath1);
            var route2 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath2);
            var expectedPath1 = route1.Path.Substring(1);
            var expectedPath2 = route2.Path.Substring(1);

            var service = new RService(_options);


            service.Routes.Keys.Should().Contain(expectedPath1);
            service.Routes[expectedPath1].Route.Should().Be(route1);
            service.Routes[expectedPath1].ServiceType.Should().Be(typeof(SvcWithMultParamRoutes));
            service.Routes[expectedPath1].ServiceMethod.Should().NotBeNull();
            service.Routes[expectedPath1].RequestDtoType.Should().Be(typeof(DtoForMultParamRoutes));
            service.Routes.Keys.Should().Contain(expectedPath2);
            service.Routes[expectedPath2].Route.Should().Be(route2);
            service.Routes[expectedPath2].ServiceType.Should().Be(typeof(SvcWithMultParamRoutes));
            service.Routes[expectedPath2].ServiceMethod.Should().NotBeNull();
            service.Routes[expectedPath2].RequestDtoType.Should().Be(typeof(DtoForMultParamRoutes));
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