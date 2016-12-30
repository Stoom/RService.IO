using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using RService.IO.Abstractions;

namespace RService.IO.Providers
{
    /// <summary>
    /// Default implementation of <see cref="IAuthProvider"/>.
    /// </summary>
    public class AuthProvider : IAuthProvider
    {
        private readonly IAuthorizationPolicyProvider _policyProvider;
        private readonly ConcurrentDictionary<string, IEnumerable<object>> _cachedAuthAttributes;

        /// <summary>
        /// Constructs the default auth provider.
        /// </summary>
        /// <param name="provider">
        /// The <see cref="IAuthorizationPolicyProvider"/> to check authorization to specified methods and classes.
        /// </param>
        public AuthProvider(IAuthorizationPolicyProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _policyProvider = provider;
            _cachedAuthAttributes = new ConcurrentDictionary<string, IEnumerable<object>>();
        }

        /// <inheritdoc/>
        public Task<bool> IsAuthorizedAsync(HttpContext ctx, ServiceMetadata metadata)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            IEnumerable<object> authenticationAttributes;

            if (!_cachedAuthAttributes.TryGetValue(metadata.Ident, out authenticationAttributes))
            {
                var serviceAuthAttributes = new List<object>();
                serviceAuthAttributes.AddRange(metadata.Service.GetCustomAttributes<AuthorizeAttribute>());
                serviceAuthAttributes.AddRange(metadata.Service.GetCustomAttributes<AllowAnonymousAttribute>());

                serviceAuthAttributes.AddRange(metadata.Method.GetCustomAttributes<AuthorizeAttribute>());
                serviceAuthAttributes.AddRange(metadata.Method.GetCustomAttributes<AllowAnonymousAttribute>());

                _cachedAuthAttributes.TryAdd(metadata.Ident, serviceAuthAttributes);
                authenticationAttributes = serviceAuthAttributes;
            }

            return IsAuthorizedAsync(ctx, authenticationAttributes);
        }

        /// <inheritdoc/>
        public async Task<bool> IsAuthorizedAsync(HttpContext ctx, IEnumerable<object> authorizationFilters)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));
            if (authorizationFilters == null)
                throw new ArgumentNullException(nameof(authorizationFilters));

            var authFilters = authorizationFilters.ToList();

            // Allow Anonymous skips all authorization
            if (authFilters.Any(x => x is IAllowAnonymous))
                return true;

            // Get the authorization policy
            var authData = authFilters.Where(x => x is IAuthorizeData).Cast<IAuthorizeData>().ToList();
            var effectivePolicy = await AuthorizationPolicy.CombineAsync(_policyProvider, authData);

            if (effectivePolicy == null)
                return true;

            // Build a ClaimsPrincipal with the policy's required authentication types
            if (effectivePolicy.AuthenticationSchemes?.Any() ?? false)
            {
                ClaimsPrincipal newPrincipal = null;
                foreach (var scheme in effectivePolicy.AuthenticationSchemes)
                {
                    var result = await ctx.Authentication.AuthenticateAsync(scheme);
                    if (result != null)
                    {
                        newPrincipal = SecurityHelper.MergeUserPrincipal(newPrincipal, result);
                    }
                }
                // If all schemes failed authentication, provide a default identity anyways
                ctx.User = newPrincipal ?? new ClaimsPrincipal(new ClaimsIdentity());
            }

            var authService = ctx.RequestServices.GetRequiredService<IAuthorizationService>();

            // Note: Default Anonymous User is new ClaimsPrincipal(new ClaimsIdentity())
            return await authService.AuthorizeAsync(ctx.User, effectivePolicy);
        }
    }
}