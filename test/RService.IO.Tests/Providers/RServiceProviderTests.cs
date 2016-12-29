using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using RService.IO.Abstractions;
using RService.IO.Providers;
using Xunit;

namespace RService.IO.Tests.Providers
{
    public class RServiceProviderTests
    {
        private readonly RService _rservice;
        private RServiceProvider _provider;
        private Mock<ISerializationProvider> _serializationProvider;

        public RServiceProviderTests()
        {
            var options = new OptionsManager<RServiceOptions>(new[]
            {
                new ConfigureOptions<RServiceOptions>(opt =>
                {
                    opt.ServiceAssemblies.Add(Utils.Instance.CurrentAssembly);
                })
            });
            _rservice = new RService(options);
        }

        private void Init()
        {
            _serializationProvider = new Mock<ISerializationProvider>();
            _provider = new RServiceProvider(_serializationProvider.Object);
        }

        [Fact]
        public async void Invoke__CallsServiceMethod()
        {
            Init();

            var service = new SvcWithMethodRoute();
            var routePath = SvcWithMethodRoute.RoutePath.Substring(1);

            var context = BuildContext(routePath, service);

            await _provider.Invoke(context.Object);

            service.HasAnyBeenCalled.Should().BeTrue();
        }

        [Fact]
        public void Invoke__WritesStringResponseToContextResponse()
        {
            Init();

            var service = new SvcWithMethodRoute { GetResponse = "Foobar" };
            var routePath = SvcWithMethodRoute.GetPath.Substring(1);

            var context = BuildContext(routePath, service);
            var body = context.Object.Response.Body;

            _provider.Invoke(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(service.GetResponse);
        }

        [Fact]
        public void Invoke__WritesPrimitiveResponseToContextResponse()
        {
            Init();

            var service = new SvcWithMethodRoute { PostResponse = 100 };
            var routePath = SvcWithMethodRoute.PostPath.Substring(1);

            var context = BuildContext(routePath, service, method:"POST");
            var body = context.Object.Response.Body;

            _provider.Invoke(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(service.PostResponse.ToString());
        }

        [Fact]
        public void Invoke__WritesEmptyStringIfServiceMethodReturnsNull()
        {
            Init();

            var service = new SvcWithMethodRoute { GetResponse = null };
            var routePath = SvcWithMethodRoute.GetPath.Substring(1);

            var context = BuildContext(routePath, service);
            var body = context.Object.Response.Body;

            _provider.Invoke(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(string.Empty);
        }

        [Fact]
        public void Invoke__SerializesResponseDto()
        {
            Init();

            const string expectedValue = "FizzBuzz";
            var service = new SvcWithMethodRoute();
            var routePath = SvcWithMethodRoute.RoutePath.Substring(1);

            var context = BuildContext(routePath, service, 
                typeof(RequestDto), responseDto: typeof(ResponseDto),
                method: "PUT");
            var body = context.Object.Response.Body;

            _serializationProvider.Setup(x => x.DehydrateResponse(It.IsAny<object>()))
                .Returns(expectedValue);

            _provider.Invoke(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(expectedValue);
        }

        [Fact]
        public void Invoke__DoesNotThrowsExceptionIfNullAuthProvider()
        {
            Init();

            var service = new SvcWithMethodRoute();
            var routePath = SvcWithMethodRoute.RoutePath.Substring(1);

            // ReSharper disable once RedundantArgumentDefaultValue
            var provider = new RServiceProvider(_serializationProvider.Object, null);

            _serializationProvider.Setup(x => x.DehydrateResponse(It.IsAny<object>())).Returns(String.Empty);
                
            var context = BuildContext(routePath, service,
                typeof(RequestDto), responseDto: typeof(ResponseDto),
                method: "PUT");

            Action act = async () => await provider.Invoke(context.Object);

            act.ShouldNotThrow<ApiException>();
        }

        [Fact]
        public void Invoke__ThrowsExceptionIfNotAuthorized()
        {
            Init();

            var service = new SvcWithMethodRoute();
            var routePath = SvcWithMethodRoute.RoutePath.Substring(1);

            var authMock = new Mock<IAuthProvider>();
            authMock.Setup(x => x.IsAuthorizedAsync(It.IsAny<HttpContext>(), It.IsAny<ServiceMetadata>()))
                .Returns(Task.FromResult(false));
            var provider = new RServiceProvider(_serializationProvider.Object, authMock.Object);

            var context = BuildContext(routePath, service,
                typeof(RequestDto), responseDto: typeof(ResponseDto),
                method: "PUT");

            Action act = () => provider.Invoke(context.Object).Wait(5000);

            act.ShouldThrow<ApiException>().And.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public void Invoke__DoesNotThrowsExceptionIfAuthorized()
        {
            Init();

            var service = new SvcWithMethodRoute();
            var routePath = SvcWithMethodRoute.RoutePath.Substring(1);

            var authMock = new Mock<IAuthProvider>();
            authMock.Setup(x => x.IsAuthorizedAsync(It.IsAny<HttpContext>(), It.IsAny<ServiceMetadata>()))
                .Returns(Task.FromResult(true));
            var provider = new RServiceProvider(_serializationProvider.Object, authMock.Object);

            var context = BuildContext(routePath, service,
                typeof(RequestDto), responseDto: typeof(ResponseDto),
                method: "PUT");

            Action act = () => provider.Invoke(context.Object).Wait(5000);

            act.ShouldNotThrow<ApiException>();
        }

        private Mock<HttpContext> BuildContext(string routePath, IService serviceInstance, Type requestDto = null,
            string requestBody = "", Type responseDto = null, string routeTemplate = "", 
            string contentType = "application/json", string method = "GET", 
            Dictionary<string, object> routeValues = null, IQueryCollection query = null)
        {
            RestVerbs restMethods;
            Enum.TryParse(method, true, out restMethods);
            var route = new RouteAttribute(routePath, restMethods);

            var context = new Mock<HttpContext>().SetupAllProperties();
            var request = new Mock<HttpRequest>().SetupAllProperties();
            var response = new Mock<HttpResponse>().SetupAllProperties();
            var reqBody = new MemoryStream(Encoding.ASCII.GetBytes(requestBody));
            var resBody = new MemoryStream();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var rserviceFeature = new RServiceFeature();
            var routingFeature = new RoutingFeature();

            features.Setup(x => x[typeof(IRServiceFeature)]).Returns(rserviceFeature);
            features.Setup(x => x[typeof(IRoutingFeature)]).Returns(routingFeature);

            rserviceFeature.RequestDtoType = requestDto;
            rserviceFeature.ResponseDtoType = responseDto;
            rserviceFeature.MethodActivator = _rservice.Routes[Utils.GetRouteKey(route, 0)].ServiceMethod;
            rserviceFeature.Service = serviceInstance;

            if (!string.IsNullOrWhiteSpace(routeTemplate))
            {
                routingFeature.RouteData = new RouteData();
                var constraints = new Mock<IInlineConstraintResolver>().SetupAllProperties();
                var irouter = new Mock<IRouter>().SetupAllProperties();
                var router = new Mock<Route>(irouter.Object, routeTemplate, constraints.Object)
                    .SetupAllProperties();
                foreach (var kvp in routeValues ?? new Dictionary<string, object>())
                {
                    routingFeature.RouteData.Values.Add(kvp.Key, kvp.Value);
                }
                routingFeature.RouteData.Routers.Add(null);
                routingFeature.RouteData.Routers.Add(router.Object);
                routingFeature.RouteData.Routers.Add(null);
            }

            request.Object.Method = method;
            request.Object.ContentType = contentType;
            request.Object.Body = reqBody;
            request.Object.Query = query;
            response.Object.Body = resBody;

            context.SetupGet(x => x.Request).Returns(request.Object);
            context.SetupGet(x => x.Response).Returns(response.Object);
            context.SetupGet(x => x.Features).Returns(features.Object);

            return context;
        }
    }
}