using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RService.IO.Abstractions;
using RService.IO.Abstractions.Providers;
using RService.IO.Authorization.DependencyInjection;
using RService.IO.Authorization.Providers;
using Xunit;

namespace RService.IO.Authorization.Tests
{
    public class RServiceIoCollectionExtensionTests
    {
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