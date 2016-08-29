using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using IRoutingFeature = RService.IO.Abstractions.IRoutingFeature;

namespace RService.IO.Router
{

    public class RoutingFeature : IRoutingFeature
    {
        public RouteData RouteData { get; set; }
        public RequestDelegate RouteHandler { get; set; }
    }
}