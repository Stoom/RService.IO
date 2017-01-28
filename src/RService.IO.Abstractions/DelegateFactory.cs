using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RService.IO.Abstractions
{
    public sealed class DelegateFactory
    {
        /// <summary>
        /// Generates a dynamic method call.
        /// </summary>
        /// <param name="method">The <see cref="MethodInfo"/> of the method to call.</param>
        /// <returns>A <see cref="Delegate.Activator"/> delegate.</returns>
        /// <remarks>The results from this function should be cached.</remarks>
        public static Delegate.Activator GenerateMethodCall(MethodInfo method)
        {
            var methodType = method.DeclaringType;


            var instance = Expression.Parameter(typeof(object), "target");
            var arguments = Expression.Parameter(typeof(object[]), "arguments");

            Expression call;
            if (method.ReturnType != typeof(void))
            {
                call = Expression.Call(
                    Expression.Convert(instance, methodType),
                    method,
                    CreateParameterExpressions(method, arguments)
                );
            }
            else
            {
                // Constants
                var nullConst = Expression.Constant(null, typeof(object));

                // Return
                var returnTarget = Expression.Label(typeof(object), "Void return target");
                var returnLable = Expression.Label(returnTarget, nullConst);

                call = Expression.Block(
                    Expression.Call(
                        Expression.Convert(instance, methodType),
                        method,
                        CreateParameterExpressions(method, arguments)),
                    returnLable);
            }
            var lambda = Expression.Lambda<Delegate.Activator>(
                Expression.Convert(call, typeof(object)),
                instance,
                arguments);

            return lambda.Compile();
        }

        /// <summary>
        /// Generates a dynamic DTO.
        /// </summary>
        /// <param name="dtoType">The <see cref="Type"/> of the DTO to create.</param>
        /// <param name="deserializerMethod">The <see cref="MethodInfo"/> of the generic deserializer.</param>
        /// <returns>A <see cref="Delegate.DtoCtor"/> delegate.</returns>
        /// <remarks>The results from this function should be cached.</remarks>
        public static Delegate.DtoCtor GenerateDtoCtor(Type dtoType, MethodInfo deserializerMethod)
        {
            // Methods
            var deserializeMethod = deserializerMethod.MakeGenericMethod(dtoType);
            var dtoCtor = dtoType.GetConstructors().First();

            // Properties and fields
            var bodyParam = Expression.Parameter(typeof(string), "Body");
            var reqDtoVar = Expression.Variable(dtoType, "Request Dto");

            // Return
            var returnTarget = Expression.Label(dtoType);
            var returnExp = Expression.Return(returnTarget, reqDtoVar, dtoType);
            var returnLable = Expression.Label(returnTarget,
                Expression.Convert(Expression.Constant(null), dtoType));

            // Constants
            var nullConst = Expression.Constant(null);

            var callExpressions = new List<Expression>
            {
                // Deserialize or ctor
                Expression.Assign(reqDtoVar, Expression.Call(deserializeMethod, bodyParam)),
                Expression.IfThen(
                    Expression.Equal(reqDtoVar, nullConst),
                    Expression.Assign(reqDtoVar, Expression.New(dtoCtor))
                ),
                returnExp,
                returnLable
            };


            var call = Expression.Block(new[] { reqDtoVar }, callExpressions);

            var lambda = Expression.Lambda<Delegate.DtoCtor>(
                Expression.Convert(call, typeof(object)),
                bodyParam);

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