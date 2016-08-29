using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RService.IO
{
    public static class DelegateFactory
    {
        /// <summary>
        /// Activates method associated with a service.
        /// </summary>
        /// <param name="args">Arguments for method being activated.</param>
        /// <returns>The service's response.</returns>
        public delegate object Activator(object target, params object[] args);

        public static Activator GenerateMethodCall(MethodInfo method)
        {
            var methodType = method.DeclaringType;


            var instance = Expression.Parameter(typeof(object), "target");
            var arguments = Expression.Parameter(typeof(object[]), "arguments");
            var call = Expression.Call(
                Expression.Convert(instance, methodType),
                method,
                CreateParameterExpressions(method, arguments)
                );
            var lambda = Expression.Lambda<Activator>(
                Expression.Convert(call, typeof(object)),
                instance,
                arguments);

            return lambda.Compile();
        }

        private static Expression[] CreateParameterExpressions(MethodBase method, Expression argumentsParameter)
        {
            // ReSharper disable once CoVariantArrayConversion
            return method.GetParameters().Select((parameter, index) =>
                Expression.Convert(
                    Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)),
                    parameter.ParameterType)).ToArray();
        }
    }
}