using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RService.IO.Abstractions;

namespace RService.IO
{
    public static class RouteBuilderExtensions
    {
        internal static Regex RoutePathCleaner = new Regex(@"^[\/~]+", RegexOptions.Compiled);

        public static IRouteBuilder MapRServiceIoRoute(this IRouteBuilder builder, RouteAttribute route, RequestDelegate handler)
        {
            var path = RoutePathCleaner.Replace(route.Path, string.Empty);

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