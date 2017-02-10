using System;
using System.Collections.Generic;
using System.Reflection;
using RService.IO.Abstractions;
using RService.IO.Abstractions.Providers;

namespace RService.IO
{
    /// <summary>
    /// Options for routing service.
    /// </summary>
    public class RServiceOptions
    {
        /// <summary>
        /// Enables the UseDeveloperExceptionPage middleware.
        /// </summary>
        /// <remarks>
        /// Debugging is defaulted to <b>False</b> and also requires to be in <b>development</b> mode.
        /// </remarks>
        public bool EnableDebugging { get; set; }

        /// <summary>
        /// The exception filter called when an <see cref="Exception"/> is
        /// thrown in a <see cref="IService"/>.
        /// </summary>
        public IExceptionFilter ExceptionFilter { get; set; }

        /// <summary>
        /// Assemblies containing RServiceIO services.
        /// </summary>
        public List<Assembly> ServiceAssemblies { get; set; } = new List<Assembly>();

        public IDictionary<string, ISerializationProvider> SerializationProviders { get; set; } =
            new Dictionary<string, ISerializationProvider>();

        public ISerializationProvider DefaultSerializationProvider { get; set; }

        /// <summary>
        /// Adds an <see cref="Assembly"/> based on a service <see cref="Type"/>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> that contains a service.</param>
        public void AddServiceAssembly(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            var asm = serviceType.GetTypeInfo().Assembly;
            ServiceAssemblies.Add(asm);
        }
    }
}