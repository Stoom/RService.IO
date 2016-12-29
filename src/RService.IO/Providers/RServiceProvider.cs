using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RService.IO.Abstractions;

namespace RService.IO.Providers
{
    /// <summary>
    /// Default implementation of the <see cref="IServiceProvider"/>
    /// </summary>
    public class RServiceProvider : IServiceProvider
    {
        private readonly ISerializationProvider _serializationProvider;

        /// <summary>
        /// Constructs a <see cref="RServiceProvider"/>.
        /// </summary>
        /// <param name="serializationProvider">The serialization provider.</param>
        public RServiceProvider(ISerializationProvider serializationProvider)
        {
            _serializationProvider = serializationProvider;
        }

        /// <inheritdoc/>
        public Task Invoke(HttpContext context)
        {
            var service = context.GetServiceInstance();
            var activator = context.GetServiceMethodActivator();
            var dtoReqType = context.GetRequestDtoType();
            var metadata = context.GetServiceMetadata();
            var args = new List<object>();


            var dto = _serializationProvider.HydrateRequest(context, dtoReqType);
            if (dto != null)
                args.Add(dto);

            var res = activator.Invoke(service, args.ToArray());

            if (ReferenceEquals(null, res))
                return context.Response.WriteAsync(string.Empty);
            if (res.IsSimple())
                return context.Response.WriteAsync(res.ToString());

            var serializedRes = _serializationProvider.DehydrateResponse(res);
            context.Request.ContentType = _serializationProvider.ContentType;
            return context.Response.WriteAsync(serializedRes);
        }
    }
}