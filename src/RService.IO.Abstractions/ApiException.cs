using System;
using System.Net;

namespace RService.IO.Abstractions
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; protected set; } = HttpStatusCode.InternalServerError;

        public ApiException() :
            base(string.Empty)
        {
        }

        public ApiException(string message)
            : base(message)
        {
        }

        public ApiException(HttpStatusCode statusCode)
            : base(string.Empty)
        {
            StatusCode = statusCode;
        }

        public ApiException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public ApiException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public ApiException(HttpStatusCode statusCode, Exception inner)
            : base(string.Empty, inner)
        {
            StatusCode = statusCode;
        }

        public ApiException(string message, HttpStatusCode statusCode, Exception inner)
            : base(message, inner)
        {
            StatusCode = statusCode;
        }
    }
}