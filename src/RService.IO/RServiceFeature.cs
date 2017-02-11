using System;
using RService.IO.Abstractions;
using RService.IO.Abstractions.Providers;
using Delegate = RService.IO.Abstractions.Delegate;

namespace RService.IO
{
    public class RServiceFeature : IRServiceFeature
    {
        public ServiceMetadata Metadata { get; set; }
        public Delegate.Activator MethodActivator { get; set; }
        public IService Service { get; set; }
        public Type RequestDtoType { get; set; }
        public Type ResponseDtoType { get; set; }
        public ISerializationProvider RequestSerializer { get; set; }
        public ISerializationProvider ResponseSerializer { get; set; }
    }
}