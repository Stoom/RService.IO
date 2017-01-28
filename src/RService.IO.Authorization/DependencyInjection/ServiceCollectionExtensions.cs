using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RService.IO.Abstractions.Providers;
using RService.IO.Authorization.Providers;

namespace RService.IO.Authorization.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRServiceIoAuthorization(
            this IServiceCollection services)
        {
            services.AddOptions();

            services.TryAddTransient<IAuthProvider, AuthProvider>();

            return services;
        }
    }
}