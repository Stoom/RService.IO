﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using RService.IO.Handler;

namespace RService.IO
{
    /// <summary>
    /// Options for routing service.
    /// </summary>
    public class RServiceOptions
    {
        /// <summary>
        /// The <see cref="Microsoft.AspNetCore.Routing.RouteHandler"/> to process request routing.
        /// </summary>
        public RequestDelegate RouteHanlder { get; set; } = RServiceHandler.Handler;

        /// <summary>
        /// Assemblies containing RServiceIO services.
        /// </summary>
        public List<Assembly> ServiceAssemblies { get; set; } = new List<Assembly>();

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