using System;
using System.Reflection;
using RService.IO.Abstractions;
using Delegate = RService.IO.Abstractions.Delegate;

namespace RService.IO
{
    /// <summary>
    /// Defines a RService.IO web service.
    /// </summary>
    public struct ServiceDef
    {
        /// <summary>
        /// The route.
        /// </summary>
        public RouteAttribute Route;
        /// <summary>
        /// The service type.
        /// </summary>
        public Type ServiceType;
        /// <summary>
        /// The service method activator.
        /// </summary>
        public Delegate.Activator ServiceMethod;
        /// <summary>
        /// The <see cref="Type"/> of the request dto.
        /// </summary>
        public Type RequestDtoType;
        /// <summary>
        /// The <see cref="Type"/> of the response dto.
        /// </summary>
        public Type ResponseDtoType;
        /// <summary>
        /// Metadata associated with the service and method.
        /// </summary>
        public ServiceMetadata Metadata;
    }
}