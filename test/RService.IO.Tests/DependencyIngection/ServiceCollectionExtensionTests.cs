using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RService.IO.Abstractions;
using RService.IO.DependencyIngection;
using RService.IO.Providers;
using IServiceProvider = RService.IO.Abstractions.Providers.IServiceProvider;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using RService.IO.Abstractions.Providers;

namespace RService.IO.Tests.DependencyIngection
{
    public class ServiceCollectionExtensionTests
    {
        private static readonly Action<RServiceOptions> EmptyRServiceOptions = options => { };
        private static readonly Action<RouteOptions> EmptyRouteOptions = options => { };
        private Assembly CurrentAssembly => GetType().GetTypeInfo().Assembly;

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

        [Fact]
        public void AddRServiceIo__AddsNetJsonProviderForISerializationProvider()
        {
            var services = new ServiceCollection();

            services.AddRServiceIo(EmptyRServiceOptions);

            var app = BuildApplicationBuilder(services);
            var provider = app.ApplicationServices.GetService<ISerializationProvider>();

            provider.Should().NotBeNull().And.BeOfType<NetJsonProvider>();
        }

        [Fact]
        public void AddRServiceIo__UserImplementationForISerializationProviderTakesPrecedence()
        {
            var services = new ServiceCollection();

            services.AddTransient<ISerializationProvider, SeralizerProvider>();
            services.AddRServiceIo(EmptyRServiceOptions);

            var app = BuildApplicationBuilder(services);
            var provider = app.ApplicationServices.GetService<ISerializationProvider>();

            provider.Should().NotBeNull().And.BeOfType<SeralizerProvider>();
        }

        [Fact]
        public void AddRServiceIo__AddsRServiceProviderForIServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddRServiceIo(EmptyRServiceOptions);

            var app = BuildApplicationBuilder(services);
            var provider = app.ApplicationServices.GetService<IServiceProvider>();

            provider.Should().NotBeNull().And.BeOfType<RServiceProvider>();
        }

        [Fact]
        public void AddRServiceIo__UserImplementationForIServiceProviderTakesPrecedence()
        {
            var services = new ServiceCollection();

            services.AddTransient<IServiceProvider, ServiceProvider>();
            services.AddRServiceIo(EmptyRServiceOptions);

            var app = BuildApplicationBuilder(services);
            var provider = app.ApplicationServices.GetService<IServiceProvider>();

            provider.Should().NotBeNull().And.BeOfType<ServiceProvider>();
        }

        [Fact]
        public void AddRServiceIo__AddsWebServiceTypes()
        {
            var services = new ServiceCollection();

            services.AddRServiceIo(opts => { opts.ServiceAssemblies.Add(CurrentAssembly); });

            var app = BuildApplicationBuilder(services);
            var webservice = app.ApplicationServices.GetService(typeof(SvcWithMethodRoute));

            webservice.Should().NotBeNull();
            webservice.Should().BeOfType<SvcWithMethodRoute>();
        }

        [Fact]
        public void AddRServiceIo__WebServicesShouldBeTransient()
        {
            var services = new ServiceCollection();

            services.AddRServiceIo(opts => { opts.ServiceAssemblies.Add(CurrentAssembly); });

            var app = BuildApplicationBuilder(services);
            var webservice1 = app.ApplicationServices.GetService(typeof(SvcWithMethodRoute));
            var webservice2 = app.ApplicationServices.GetService(typeof(SvcWithMethodRoute));

            webservice1.Should().NotBe(webservice2);
        }

        [Fact]
        public void AddRServiceIo__RegistersExceptionFilterWithIoC()
        {
            var services = new ServiceCollection();
            var exceptionFilter = new Mock<IExceptionFilter>().SetupAllProperties();

            services.AddRServiceIo(opts =>
            {
                opts.ServiceAssemblies.Add(CurrentAssembly);
                opts.ExceptionFilter = exceptionFilter.Object;
            });

            var app = BuildApplicationBuilder(services);
            var globalExceptionFilter = app.ApplicationServices.GetService(typeof(IExceptionFilter));

            globalExceptionFilter.Should().NotBeNull().And.BeOfType(exceptionFilter.Object.GetType());
        }

        [Fact]
        public void AddRServiceIo__DoesNotRegistersExceptionFilterWithIoCIfNull()
        {
            var services = new ServiceCollection();

            services.AddRServiceIo(opts =>
            {
                opts.ServiceAssemblies.Add(CurrentAssembly);
                opts.ExceptionFilter = null;
            });

            var app = BuildApplicationBuilder(services);
            var globalExceptionFilter = app.ApplicationServices.GetService(typeof(IExceptionFilter));

            globalExceptionFilter.Should().BeNull();
        }

        [Fact]
        public void AddRServiceIoAuthorization__AddsRServiceProviderForIAuthProvider()
        {
            var services = new ServiceCollection();

            services.AddAuthorization()
                .AddRServiceIoAuthorization();

            var app = BuildApplicationBuilder(services);
            var provider = app.ApplicationServices.GetService<IAuthProvider>();

            provider.Should().NotBeNull().And.BeOfType<AuthProvider>();
        }

        [Fact]
        public void AddRServiceIoAuthorization__UserImplementationForIAuthProviderTakesPrecedence()
        {
            var services = new ServiceCollection();

            services.AddTransient<IAuthProvider, AuthorizationProvider>()
                .AddAuthorization()
                .AddRServiceIoAuthorization();

            var app = BuildApplicationBuilder(services);
            var provider = app.ApplicationServices.GetService<IAuthProvider>();

            provider.Should().NotBeNull().And.BeOfType<AuthorizationProvider>();
        }

        private static IApplicationBuilder BuildApplicationBuilder(IServiceCollection services)
        {
            var builder = new Mock<IApplicationBuilder>();
            builder.SetupAllProperties();
            builder.Object.ApplicationServices = services.BuildServiceProvider();

            return builder.Object;
        }

        // ReSharper disable ClassNeverInstantiated.Local
        private class ServiceProvider : IServiceProvider
        {
            public Task Invoke(HttpContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class SeralizerProvider : ISerializationProvider
        {
            public string ContentType { get; } = string.Empty;
            public object HydrateRequest(HttpContext ctx, Type dtoType)
            {
                throw new NotImplementedException();
            }

            public string DehydrateResponse(object resDto)
            {
                throw new NotImplementedException();
            }
        }

        private class AuthorizationProvider : IAuthProvider
        {
            public Task<bool> IsAuthorizedAsync(HttpContext ctx, ServiceMetadata metadata)
            {
                throw new NotImplementedException();
            }

            public Task<bool> IsAuthorizedAsync(HttpContext ctx, IEnumerable<object> authorizationFilters)
            {
                throw new NotImplementedException();
            }
        }
        // ReSharper restore ClassNeverInstantiated.Local
    }
}