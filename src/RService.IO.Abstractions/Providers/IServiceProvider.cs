using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RService.IO.Abstractions
{
    /// <summary>
    /// Supports reading request and calling user service endpoints.
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// Invokes the user service endpoint.
        /// </summary>
        /// <param name="context">The current <see cref="HttpContext"/> for the request.</param>
        /// <returns>The results of processing the endpoint.</returns>
        Task Invoke(HttpContext context);
    }
}