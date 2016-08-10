using System.Reflection;
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
        /// Constructs a startup base and scans for routes and services in assemblies.
        /// </summary>
        /// <param name="assemblies">Array of <see cref="Assembly"/>s to scan for routes and services.</param>
        protected StartupBase(params Assembly[] assemblies)
        {
            
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
        }
    }
}
