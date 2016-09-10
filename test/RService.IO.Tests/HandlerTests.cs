using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Moq;
using RService.IO.Abstractions;
using Xunit;

namespace RService.IO.Tests
{
    public class HandlerTests
    {
        private readonly IOptions<RServiceOptions> _options;
        private readonly RService _rservice;

        public HandlerTests()
        {
            _options = new OptionsManager<RServiceOptions>(new[]
            {
                new ConfigureOptions<RServiceOptions>(opt =>
                {
                    opt.ServiceAssemblies.Add(Utils.Instance.CurrentAssembly);
                })
            });
            _rservice = new RService(_options);
        }

        [Fact]
        public void ServiceHandler__CallsServiceMethod()
        {
            var service = new SvcWithMethodRoute();
            var routePath = SvcWithMethodRoute.RoutePath.Substring(1);

            var context = BuildContext(routePath, service);

            Handler.ServiceHandler(context.Object);

            service.HasAnyBeenCalled.Should().BeTrue();
        }

        [Fact]
        public void ServiceHandler__WritesStringResponseToContextResponse()
        {
            var service = new SvcWithMethodRoute { GetResponse = "Foobar" };
            var routePath = SvcWithMethodRoute.GetPath.Substring(1);

            var context = BuildContext(routePath, service);
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(service.GetResponse);
        }

        [Fact]
        public void ServiceHandler__WritesPrimitiveResponseToContextResponse()
        {
            var service = new SvcWithMethodRoute { PostResponse = 100 };
            var routePath = SvcWithMethodRoute.PostPath.Substring(1);

            var context = BuildContext(routePath, service);
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(service.PostResponse.ToString());
        }

        [Fact]
        public void ServiceHandler__WritesEmptyStringIfServiceMethodReturnsNull()
        {
            var service = new SvcWithMethodRoute { GetResponse = null };
            var routePath = SvcWithMethodRoute.GetPath.Substring(1);

            var context = BuildContext(routePath, service);
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(string.Empty);
        }

        [Fact]
        public void ServiceHandler__CreatesRequestDtoObjectFromContextBody()
        {
            const string expectedValue = "Eats llamas";
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);
            var reqBody = $"{{\"{nameof(DtoForParamRoute.Foobar)}\":\"{expectedValue}\"}}";

            var context = BuildContext(routePath, service, typeof(DtoForParamRoute), reqBody);
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(expectedValue);
        }
        private Mock<HttpContext> BuildContext(string routePath, IService serviceInstance, Type requestDto = null, string requestBody = "")
        {
            var context = new Mock<HttpContext>().SetupAllProperties();
            var request = new Mock<HttpRequest>().SetupAllProperties();
            var response = new Mock<HttpResponse>().SetupAllProperties();
            var reqBody = new MemoryStream(Encoding.ASCII.GetBytes(requestBody));
            var resBody = new MemoryStream();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var rserviceFeature = new RServiceFeature();

            features.Setup(x => x[typeof(IRServiceFeature)]).Returns(rserviceFeature);
            rserviceFeature.RequestDtoType = requestDto;
            rserviceFeature.MethodActivator = _rservice.Routes[routePath].ServiceMethod;
            rserviceFeature.Service = serviceInstance;

            request.Object.Body = reqBody;
            response.Object.Body = resBody;

            context.SetupGet(x => x.Request).Returns(request.Object);
            context.SetupGet(x => x.Response).Returns(response.Object);
            context.SetupGet(x => x.Features).Returns(features.Object);

            return context;
        }
    }
}