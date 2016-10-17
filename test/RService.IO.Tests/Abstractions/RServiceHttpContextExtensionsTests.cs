using System;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Moq;
using RService.IO.Abstractions;
using Xunit;
using Delegate = RService.IO.Abstractions.Delegate;

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

            var handle = context.Object.GetRequestDtoType();

            handle.Should().BeNull();
        }

        [Fact]
        public void GetResponseDtoType__GetsHandlerFromRServiceFeature()
        {
            var expectedDtoType = typeof(ResponseDto);

            var context = new Mock<HttpContext>().SetupAllProperties();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var rserviceFeature = new RServiceFeature();
            context.SetupGet(x => x.Features).Returns(features.Object);
            features.Setup(x => x[typeof(IRServiceFeature)]).Returns(rserviceFeature);
            rserviceFeature.ResponseDtoType = expectedDtoType;

            var type = context.Object.GetResponseDtoType();

            type.Should().NotBeNull().And.Be(expectedDtoType);
        }

        [Fact]
        public void GetResponseDtoType__ReturnsNullIfNotRServiceFeature()
        {
            var context = new Mock<HttpContext>().SetupAllProperties();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var routingFeature = new Mock<IRoutingFeature>().SetupAllProperties();
            context.SetupGet(x => x.Features).Returns(features.Object);
            features.Setup(x => x[typeof(IRoutingFeature)]).Returns(routingFeature.Object);

            var handle = context.Object.GetResponseDtoType();

            handle.Should().BeNull();
        }

        [Fact]
        public void GetServiceMethodActivator__ThrowsArguementNullExceptionOnNullContext()
        {
            Action comparison = () => { RServiceHttpContextExtensions.GetServiceMethodActivator(null); };

            comparison.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetServiceMethodActivator__GetActivatorFromRServiceFeature()
        {
            var expectedActivator = new Mock<Delegate.Activator>().Object;

            var context = new Mock<HttpContext>().SetupAllProperties();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var rserviceFeature = new RServiceFeature();
            context.SetupGet(x => x.Features).Returns(features.Object);
            features.Setup(x => x[typeof(IRServiceFeature)]).Returns(rserviceFeature);
            rserviceFeature.MethodActivator = expectedActivator;

            var type = context.Object.GetServiceMethodActivator();

            type.Should().NotBeNull().And.Be(expectedActivator);
        }

        [Fact]
        public void GetServiceMethodActivator__ReturnsNullIfNotRServiceFeature()
        {
            var context = new Mock<HttpContext>().SetupAllProperties();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var routingFeature = new Mock<IRoutingFeature>().SetupAllProperties();
            context.SetupGet(x => x.Features).Returns(features.Object);
            features.Setup(x => x[typeof(IRoutingFeature)]).Returns(routingFeature.Object);

            var handle = context.Object.GetServiceMethodActivator();

            handle.Should().BeNull();
        }

        [Fact]
        public void GetServiceInstance__ThrowsArguementNullExceptionOnNullContext()
        {
            Action comparison = () => { RServiceHttpContextExtensions.GetServiceInstance(null); };

            comparison.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetServiceInstance__GeServiceInstanceFromRServiceFeature()
        {
            var expectedServiceInstance = new Mock<SvcWithMethodRoute>().Object;

            var context = new Mock<HttpContext>().SetupAllProperties();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var rserviceFeature = new RServiceFeature();
            context.SetupGet(x => x.Features).Returns(features.Object);
            features.Setup(x => x[typeof(IRServiceFeature)]).Returns(rserviceFeature);
            rserviceFeature.Service = expectedServiceInstance;

            var type = context.Object.GetServiceInstance();

            type.Should().NotBeNull().And.Be(expectedServiceInstance);
        }

        [Fact]
        public void GetServiceInstance__ReturnsNullIfNotRServiceFeature()
        {
            var context = new Mock<HttpContext>().SetupAllProperties();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var routingFeature = new Mock<IRoutingFeature>().SetupAllProperties();
            context.SetupGet(x => x.Features).Returns(features.Object);
            features.Setup(x => x[typeof(IRoutingFeature)]).Returns(routingFeature.Object);

            var handle = context.Object.GetServiceInstance();

            handle.Should().BeNull();
        }
    }
}