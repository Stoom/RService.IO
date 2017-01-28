using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using RService.IO.Abstractions;
using RService.IO.Providers;
using Xunit;
using FluentAssertions;
using RService.IO.Abstractions.Providers;

namespace RService.IO.Tests.Providers
{
    public class NetJsonProviderTests
    {
        private readonly ISerializationProvider _provider;

        public NetJsonProviderTests()
        {
            _provider = new NetJsonProvider();
        }

        [Fact]
        public void HydrateRequest__ThrowsNotImplementedIfNotJsonAndBodyHasContent()
        {
            var context = BuildContext("FizzBuzzing", contentType: "text/plain");

            Action act = () => _provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            act.ShouldThrow<NotImplementedException>().WithMessage("text/plain is currently not supported.");
        }

        [Fact]
        public void HydrateRequest__ThrowsNotImplementedIfNoContentTypeAndBodyHasContent()
        {
            var context = BuildContext("Hello World", contentType: null);

            Action act = () => _provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            act.ShouldThrow<NotImplementedException>().WithMessage("Missing content type is currently not supported.");
        }

        [Fact]
        public void HydrateRequest__DoesNotThrowIfNoContentTypeAndNoBody()
        {
            var context = BuildContext(string.Empty, contentType: null);

            Action act = () => _provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            act.ShouldNotThrow<Exception>();
        }

        [Fact]
        public void HydrateRequest__DoesNotThrowIfJsonAndBodyHasContent()
        {
            const string expectedValue = "Eats llamas";
            var reqBody = $"{{\"{nameof(DtoForParamRoute.Foobar)}\":\"{expectedValue}\"}}";

            var context = BuildContext(reqBody);

            Action act = () => _provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            act.ShouldNotThrow<Exception>();
        }

        [Fact]
        public void HydrateRequest__DoesNotThrowIfEmptyBodyAndNotJson()
        {
            const string reqBody = "";

            var context = BuildContext(reqBody, contentType: "text/plain");

            Action act = () => _provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            act.ShouldNotThrow<Exception>();
        }

        [Fact]
        public void HydrateRequest__CreatesRequestDtoObjectFromContextBody()
        {
            const string expectedValue = "Eats llamas";
            var reqBody = $"{{\"{nameof(DtoForParamRoute.Foobar)}\":\"{expectedValue}\"}}";

            var context = BuildContext(reqBody);

            var dto = (DtoForParamRoute) _provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            dto.Should().BeOfType<DtoForParamRoute>();
            dto.Foobar.Should().Be(expectedValue);
        }

        [Fact]
        public void HydrateRequest__CreatesRequestDtoObjectFromUri_String()
        {
            const string expectedValue = "Eats llamas";
            var routePath = SvcWithParamRoute.RoutePathUri.Substring(1);
            var routeValues = new Dictionary<string, object>
                    {
                        { nameof(DtoForParamRoute.Foobar), expectedValue }
                    };

            var context = BuildContext(routeTemplate: routePath, routeValues: routeValues);

            var dto = (DtoForParamRoute)_provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            dto.Should().BeOfType<DtoForParamRoute>();
            dto.Foobar.Should().Be(expectedValue);
        }

        [Fact]
        public void HydrateRequest__CreatesRequestDtoObjectFromUri_Integer()
        {
            const int expectedValue = 100;
            var routePath = SvcWithParamRoute.RoutePathUri.Substring(1);
            var routeValues = new Dictionary<string, object>
                    {
                        { nameof(DtoForParamRoute.Llama), expectedValue }
                    };

            var context = BuildContext(routeTemplate: routePath, routeValues: routeValues);

            var dto = (DtoForParamRoute)_provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            dto.Should().BeOfType<DtoForParamRoute>();
            dto.Llama.Should().Be(expectedValue);
        }

        [Fact]
        public void HydrateRequest__UriOverridesContextBody()
        {
            const string expectedValue = "Eats llamas";
            var routePath = SvcWithParamRoute.RoutePathUri.Substring(1);
            var routeValues = new Dictionary<string, object>
                    {
                        { nameof(DtoForParamRoute.Foobar), expectedValue }
                    };
            var reqBody = $"{{\"{nameof(DtoForParamRoute.Foobar)}\":\"Bar\"}}";

            var context = BuildContext(routeTemplate: routePath, routeValues: routeValues, requestBody: reqBody);

            var dto = (DtoForParamRoute)_provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            dto.Should().BeOfType<DtoForParamRoute>();
            dto.Foobar.Should().Be(expectedValue);
        }

        [Fact]
        public void HydrateRequest__CreatesRequestDtoObjectFromQueryString_String()
        {
            const string expectedValue = "FizzBuzz";
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);
            var query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(DtoForParamRoute.Foobar), expectedValue }
                    });

            var context = BuildContext(routePath, query: query);

            var dto = (DtoForParamRoute)_provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            dto.Should().BeOfType<DtoForParamRoute>();
            dto.Foobar.Should().Be(expectedValue);
        }

        [Fact]
        public void HydrateRequest__CreatesRequestDtoObjectFromQueryString_Integer()
        {
            const string expectedValue = "100";
            var routePath = SvcWithParamRoute.RoutePath.Substring(1);
            var query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(DtoForParamRoute.Llama), expectedValue }
                    });

            var context = BuildContext(routePath, query: query);

            var dto = (DtoForParamRoute)_provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            dto.Should().BeOfType<DtoForParamRoute>();
            dto.Llama.Should().Be(int.Parse(expectedValue));
        }

        [Fact]
        public void HydrateRequest__UriOverridesQueryString()
        {
            const string expectedValue = "Eats llamas";
            var routePath = SvcWithParamRoute.RoutePathUri.Substring(1);
            var routeValues = new Dictionary<string, object>
                    {
                        { nameof(DtoForParamRoute.Foobar), expectedValue }
                    };
            var query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(DtoForParamRoute.Foobar), "FizzBuzz" }
                    });

            var context = BuildContext(routeTemplate: routePath, routeValues: routeValues, query: query);

            var dto = (DtoForParamRoute)_provider.HydrateRequest(context.Object, typeof(DtoForParamRoute));

            dto.Should().BeOfType<DtoForParamRoute>();
            dto.Foobar.Should().Be(expectedValue);
        }

        [Fact]
        public void DehydrateResponse__ReturnsCorrectlyTypedObject()
        {
            var dto = new DtoForParamRoute {Foobar = "FizzBuzz", Llama = 299};

            var expected = $"{{\"{nameof(dto.Foobar)}\":\"{dto.Foobar}\",\"{nameof(dto.Llama)}\":{dto.Llama}}}";
            var results = _provider.DehydrateResponse(dto);

            results.Should().Be(expected);
        }

        private static Mock<HttpContext> BuildContext(string requestBody = "", string routeTemplate = "",
            string contentType = "application/json", string method = "GET", 
            Dictionary<string, object> routeValues = null, IQueryCollection query = null)
        {
            var context = new Mock<HttpContext>().SetupAllProperties();
            var request = new Mock<HttpRequest>().SetupAllProperties();
            var response = new Mock<HttpResponse>().SetupAllProperties();
            var reqBody = new MemoryStream(Encoding.ASCII.GetBytes(requestBody));
            var resBody = new MemoryStream();
            var features = new Mock<IFeatureCollection>().SetupAllProperties();
            var routingFeature = new RoutingFeature();

            features.Setup(x => x[typeof(IRoutingFeature)]).Returns(routingFeature);

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