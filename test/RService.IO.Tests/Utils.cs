using System;
using System.Reflection;
using RService.IO.Abstractions;

namespace RService.IO.Tests
{
    public class Utils
    {
        public static Utils Instance => _instance.Value;
        // ReSharper disable once InconsistentNaming
        private static readonly Lazy<Utils> _instance = new Lazy<Utils>(() => new Utils());

        public Assembly CurrentAssembly => GetType().GetTypeInfo().Assembly;


        private Utils() { }

        public static string GetRouteKey(RouteAttribute route, int offset=1)
        {
            return $"{route.Path.Substring(offset)}:{route.Verbs.ToString().ToUpperInvariant()}";
        }
    }
}