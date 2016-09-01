using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RService.IO
{
    /// <summary>
    /// The RServiceIO route handlers.
    /// </summary>
    public class Handler
    {
        /// <summary>
        /// The default route handler that must be used with RService.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the request and response.</param>
        /// <returns>A <see cref="Task"/> for async calls.</returns>
        /// <remarks>
        /// Pre/post routing tasks can be expanded by extending 
        /// the <b>BeforeHandler</b> and <b>AfterHandler</b> methods.
        /// </remarks>
        public static Task ServiceHandler(HttpContext context)
        {
            throw new NotImplementedException();
        }
    }
}