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
        public Dictionary<string, ServiceDef> Routes { get; protected set; }

        /// <summary>
        /// <see cref="Type"/>s of all the services in the <see cref="Assembly"/>s.
        /// </summary>
        public IEnumerable<Type> ServiceTypes => Routes.Values.Select(x => x.ServiceType);

        protected readonly RServiceOptions Options;

        /// <summary>
        /// Constructs a RService service and scans for routes and services in assemblies.
        /// </summary>
        /// <param name="options">Configuration options including the assemblies to scan.</param>
        public RService(IOptions<RServiceOptions> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Routes = new Dictionary<string, ServiceDef>();
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

            methodsWithAttribute.ForEach(method =>
            {
                var attrs = method.GetCustomAttributes<RouteAttribute>().ToList();
                var type = method.DeclaringType;
                attrs.ForEach(attr =>
                {
                    var def = new ServiceDef
                    {
                        Route = attr,
                        ServiceType = type
                    };
                    Routes.Add(attr.Path, def);
                });
            });

            methodsParamWithAttribute.ForEach(method =>
            {
                var methodType = method.DeclaringType;
                var paramType = method.GetParamWithAttribute<RouteAttribute>();
                var attrs = paramType?.GetAttributes<RouteAttribute>().ToList();
                attrs?.ForEach(a =>
                {
                    var def = new ServiceDef
                    {
                        Route = (RouteAttribute)a,
                        ServiceType = methodType
                    };
                    Routes.Add(def.Route.Path, def);
                });
            });
        }
    }
}