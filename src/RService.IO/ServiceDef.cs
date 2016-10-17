using System;
using RService.IO.Abstractions;
using Delegate = RService.IO.Abstractions.Delegate;

namespace RService.IO
{
    public struct ServiceDef
    {
        public RouteAttribute Route;
        public Type ServiceType;
        public Delegate.Activator ServiceMethod;
        public Type RequestDtoType;
        public Type ResponseDtoType;
    }
}