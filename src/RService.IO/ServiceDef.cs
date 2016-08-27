using System;
using System.Reflection.Emit;

namespace RService.IO
{
    public struct ServiceDef
    {
        public RouteAttribute Route;
        public Type ServiceType;
        public DynamicMethod ServiceMethod;
    }
}