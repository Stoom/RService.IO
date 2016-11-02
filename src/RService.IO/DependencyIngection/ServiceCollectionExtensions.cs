using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RService.IO.Abstractions;
using RService.IO.Providers;
using IServiceProvider = RService.IO.Abstractions.IServiceProvider;

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

            services.TryAddTransient<ISerializationProvider, NetJsonProvider>();
            services.TryAddTransient<IServiceProvider, RServiceProvider>();

            services.AddSingleton<RService>();
            services.Configure(rserviceOptions);

            var options = new RServiceOptions();
            rserviceOptions(options);

            if (options.ExceptionFilter != null)
                services.AddScoped(typeof(IExceptionFilter), options.ExceptionFilter.GetType());

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