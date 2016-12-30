using Microsoft.AspNetCore.Http;

namespace RService.IO.Abstractions
{
    /// <summary>
    /// The interface describing a RService.IO service.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// The context of the request.
        /// </summary>
        HttpContext Context { get; set; }
    }
}