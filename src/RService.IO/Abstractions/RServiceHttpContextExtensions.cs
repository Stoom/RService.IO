using System;
using Microsoft.AspNetCore.Http;

namespace RService.IO.Abstractions
{
    public static class RServiceHttpContextExtensions
    {
        public static Type GetRequestDtoType(this HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var feature = context.Features[typeof(IRServiceFeature)] as RServiceFeature;
            return feature?.RequestDtoType;
        }

        public static Delegate.Activator GetServiceMethodActivator(this HttpContext context)
        {

            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var feature = context.Features[typeof(IRServiceFeature)] as RServiceFeature;
            return feature?.MethodActivator;
        }

        public static IService GetServiceInstance(this HttpContext context)
        {

            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var feature = context.Features[typeof(IRServiceFeature)] as RServiceFeature;
            return feature?.Service;
        }
    }
}