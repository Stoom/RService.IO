﻿using System;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RService.IO.DependencyIngection;
using Xunit;

namespace RService.IO.Tests
{
    public class ServiceCollectionExtensionTests
    {
        private static readonly Action<RServiceOptions> EmptyRServiceOptions = options => { };
        private static readonly Action<RouteOptions> EmptyRouteOptions = options => { };

        [Fact]
        public void AddRServiceIo__AddsRService()
        {
            var services = new ServiceCollection();

            services.AddRServiceIo(EmptyRServiceOptions, EmptyRouteOptions);

            var app = BuildApplicationBuilder(services);
            var service = app.ApplicationServices.GetService<RService>();

            service.Should().NotBeNull();
        }

        [Fact]
        public void AddRServiceIo__ConfiguresRServiceOptions()
        {
            var services = new ServiceCollection();

            services.AddRServiceIo(EmptyRServiceOptions, EmptyRouteOptions);

            var app = BuildApplicationBuilder(services);
            var options = app.ApplicationServices.GetService<IOptions<RServiceOptions>>();

            options.Should().NotBeNull();
            options.Value.Should().NotBeNull();
        }

        [Fact]
        public void AddRServiceIo__AddsRouting()
        {
            var services = new ServiceCollection();

            services.AddRServiceIo(EmptyRServiceOptions, EmptyRouteOptions);

            var app = BuildApplicationBuilder(services);
            var service = app.ApplicationServices.GetService<RoutingMarkerService>();

            service.Should().NotBeNull();
        }

        [Fact]
        public void AddRServiceIo__ConfiguresRoutingOptions()
        {
            var services = new ServiceCollection();
            Action<RouteOptions> optionAction = opt =>
            {
                opt.AppendTrailingSlash = true;
                opt.LowercaseUrls = true;
            };

            services.AddRServiceIo(EmptyRServiceOptions, optionAction);

            var app = BuildApplicationBuilder(services);
            var options = app.ApplicationServices.GetService<IOptions<RouteOptions>>();

            options.Should().NotBeNull();
            options.Value.Should().NotBeNull();
            options.Value.AppendTrailingSlash.Should().Be(true);
            options.Value.LowercaseUrls.Should().Be(true);
        }

        [Fact]
        public void AddRServiceIo__AddsBlankRouteOptionsIfNotSpecified()
        {
            var expectedOptions = new RouteOptions();
            var services = new ServiceCollection();

            services.AddRServiceIo(EmptyRServiceOptions);

            var app = BuildApplicationBuilder(services);
            var options = app.ApplicationServices.GetService<IOptions<RouteOptions>>();

            options.Should().NotBeNull();
            options.Value.Should().NotBeNull();
            options.Value.AppendTrailingSlash.Should().Be(expectedOptions.AppendTrailingSlash);
            options.Value.LowercaseUrls.Should().Be(expectedOptions.LowercaseUrls);

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