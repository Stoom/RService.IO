using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace RService.IO.Abstractions
{

    public class RoutingFeature : IRoutingFeature
    {
        public RouteData RouteData { get; set; }
        public RequestDelegate RouteHandler { get; set; }
    }
}