using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Options;
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
            var def = new ServiceDef
            {
                Route = route,
                ServiceType = typeof(SvcWithMethodRoute)
            };
            var expected = new Dictionary<string, ServiceDef>
            {
                { route.Path, def }
            };

            var service = new RService(_options);

            service.Routes.Should().Contain(expected);
        }

        [Fact]
        public void Ctor__ScansAssembliesForRoutesOnMethodsFirstParam()
        {
            var route = new RouteAttribute(SvcWithParamRoute.RoutePath);
            var def = new ServiceDef
            {
                Route = route,
                ServiceType = typeof(SvcWithParamRoute)
            };
            var expected = new Dictionary<string, ServiceDef>
            {
                { route.Path, def }
            };

            var service = new RService(_options);

            service.Routes.Should().Contain(expected);
        }

        [Fact]
        public void Ctor__ScansAssembliesForMultipleRoutesOnMethods()
        {
            var route1 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath1);
            var route2 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath2);
            var def1 = new ServiceDef
            {
                Route = route1,
                ServiceType = typeof(SvcWithMultMethodRoutes)
            };
            var def2 = new ServiceDef
            {
                Route = route2,
                ServiceType = typeof(SvcWithMultMethodRoutes)
            };
            var expected = new Dictionary<string, ServiceDef>
            {
                { route1.Path, def1 },
                { route2.Path, def2 }
            };

            var service = new RService(_options);

            service.Routes.Should().Contain(expected);
        }

        [Fact]
        public void Ctor__ScansAssembliesForMultipleRoutesOfParams()
        {
            var route1 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath1);
            var route2 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath2);
            var def1 = new ServiceDef
            {
                Route = route1,
                ServiceType = typeof(SvcWithMultParamRoutes)
            };
            var def2 = new ServiceDef
            {
                Route = route2,
                ServiceType = typeof(SvcWithMultParamRoutes)
            };
            var expected = new Dictionary<string, ServiceDef>
            {
                { route1.Path, def1 },
                { route2.Path, def2 }
            };

            var service = new RService(_options);

            service.Routes.Should().Contain(expected);
        }

        [Fact]
        public void Ctor__ScansAssembliesForSerivceTypes()
        {
            var expectedServiceType = typeof(SvcWithMethodRoute);

            var service = new RService(_options);

            service.ServiceTypes.Should().Contain(expectedServiceType);
        }
    }
}