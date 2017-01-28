using System;
using Microsoft.AspNetCore.Http;

namespace RService.IO.Abstractions.Providers
{
    /// <summary>
    /// Describes how to hydrate (deserialize), and dehydrate (serialize)
    /// requests and response.
    /// </summary>
    public interface ISerializationProvider
    {
        /// <summary>
        /// The content type for serialization.
        /// </summary>
        string ContentType { get; }
        /// <summary>
        /// Hydrates a a request DTO.
        /// </summary>
        /// <param name="ctx">A <see cref="HttpContext"/> with the request to hydrate (deserialize).</param>
        /// <param name="dtoType">The <see cref="Type"/> to hydrate to.</param>
        /// <returns>A strongly typed object of the specified type.</returns>
        object HydrateRequest(HttpContext ctx, Type dtoType);
        /// <summary>
        /// Dehydrates the response DTO.
        /// </summary>
        /// <param name="resDto">The response DTO to dehydrate (serialize).</param>
        /// <returns>The string representing the serialized response.</returns>
        string DehydrateResponse(object resDto);
    }
}