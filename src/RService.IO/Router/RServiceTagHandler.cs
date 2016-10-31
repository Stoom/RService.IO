using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RService.IO.Router
{
    /// <summary>
    /// Tagging routing handler for RService endpoints.
    /// </summary>
    public static class RServiceTagHandler
    {
        /// <summary>
        /// The tagging handler for RService endpoints. 
        /// !!!THIS SHOULD NEVER BE CALLED!!!
        /// </summary>
        /// <param name="ctx">Not used.</param>
        /// <returns>Not used.</returns>
        // ReSharper disable once UnusedParameter.Global
        public static Task Tag(HttpContext ctx)
        {
            throw new NotSupportedException("This is only a tagging method.");
        }
    }
}