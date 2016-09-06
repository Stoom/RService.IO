using System;

namespace RService.IO.Abstractions
{
    public interface IRServiceFeature
    {
        Delegate.Activator MethodActivator { get; set; }
        IService Service { get; set; }
        Type RequestDtoType { get; set; }
    }
}