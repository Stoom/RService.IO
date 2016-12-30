using System;
using Microsoft.AspNetCore.Http;

namespace RService.IO.Abstractions
{
    /// <summary>
    /// An interface describing an exception filter for <see cref="Exception"/>s 
    /// thrown from the service.
    /// </summary>
    public interface IExceptionFilter
    {
        /// <summary>
        /// The method called when an exception has been raised.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the request.</param>
        /// <param name="exc">the <see cref="Exception"/> thrown by the service.</param>
        void OnException(HttpContext context, Exception exc);
    }
}