using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace RService.IO
{
    public static class RouteBuilderExtensions
    {

        public static IRouteBuilder MapRServiceIoRoute(this IRouteBuilder builder, RouteAttribute route, RequestDelegate handler)
        {
            var path = route.Path.Substring(1);

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (route.Verbs)
            {
                case RestVerbs.Any:
                    builder.MapRoute(path, handler);
                    break;
                default:
                    var verbs = route.Verbs.ToString().ToUpper()
                        .Split(',').Select(x => x.Trim())
                        .ToList();
                    verbs.ForEach(verb => builder.MapVerb(verb, path, handler));
                    break;
            }

            return builder;
        }
    }
}