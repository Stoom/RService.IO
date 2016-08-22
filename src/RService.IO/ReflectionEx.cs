using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RService.IO
{
    /// <summary>
    /// Additional reflection commands use for working with RServiceIO.
    /// </summary>
    public static class ReflectionEx
    {
        /// <summary>
        /// Checks if an attribute is associated with a type.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute to search for.</typeparam>
        /// <param name="type">The type to search in.</param>
        /// <param name="inherit">If the attribute should search for inherited attributes.</param>
        /// <returns><b>True</b> if the attribute is associated with the type, otherwise <b>False</b>.</returns>
        public static bool HasAttribute<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
        {
            return type.GetTypeInfo().GetCustomAttributes<TAttribute>(inherit).Any();
        }

        /// <summary>
        /// Checks if an attribute is associated with a method.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute to search for.</typeparam>
        /// <param name="method">The method to search in.</param>
        /// <param name="inherit">If the attribute should search for inherited attributes.</param>
        /// <returns><b>True</b> if the attribute is associated with the method, otherwise <b>False</b>.</returns>
        public static bool HasAttribute<TAttribute>(this MethodInfo method, bool inherit = false)
            where TAttribute : Attribute
        {
            return method.GetCustomAttributes<TAttribute>(inherit).Any();
        }

        /// <summary>
        /// Checks if an attribute is associated with the first parameter type of a method.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute to search for.</typeparam>
        /// <param name="method">The method to search in.</param>
        /// <param name="inherit">If the attribute should search for inherited attributes.</param>
        /// <returns><b>True</b> if the attribute is associated with the method's first parameter, otherwise <b>False</b>.</returns>
        public static bool HasParamWithAttribute<TAttribute>(this MethodInfo method, bool inherit = false) where TAttribute : Attribute
        {
            return method.GetParameters().FirstOrDefault(x => x.ParameterType.HasAttribute<TAttribute>(inherit)) != null;
        }

        /// <summary>
        /// Gets the attribute associated with a type.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute to get.</typeparam>
        /// <param name="type">The type to search in.</param>
        /// <param name="inherit">If the attribute should search for inherited attributes.</param>
        /// <returns>The attribute associated with the type.</returns>
        public static IEnumerable<Attribute> GetAttributes<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
        {
            return type.GetTypeInfo().GetCustomAttributes<TAttribute>(inherit);
        }

        /// <summary>
        /// Gets all public instance methods associated with a type.
        /// </summary>
        /// <param name="type">The type to get public instance methods from.</param>
        /// <returns><see cref="Array"/> of <see cref="MethodInfo"/>.</returns>
        public static MethodInfo[] GetPublicMethods(this Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Gets the method's first parameter type if it has the associated attribute.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute to search for.</typeparam>
        /// <param name="method">The method to search in.</param>
        /// <param name="inherit">If the attribute should search for inherited attributes.</param>
        /// <returns>The <see cref="Type"/> of the first parameter that is associated with the attribute.</returns>
        public static Type GetParamWithAttribute<TAttribute>(this MethodInfo method, bool inherit = false)
            where TAttribute : Attribute
        {
            return method.GetParameters().FirstOrDefault(x => x.ParameterType.HasAttribute<TAttribute>(inherit))?.ParameterType;
        }

        /// <summary>
        /// Checks if a <see cref="Type"/> implements a specified interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface to search for.</typeparam>
        /// <param name="type">The type to search in.</param>
        /// <param name="isAbstract">If it should also include abstract types.</param>
        /// <returns><b>True</b> if the type implements the interface, otherwise <b>False</b>.</returns>
        public static bool ImplementsInterface<TInterface>(this Type type, bool isAbstract = false)
        {
            var info = type.GetTypeInfo();
            return info.IsClass && info.IsPublic && info.IsAbstract == isAbstract &&
                   info.GetInterfaces().Any(i => i == typeof(TInterface));
        }
    }
}