using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RService.IO
{
    /// <summary>
    /// Main service to hold routing and web service details.
    /// </summary>
    public class RService
    {
        /// <summary>
        /// The <see cref="Microsoft.AspNetCore.Routing.RouteHandler"/> to process request routing.
        /// </summary>
        public RequestDelegate RouteHanlder { get; set; } = DefaultRouteHandler;

        /// <summary>
        /// All discovered routes from services and their DTOs.
        /// </summary>
        public Dictionary<string, RouteAttribute> Routes { get; }

        /// <summary>
        /// Constructs a RService service and scans for routes and services in assemblies.
        /// </summary>
        /// <param name="assemblies"><see cref="Assembly"/>s to scan for routes and services.</param>
        public RService(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
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

        protected static Task DefaultRouteHandler(HttpContext context)
        {
            throw new NotImplementedException();
        }
    }
}