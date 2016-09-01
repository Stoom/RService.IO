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
    }
}