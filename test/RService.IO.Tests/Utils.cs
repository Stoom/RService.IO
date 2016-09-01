using System;
using System.Reflection;

namespace RService.IO.Tests
{
    public class Utils
    {
        public static Utils Instance => _instance.Value;
        // ReSharper disable once InconsistentNaming
        private static readonly Lazy<Utils> _instance = new Lazy<Utils>(() => new Utils());

        public Assembly CurrentAssembly => GetType().GetTypeInfo().Assembly;


        private Utils() { }
    }
}