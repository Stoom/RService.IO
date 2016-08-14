using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RService.IO.Tests
{
    public class StartupBaseTests
    {
        private Assembly CurrentAssembly => GetType().GetTypeInfo().Assembly;

        [Fact]
        public void Ctor__ScansAssemblieForRoutesOnMethods()
        {
            var attr = new RouteAttribute(SvcWithMethodRoute.RoutePath);
            var type = typeof(SvcWithMethodRoute);
            const string name = nameof(SvcWithMethodRoute.Any);

            var expectedRoutes = new Dictionary<string, RouteAttribute>
            {
                {
                    GenerateMethodRouteName(type, name, attr),
                    attr
                }
            };

            var startup = new Startup(CurrentAssembly);

            startup.Routes.Should().Contain(expectedRoutes);
        }

        [Fact]
        public void Ctor__ScansAssemblieForRoutesOnMethodsFirstParam()
        {
            var attr = new RouteAttribute(SvcWithParamRoute.RoutePath);
            var type = typeof(DtoForParamRoute);

            var expectedRoutes = new Dictionary<string, RouteAttribute>
            {
                {
                    GenerateParmaRouteName(type, attr),
                    attr
                }
            };

            var startup = new Startup(CurrentAssembly);

            startup.Routes.Should().Contain(expectedRoutes);
        }

        [Fact]
        public void Ctor__ScansAssemblyForMultipleRoutesOnMethods()
        {
            var attr1 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath1);
            var attr2 = new RouteAttribute(SvcWithMultMethodRoutes.RoutePath2);
            var type = typeof(SvcWithMultMethodRoutes);
            const string name = nameof(SvcWithMultMethodRoutes.Any);

            var expectedRoutes = new Dictionary<string, RouteAttribute>
            {
                {
                    GenerateMethodRouteName(type, name, attr1),
                    attr1
                },
                {
                    GenerateMethodRouteName(type, name, attr2),
                    attr2
                }
            };

            var startup = new Startup(CurrentAssembly);

            startup.Routes.Should().Contain(expectedRoutes);
        }

        [Fact]
        public void Ctor__ScansAssemblieForMultipleRoutesOfParams()
        {
            var attr1 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath1);
            var attr2 = new RouteAttribute(SvcWithMultParamRoutes.RoutePath2);
            var type = typeof(DtoForMultParamRoutes);

            var expectedRoutes = new Dictionary<string, RouteAttribute>
            {
                {
                    GenerateParmaRouteName(type, attr1),
                    attr1
                },
                {
                    GenerateParmaRouteName(type, attr2),
                    attr2
                }
            };

            var startup = new Startup(CurrentAssembly);

            startup.Routes.Should().Contain(expectedRoutes);
        }

        [Fact]
        public void ConfigureService__AddsRouting()
        {
            var services = new ServiceCollection();
            var startup = new Startup();

            startup.ConfigureServices(services);

            Assert.True(services.Any(x => x.ImplementationType?.Name.Equals("RoutingMarkerService") ?? false));
        }

        private static string GenerateMethodRouteName(Type type, string methodName, RouteAttribute attribute)
        {
            return $"{type.FullName}.{methodName} {attribute.Verbs} {attribute.GetHashCode()}";
        }

        private static string GenerateParmaRouteName(Type type, RouteAttribute attribute)
        {
            return $"{type.FullName} {attribute.Verbs} {attribute.GetHashCode()}";
        }

        #region Classes for testing
        private class Startup : StartupBase
        {
            internal Startup(params Assembly[] assemblies) : base(assemblies) { }
        }
        #endregion
    }
}
