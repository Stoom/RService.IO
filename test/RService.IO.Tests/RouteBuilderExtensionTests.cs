﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Xunit;

namespace RService.IO.Tests
{
    public class RouteBuilderExtensionTests
    {
        private static readonly RequestDelegate NullHandler = (c) => Task.FromResult(0);

        [Fact]
        public void MapRServiceIoRoute__AddsOneEntryForANY()
        {
            const string path = "/Foobar";
            var builder = CreateRouteBuilder();
            var route = new RouteAttribute(path);

            builder.MapRServiceIoRoute(route, NullHandler);
            var routes = GetRouteTemplates(builder);
            var expectedPath = path.Substring(1);

            var actualRoute = routes.FirstOrDefault(x => x.RouteTemplate.Equals(expectedPath));
            Assert.NotNull(actualRoute);
            actualRoute.Constraints.Count(x => x.Value is HttpMethodRouteConstraint).ShouldBeEquivalentTo(0);
        }

        [Fact]
        public void MapRServiceIoRoute__AddsOneEntryForGET()
        {
            const string path = "/Foobar";
            var builder = CreateRouteBuilder();
            var route = new RouteAttribute(path, RestVerbs.Get);

            builder.MapRServiceIoRoute(route, NullHandler);
            var routes = GetRouteTemplates(builder);

            var expectedPath = path.Substring(1);
            var expectedVerbs = route.Verbs.ToEnumerable();

            var methods = GetMethodsFromRoutes(routes, expectedPath);

            methods.Should().Contain(expectedVerbs);
        }

        [Fact]
        public void MapRServiceIoRoute__AddsEntryForEachVerbFlag()
        {
            const string path = "/Foobar";
            var builder = CreateRouteBuilder();
            var route = new RouteAttribute(path, RestVerbs.Get|RestVerbs.Post);

            builder.MapRServiceIoRoute(route, NullHandler);
            var routes = GetRouteTemplates(builder);

            var expectedPath = path.Substring(1);
            var expectedVerbs = route.Verbs.ToEnumerable();

            var methods = GetMethodsFromRoutes(routes, expectedPath);

            methods.Should().Contain(expectedVerbs);
        }

        private static IEnumerable<Route> GetRouteTemplates(IRouteBuilder builder)
        {
            return builder.Routes
                .Cast<Route>()
                .Where(r => r.Constraints.All(c => c.Key.Equals("httpMethod", StringComparison.CurrentCultureIgnoreCase)));
        }

        private static IEnumerable<string> GetMethodsFromRoutes(IEnumerable<Route> routes, string path)
        {
            return routes.Where(t => t.RouteTemplate.Equals(path))
                .SelectMany(x => x.Constraints.Values
                .OfType<HttpMethodRouteConstraint>()
                .Select(y => y.AllowedMethods))
                .SelectMany(s => s);
        }

        private static IRouteBuilder CreateRouteBuilder()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            serviceCollection.AddOptions();
            serviceCollection.AddRouting();
            serviceCollection.AddLogging();

            var applicationBuilder = new Mock<IApplicationBuilder>();
            var services = serviceCollection.BuildServiceProvider();
            applicationBuilder.SetupAllProperties();

            applicationBuilder
                .Setup(b => b.New().Build())
                .Returns(NullHandler);

            applicationBuilder.Object.ApplicationServices = services;

            var routeBuilder = new RouteBuilder(applicationBuilder.Object);
            return routeBuilder;
        }
    }
}