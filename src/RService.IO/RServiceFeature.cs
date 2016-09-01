using RService.IO.Abstractions;

namespace RService.IO
{
    public class RServiceFeature : IRServiceFeature
    {
        public Delegate.Activator MethodActivator { get; set; }
    }
}