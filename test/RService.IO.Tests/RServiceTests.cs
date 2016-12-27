using System;
using System.Linq;
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
        public void Ctor__CompositeKeyIncludesPathAndRoute()
        {
            const string path = SvcForMethods.PostPath;
            const RestVerbs method = SvcForMethods.PostMethod;
            var route = new RouteAttribute(path, method);

            var service = new RService(_options);

            service.Routes.Keys.Should().Contain(Utils.GetRouteKey(route));
        }

        [Fact]
        public void Ctor__AddsEntryForEachMethod()
        {
            const string path = SvcForMethods.MultiPath;
            const RestVerbs methods = SvcForMethods.MultiMethod;
            var routes = methods.GetFlags().Select(m => new RouteAttribute(path, m)).ToList();
            var expectedKeys = routes.Select(r => Utils.GetRouteKey(r)).ToList();
            var expectedRoute = new RouteAttribute(path, methods);

            var service = new RService(_options);

            service.Routes.Keys.Should().Contain(expectedKeys);
            service.Routes[expectedKeys[0]].Route.Should().Be(expectedRoute);
            service.Routes[expectedKeys[1]].Route.Should().Be(expectedRoute);
        }

        [Fact]
        public void Ctor__ScansAssembliesForRoutesOnMethods()
        {
            var route = new RouteAttribute(SvcWithMethodRoute.RoutePath, RestVerbs.Get);
            var expectedPath = Utils.GetRouteKey(route);

            var service = new RService(_options);

            service.Routes.Keys.Should().Contain(expectedPath);
            service.Routes[expectedPath].Route.Should().Be(route);
            service.Routes[expectedPath].ServiceType.Should().Be(typeof(SvcWithMethodRoute));
            service.Routes[expectedPath].ServiceMethod.Should().NotBeNull();
        }

        [Fact]
        public void Ctor__ScansAssembliesForRoutesOnMethodsFirstParam()
        {
            var route = new RouteAttribute(SvcWithParamRoute.RoutePath, RestVerbs.Get);
            var expectedPath = Utils.GetRouteKey(route);

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
            var route1 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath1, RestVerbs.Get);
            var route2 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath2, RestVerbs.Get);
            var expectedPath1 = Utils.GetRouteKey(route1);
            var expectedPath2 = Utils.GetRouteKey(route2);

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
            var route1 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath1, RestVerbs.Get);
            var route2 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath2, RestVerbs.Get);
            var expectedPath1 = Utils.GetRouteKey(route1);
            var expectedPath2 = Utils.GetRouteKey(route2);

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
        public void Ctor__AddsUniqueIdentForEachRoute()
        {
            var service = new RService(_options);

            var idents = service.Routes.Values.Select(x => x.Ident).Where(y => y != null).ToList();
            idents.Count.Should().Be(service.Routes.Count, "Service method(s) missing ident.");
            idents.Duplicates().Should().BeEmpty();
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