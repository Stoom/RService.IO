using System;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RService.IO.DependencyIngection
{
    public static class ServiceCollectionExtensions
    {
        private static readonly Action<RouteOptions> EmptyRouteOptions = options => { };

        public static IServiceCollection AddRServiceIo(
            this IServiceCollection services,
            Action<RServiceOptions> rserviceOptions)
        {
            return AddRServiceIo(services, rserviceOptions, EmptyRouteOptions);
        }

        public static IServiceCollection AddRServiceIo(
            this IServiceCollection services,
            Action<RServiceOptions> rserviceOptions,
            Action<RouteOptions> routeOptions,
            params Assembly[] assemblies)
        {
            services.AddOptions();
            services.AddRouting(routeOptions);

            services.AddSingleton<RService>();
            services.Configure(rserviceOptions);

            return services;
        }
    }
}