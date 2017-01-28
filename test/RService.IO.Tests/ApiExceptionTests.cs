using System;
using System.Net;
using FluentAssertions;
using RService.IO.Abstractions;
using Xunit;

namespace RService.IO.Tests
{
    public class ApiExceptionTests
    {
        [Fact]
        public void Ctor__500WithNoMessage()
        {
            var exc = new ApiException();

            exc.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            exc.Message.Should().BeEmpty();
        }

        [Fact]
        public void Ctor_Message__500WithMessage()
        {
            const string expectedMessage = "Foobar";
            var exc = new ApiException(expectedMessage);

            exc.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            exc.Message.Should().Be(expectedMessage);
        }

        [Fact]
        public void Ctor_Status__StatusWithNoMessage()
        {
            const HttpStatusCode expectedStatus = HttpStatusCode.Forbidden;
            var exc = new ApiException(expectedStatus);

            exc.StatusCode.Should().Be(expectedStatus);
            exc.Message.Should().BeEmpty();
        }

        [Fact]
        public void Ctor_MessageAndStatus__StatusWithMessage()
        {
            const HttpStatusCode expectedStatus = HttpStatusCode.Forbidden;
            const string expectedMessage = "Foobar";
            var exc = new ApiException(expectedMessage, expectedStatus);

            exc.StatusCode.Should().Be(expectedStatus);
            exc.Message.Should().Be(expectedMessage);
        }

        [Fact]
        public void Ctor_MessageAndInner__500WithMessageAndInnerExc()
        {
            const string expectedMessage = "Foobar";
            var inner = new Exception();
            var exc = new ApiException(expectedMessage, inner);

            exc.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            exc.Message.Should().Be(expectedMessage);
            exc.InnerException.Should().Be(inner);
        }

        [Fact]
        public void Ctor_StatusAndInner__StatisWithNoMessageAndInnerExc()
        {
            const HttpStatusCode expectedStatus = HttpStatusCode.Forbidden;
            var inner = new Exception();
            var exc = new ApiException(expectedStatus, inner);

            exc.StatusCode.Should().Be(expectedStatus);
            exc.Message.Should().BeEmpty();
            exc.InnerException.Should().Be(inner);
        }

        [Fact]
        public void Ctor_MessageStatusAndInner__StatisWithMessageAndInnerExc()
        {
            const string expectedMessage = "Foobar";
            const HttpStatusCode expectedStatus = HttpStatusCode.Forbidden;
            var inner = new Exception();
            var exc = new ApiException(expectedMessage, expectedStatus, inner);

            exc.StatusCode.Should().Be(expectedStatus);
            exc.Message.Should().Be(expectedMessage);
            exc.InnerException.Should().Be(inner);
        }
    }
}
