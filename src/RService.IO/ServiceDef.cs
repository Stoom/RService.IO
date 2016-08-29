using System;

namespace RService.IO
{
    public struct ServiceDef
    {
        public RouteAttribute Route;
        public Type ServiceType;
        public DelegateFactory.Activator ServiceMethod;
    }
}