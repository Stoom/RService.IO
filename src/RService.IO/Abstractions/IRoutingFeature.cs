
using Microsoft.AspNetCore.Http;
using IAspNetCoreRoutingFeature = Microsoft.AspNetCore.Routing.IRoutingFeature;

namespace RService.IO.Abstractions
{

    /// <summary>
    /// A feature interface for RService.IO routing functionality.
    /// </summary>
    public interface IRoutingFeature : IAspNetCoreRoutingFeature
    {
        /// <summary>
        /// Gets or sets the <see cref="RequestDelegate" /> associated with the current request.
        /// </summary>
        RequestDelegate RouteHandler { get; set; }
    }
}