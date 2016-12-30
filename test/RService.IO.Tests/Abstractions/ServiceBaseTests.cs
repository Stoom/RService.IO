using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using RService.IO.Abstractions;
using Xunit;

namespace RService.IO.Tests.Abstractions
{
    public class ServiceBaseTests
    {
        [Fact]
        public void RequestMethod__SetFromContext()
        {
            var context = new Mock<HttpContext>().SetupAllProperties();
            context.Setup(x => x.Request.Method).Returns("GET");

            var service = new Mock<ServiceBase>().SetupAllProperties();
            service.Object.Context = context.Object;

            service.Object.RequestMethod.Should().Be(RestVerbs.Get);
        }

        [Fact]
        public void Request__IsTheRequestOfTheContext()
        {
            var context = new Mock<HttpContext>().SetupAllProperties();
            var service = new Mock<ServiceBase>().SetupAllProperties();
            service.Object.Context = context.Object;

            service.Object.Request.Should().Be(context.Object.Request);
        }

        [Fact]
        public void Response__IsTheResponseOfTheContext()
        {
            var context = new Mock<HttpContext>().SetupAllProperties();
            var service = new Mock<ServiceBase>().SetupAllProperties();
            service.Object.Context = context.Object;

            service.Object.Response.Should().Be(context.Object.Response);
        }
    }
}