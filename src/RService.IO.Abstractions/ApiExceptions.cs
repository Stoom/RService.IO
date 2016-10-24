using System;
using System.Net;

namespace RService.IO.Abstractions
{
    public class ApiExceptions : Exception
    {
        public HttpStatusCode StatusCode { get; protected set; } = HttpStatusCode.InternalServerError;

        public ApiExceptions()
        {
        }

        public ApiExceptions(string message)
            : base(message)
        {
        }

        public ApiExceptions(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public ApiExceptions(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public ApiExceptions(string message, Exception inner)
            : base(message, inner)
        {
        }

        public ApiExceptions(HttpStatusCode statusCode, Exception inner)
            : base(string.Empty, inner)
        {
            StatusCode = statusCode;
        }

        public ApiExceptions(string message, HttpStatusCode statusCode, Exception inner)
            : base(message, inner)
        {
            StatusCode = statusCode;
        }
    }
}