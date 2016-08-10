using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RService.IO.Tests
{
    public class StartupBaseTests
    {

        [Fact]
        public void ConfigureService__AddsRouting()
        {
            var services = new ServiceCollection();
            var startup = new Startup();
            
            startup.ConfigureServices(services);

            Assert.True(services.Any(x => x.ImplementationType?.Name.Equals("RoutingMarkerService") ?? false));
        }


        private class Startup : StartupBase { }
    }
}
