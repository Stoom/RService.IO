using Microsoft.AspNetCore.Http;

namespace RService.IO.Abstractions
{
    /// <summary>
    /// Tagging interface for RService services.
    /// </summary>
    public abstract class ServiceBase
    {
        /// <summary>
        /// The context of the request.
        /// </summary>
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