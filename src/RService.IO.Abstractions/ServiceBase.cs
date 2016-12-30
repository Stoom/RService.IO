using Microsoft.AspNetCore.Http;

namespace RService.IO.Abstractions
{
    /// <summary>
    /// Abstract implementation of the RService <see cref="IService"/> interface
    /// providing easy access to the context, request context, request method,
    /// and response context.
    /// </summary>
    public abstract class ServiceBase : IService
    {
        /// <inheritdoc/>>
        public HttpContext Context { get; set; }

        /// <summary>
        /// The request.
        /// </summary>
        public HttpRequest Request => Context.Request;

        /// <summary>
        /// The method of the request.
        /// </summary>
        public RestVerbs RequestMethod => Context.Request.Method.ParseRestVerb();

        /// <summary>
        /// The response.
        /// </summary>
        public HttpResponse Response => Context.Response;
    }
}