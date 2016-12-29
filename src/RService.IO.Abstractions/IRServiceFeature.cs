using System;

namespace RService.IO.Abstractions
{
    public interface IRServiceFeature
    {
        ServiceMetadata Metadata { get; set; }
        Delegate.Activator MethodActivator { get; set; }
        IService Service { get; set; }
        Type RequestDtoType { get; set; }
        Type ResponseDtoType { get; set; }
    }
}