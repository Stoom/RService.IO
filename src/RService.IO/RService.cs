using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;
using RService.IO.Abstractions;

namespace RService.IO
{
    /// <summary>
    /// Main service to hold routing and web service details.
    /// </summary>
    public class RService
    {
        /// <summary>
        /// All discovered routes from services and their DTOs.
        /// </summary>
        public Dictionary<string, RouteAttribute> Routes { get; }

        protected RServiceOptions Options;

        /// <summary>
        /// Constructs a RService service and scans for routes and services in assemblies.
        /// </summary>
        /// <param name="options">Configuration options including the assemblies to scan.</param>
        public RService(IOptions<RServiceOptions> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Routes = new Dictionary<string, RouteAttribute>();
            Options = options.Value;

            foreach (var assembly in Options.ServiceAssemblies)
            {
                ScanAssemblyForRoutes(assembly);
            }
        }

        /// <summary>
        /// Scans an <see cref="Assembly"/> for services and routes.
        /// </summary>
        /// <param name="assembly">Assembly to scan.</param>
        protected void ScanAssemblyForRoutes(Assembly assembly)
        {
            var classes = assembly.GetTypes().Where(x => x.ImplementsInterface<IService>()).ToList();
            var methodsWithAttribute = classes.SelectMany(x => x.GetPublicMethods()).Where(x => x.HasAttribute<RouteAttribute>()).ToList();
            var methodsParamWithAttribute = classes.SelectMany(x => x.GetPublicMethods()).Where(x => x.HasParamWithAttribute<RouteAttribute>()).ToList();

            methodsWithAttribute.ForEach(x =>
            {
                var attrs = x.GetCustomAttributes<RouteAttribute>().ToList();
                attrs.ForEach(attr => Routes.Add(attr.Path, attr));
            });

            methodsParamWithAttribute.ForEach(method =>
            {
                var type = method.GetParamWithAttribute<RouteAttribute>();
                var attrs = type?.GetAttributes<RouteAttribute>().ToList();
                attrs?.ForEach(a =>
                {
                    var attr = (RouteAttribute)a;
                    Routes.Add(attr.Path, attr);
                });
            });
        }
    }
}