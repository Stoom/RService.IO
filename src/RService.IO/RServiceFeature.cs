using System;
using RService.IO.Abstractions;
using Delegate = RService.IO.Abstractions.Delegate;

namespace RService.IO
{
    public class RServiceFeature : IRServiceFeature
    {
        public Delegate.Activator MethodActivator { get; set; }
        public IService Service { get; set; }
        public Type RequestDtoType { get; set; }
    }
}