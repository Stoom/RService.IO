using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using RService.IO.Abstractions;

namespace RService.IO
{
    public sealed class DelegateFactory
    {

        public static Delegate.Activator GenerateMethodCall(MethodInfo method)
        {
            var methodType = method.DeclaringType;


            var instance = Expression.Parameter(typeof(object), "target");
            var arguments = Expression.Parameter(typeof(object[]), "arguments");
            var call = Expression.Call(
                Expression.Convert(instance, methodType),
                method,
                CreateParameterExpressions(method, arguments)
                );
            var lambda = Expression.Lambda<Delegate.Activator>(
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