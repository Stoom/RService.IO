using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RService.IO
{
    /// <summary>
    /// Startup base class that will auto-wire route.
    /// </summary>
    public abstract class StartupBase
    {
        /// <summary>
        /// The <see cref="RouteHandler"/> to process request routing.
        /// </summary>
        public RequestDelegate RouteHanlder { get; set; } = InternalRouteHandler;
        /// <summary>
        /// All discovered routes from services and their DTOs.
        /// </summary>
        public Dictionary<string, RouteAttribute> Routes { get; }

        /// <summary>
        /// Constructs a startup base and scans for routes and services in assemblies.
        /// </summary>
        /// <param name="assemblies">Array of <see cref="Assembly"/>s to scan for routes and services.</param>
        protected StartupBase(params Assembly[] assemblies)
        {
            Routes = new Dictionary<string, RouteAttribute>();

            // Scan assemblies
            foreach (var assembly in assemblies)
            {
                var classes = assembly.GetTypes().Where(x => x.ImplementsInterface<IService>()).ToList();
                var methodsWithAttribute = classes.SelectMany(x => x.GetPublicMethods()).Where(x => x.HasAttribute<RouteAttribute>()).ToList();
                var methodsParamWithAttribute = classes.SelectMany(x => x.GetPublicMethods()).Where(x => x.HasParamWithAttribute<RouteAttribute>()).ToList();

                methodsWithAttribute.ForEach(x =>
                {
                    var attrs = x.GetCustomAttributes<RouteAttribute>().ToList();
                    attrs.ForEach(attr => Routes.Add(GetNameForMethodRoute(x, attr), attr));
                });

                methodsParamWithAttribute.ForEach(method =>
                {
                    var type = method.GetParamWithAttribute<RouteAttribute>();
                    var attrs = type?.GetAttributes<RouteAttribute>().ToList();
                    attrs?.ForEach(a =>
                    {
                        var attr = (RouteAttribute) a;
                        Routes.Add(GetNameForParamRoute(type, attr), attr);
                    });
                });
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var routeBuilder = new RouteBuilder(app, new RouteHandler(RouteHanlder));

            foreach (var route in Routes)
            {
                MapRoute(routeBuilder, route.Value);
            }

            var routes = routeBuilder.Build();
            app.UseRouter(routes);
        }

        protected void MapRoute(RouteBuilder builder, RouteAttribute route)
        {
            var path = route.Path.Substring(1);
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (route.Verbs)
            {
                case RestVerbs.Any:
                    builder.MapRoute(path, RouteHanlder);
                    break;
                default:
                    var verbs = route.Verbs.ToString().ToUpper().Split(',').ToList();
                    verbs.ForEach(verb => builder.MapVerb(verb, path, RouteHanlder));
                    break;
            }
        }

        protected static string GetNameForMethodRoute(MethodInfo info, RouteAttribute attribute)
        {
            return $"{info.DeclaringType.FullName}.{info.Name} {attribute.Verbs} {attribute.GetHashCode()}";
        }

        protected static string GetNameForParamRoute(Type type, RouteAttribute attribute)
        {
            return $"{type.FullName} {attribute.Verbs} {attribute.GetHashCode()}";
        }

        protected static Task InternalRouteHandler(HttpContext context)
        {
            throw new NotImplementedException();
        }
    }
}
