using System;
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
            Action<RouteOptions> routeOptions)
        {
            services.AddOptions();
            services.AddRouting(routeOptions);

            services.AddSingleton<RService>();
            services.Configure(rserviceOptions);

            var provider = services.BuildServiceProvider();
            var rservice = provider.GetService<RService>();
            foreach (var serviceType in rservice.ServiceTypes)
            {
                services.AddTransient(serviceType);
            }


            return services;
        }
    }
}