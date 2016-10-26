using System;

namespace RService.IO.Abstractions
{
    public interface IRServiceFeature
    {
        Delegate.Activator MethodActivator { get; set; }
        ServiceBase Service { get; set; }
        Type RequestDtoType { get; set; }
        Type ResponseDtoType { get; set; }
    }
}