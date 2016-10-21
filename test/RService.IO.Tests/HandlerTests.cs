using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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

            var context = BuildContext(routePath, service, method:"POST");
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
        public void ServiceHandler__ThrowsNotImplementedIfNotJsonAndBodyHasContent()
        {
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);
            const string reqBody = "Hello World";

            var context = BuildContext(routePath, service, typeof(DtoForParamRoute), reqBody, contentType: "text/plain");

            Action act = () => Handler.ServiceHandler(context.Object).Wait(5000);

            act.ShouldThrow<NotImplementedException>().WithMessage("text/plain is currently not supported.");
        }

        [Fact]
        public void ServiceHandler__ThrowsNotImplementedIfNoContentTypeAndBodyHasContent()
        {
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);
            const string reqBody = "Hello World";

            var context = BuildContext(routePath, service, typeof(DtoForParamRoute), reqBody, contentType: null);

            Action act = () => Handler.ServiceHandler(context.Object).Wait(5000);

            act.ShouldThrow<NotImplementedException>().WithMessage("Missing content type is currently not supported.");
        }

        [Fact]
        public void ServiceHandler__DoesNotThrowIfNoContentTypeAndNoBody()
        {
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);

            var context = BuildContext(routePath, service, typeof(DtoForParamRoute), string.Empty, contentType: null);

            Action act = () => Handler.ServiceHandler(context.Object).Wait(5000);

            act.ShouldNotThrow<Exception>();
        }

        [Fact]
        public void ServiceHandler__DoesNotThrowIfJsonAndBodyHasContent()
        {
            const string expectedValue = "Eats llamas";
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);
            var reqBody = $"{{\"{nameof(DtoForParamRoute.Foobar)}\":\"{expectedValue}\"}}";

            var context = BuildContext(routePath, service, typeof(DtoForParamRoute), reqBody);

            Action act = () => Handler.ServiceHandler(context.Object).Wait(5000);

            act.ShouldNotThrow<Exception>();
        }

        [Fact]
        public void ServiceHandler__DoesNotThrowIfEmptyBodyAndNotJson()
        {
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);
            const string reqBody = "";

            var context = BuildContext(routePath, service, typeof(DtoForParamRoute), reqBody, contentType: "text/plain");

            Action act = () => Handler.ServiceHandler(context.Object).Wait(5000);

            act.ShouldNotThrow<Exception>();
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

        [Fact]
        public void ServiceHandler__CreatesRequestDtoObjectFromUri_String()
        {
            const string expectedValue = "Eats llamas";
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePathUri.Substring(1);
            var routeValues = new Dictionary<string, object>
            {
                { nameof(DtoForParamRoute.Foobar), expectedValue }
            };

            var context = BuildContext(routePath, service, typeof(DtoForParamQueryRoute), routeTemplate: routePath, routeValues: routeValues);
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(expectedValue);
        }

        [Fact]
        public void ServiceHandler__CreatesRequestDtoObjectFromUri_Integer()
        {
            const int expectedValue = 100;
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePathUri.Substring(1);
            var routeValues = new Dictionary<string, object>
            {
                { nameof(DtoForParamRoute.Llama), expectedValue }
            };

            var context = BuildContext(routePath, service, typeof(DtoForParamQueryRoute), routeTemplate: routePath, routeValues: routeValues);
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be($"{expectedValue}");
        }

        [Fact]
        public void ServiceHandler__UriOverridesContextBody()
        {
            const string expectedValue = "Eats llamas";
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePathUri.Substring(1);
            var routeValues = new Dictionary<string, object>
            {
                { nameof(DtoForParamRoute.Foobar), expectedValue }
            };
            var reqBody = $"{{\"{nameof(DtoForParamRoute.Foobar)}\":\"Bar\"}}";

            var context = BuildContext(routePath, service, typeof(DtoForParamQueryRoute), routeTemplate: routePath, routeValues: routeValues, requestBody:reqBody);
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(expectedValue);
        }

        [Fact]
        public void ServiceHandler__CreatesRequestDtoObjectFromQueryString_String()
        {
            const string expectedValue = "FizzBuzz";
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);
            var query = new QueryCollection(new Dictionary<string, StringValues>
            {
                { nameof(DtoForParamRoute.Foobar), expectedValue }
            });

            var context = BuildContext(routePath, service, typeof(DtoForParamRoute), query: query);
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(expectedValue);
        }

        [Fact]
        public void ServiceHandler__CreatesRequestDtoObjectFromQueryString_Integer()
        {
            const string expectedValue = "100";
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);
            var query = new QueryCollection(new Dictionary<string, StringValues>
            {
                { nameof(DtoForParamRoute.Llama), expectedValue }
            });

            var context = BuildContext(routePath, service, typeof(DtoForParamRoute), query: query);
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be($"{expectedValue}");
        }

        [Fact]
        public void ServiceHandler__UriOverridesQueryString()
        {
            const string expectedValue = "Eats llamas";
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePathUri.Substring(1);
            var routeValues = new Dictionary<string, object>
            {
                { nameof(DtoForParamRoute.Foobar), expectedValue }
            };
            var query = new QueryCollection(new Dictionary<string, StringValues>
            {
                { nameof(DtoForParamRoute.Foobar), "FizzBuzz" }
            });

            var context = BuildContext(routePath, service, typeof(DtoForParamQueryRoute), routeTemplate: routePath, routeValues: routeValues, query: query);
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(expectedValue);
        }

        [Fact]
        public void ServiceHandler__SerializesResponseDto()
        {
            const string expectedValue = "FizzBuzz";
            var expectedBody = $"{{\"{nameof(RequestDto.Foobar)}\":\"{expectedValue}\"}}";
            var service = new SvcWithParamRoute();
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);
            var query = new QueryCollection(new Dictionary<string, StringValues>
            {
                { nameof(RequestDto.Foobar), expectedValue }
            });

            var context = BuildContext(routePath, service, 
                typeof(RequestDto), responseDto: typeof(ResponseDto), 
                query: query, method:"PUT");
            var body = context.Object.Response.Body;

            Handler.ServiceHandler(context.Object).Wait(5000);
            body.Position = 0;

            using (var reader = new StreamReader(body))
                reader.ReadToEnd().Should().Be(expectedBody);
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