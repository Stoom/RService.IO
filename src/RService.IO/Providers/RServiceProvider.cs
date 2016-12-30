using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RService.IO.Abstractions;
using IServiceProvider = RService.IO.Abstractions.IServiceProvider;

namespace RService.IO.Providers
{
    /// <summary>
    /// Default implementation of the <see cref="Abstractions.IServiceProvider"/>
    /// </summary>
    public class RServiceProvider : IServiceProvider
    {
        private readonly ISerializationProvider _serializationProvider;
        private readonly IAuthProvider _authProvider;

        /// <summary>
        /// Constructs a <see cref="RServiceProvider"/>.
        /// </summary>
        /// <param name="serializationProvider">The serialization provider.</param>
        /// <param name="authProvider">The auth provider. (Optional)</param>
        public RServiceProvider(ISerializationProvider serializationProvider, IAuthProvider authProvider = null)
        {
            _serializationProvider = serializationProvider;
            _authProvider = authProvider;
        }

        /// <inheritdoc/>
        public async Task Invoke(HttpContext context)
        {
            var service = context.GetServiceInstance();
            var activator = context.GetServiceMethodActivator();
            var dtoReqType = context.GetRequestDtoType();
            var metadata = context.GetServiceMetadata();
            var args = new List<object>();

            if (_authProvider != null && !await _authProvider.IsAuthorizedAsync(context, metadata))
            {
                throw new ApiException(HttpStatusCode.Forbidden);
            }

            var dto = _serializationProvider.HydrateRequest(context, dtoReqType);
            if (dto != null)
                args.Add(dto);

            var res = activator.Invoke(service, args.ToArray());

            if (ReferenceEquals(null, res))
            { 
                await context.Response.WriteAsync(string.Empty);
                return;
            }
            if (res.IsSimple())
            {
                await context.Response.WriteAsync(res.ToString());
                return;
            }

            var serializedRes = _serializationProvider.DehydrateResponse(res);
            context.Request.ContentType = _serializationProvider.ContentType;
            await context.Response.WriteAsync(serializedRes);
        }
    }
}