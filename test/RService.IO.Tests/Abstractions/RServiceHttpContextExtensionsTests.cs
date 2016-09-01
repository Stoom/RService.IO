using System;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Moq;
using RService.IO.Abstractions;
using Xunit;

namespace RService.IO.Tests.Abstractions
{
    public class RServiceHttpContextExtensionsTests
    {
        [Fact]
        public void GetRequestDtoType__ThrowsArguementNullExceptionOnNullContext()
        {
            Action comparison = () => { RServiceHttpContextExtensions.GetRequestDtoType(null); };

            comparison.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetRequestDtoType__GetsHandlerFromRServiceFeature()
        {
            var expectedDtoType = typeof(DtoForParamRoute);

            var context = new Mock<HttpContext>().SetupAllProperties();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var rserviceFeature = new RServiceFeature();
            context.SetupGet(x => x.Features).Returns(features.Object);
            features.Setup(x => x[typeof(IRServiceFeature)]).Returns(rserviceFeature);
            rserviceFeature.RequestDtoType = expectedDtoType;

            var type = context.Object.GetRequestDtoType();

            type.Should().NotBeNull().And.Be(expectedDtoType);
        }

        [Fact]
        public void GetRequestDtoType__ReturnsNullIfNotRServiceFeature()
        {
            var context = new Mock<HttpContext>().SetupAllProperties();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var routingFeature = new Mock<IRoutingFeature>().SetupAllProperties();
            context.SetupGet(x => x.Features).Returns(features.Object);
            features.Setup(x => x[typeof(IRoutingFeature)]).Returns(routingFeature.Object);

            var handle = context.Object.GetRouteHandler();

            handle.Should().BeNull();
        }
    }
}