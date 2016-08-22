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
            var expected = new Dictionary<string, RouteAttribute>
            {
                { route.Path, route }
            };

            var service = new RService(_options);

            service.Routes.Should().Contain(expected);
        }

        [Fact]
        public void Ctor__ScansAssembliesForRoutesOnMethodsFirstParam()
        {
            var route = new RouteAttribute(SvcWithParamRoute.RoutePath);
            var expected = new Dictionary<string, RouteAttribute>
            {
                { route.Path, route }
            };

            var service = new RService(_options);

            service.Routes.Should().Contain(expected);
        }

        [Fact]
        public void Ctor__ScansAssembliesForMultipleRoutesOnMethods()
        {
            var route1 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath1);
            var route2 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath2);
            var expected = new Dictionary<string, RouteAttribute>
            {
                { route1.Path, route1 },
                { route2.Path, route2 }
            };

            var service = new RService(_options);

            service.Routes.Should().Contain(expected);
        }

        [Fact]
        public void Ctor__ScansAssembliesForMultipleRoutesOfParams()
        {
            var route1 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath1);
            var route2 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath2);
            var expected = new Dictionary<string, RouteAttribute>
            {
                { route1.Path, route1 },
                { route2.Path, route2 }
            };

            var service = new RService(_options);

            service.Routes.Should().Contain(expected);
        }
    }
}