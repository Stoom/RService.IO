using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RService.IO.Abstractions;

namespace RService.IO.Providers
{
    /// <summary>
    /// Default implementation of <see cref="IAuthProvider"/>.
    /// </summary>
    public class AuthProvider : IAuthProvider
    {
        /// <inheritdoc/>
        public bool IsAuthenticated(HttpContext ctx)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public bool IsAuthorized(ClaimsPrincipal user, IEnumerable<object> authorizationFilters)
        {
            throw new System.NotImplementedException();
        }
    }
}