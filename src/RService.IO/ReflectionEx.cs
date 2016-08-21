using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RService.IO
{
    public static class ReflectionEx
    {
        public static bool HasAttribute<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
        {
            return type.GetTypeInfo().GetCustomAttributes<TAttribute>(inherit).Any();
        }

        public static bool HasAttribute<TAttribute>(this MethodInfo method, bool inherit = false)
            where TAttribute : Attribute
        {
            return method.GetCustomAttributes<TAttribute>(inherit).Any();
        }

        public static bool HasParamWithAttribute<TAttribute>(this MethodInfo method, bool inherit = false) where TAttribute : Attribute
        {
            return method.GetParameters().FirstOrDefault(x => x.ParameterType.HasAttribute<TAttribute>(inherit)) != null;
        }

        public static IEnumerable<Attribute> GetAttributes<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
        {
            return type.GetTypeInfo().GetCustomAttributes<TAttribute>(inherit);
        }

        public static MethodInfo[] GetPublicMethods(this Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        }

        public static Type GetParamWithAttribute<TAttribute>(this MethodInfo method, bool inherit = false)
            where TAttribute : Attribute
        {
            return method.GetParameters().FirstOrDefault(x => x.ParameterType.HasAttribute<TAttribute>(inherit))?.ParameterType;
        }

        public static bool ImplementsInterface<TInterface>(this Type type, bool isAbstract = false)
        {
            var info = type.GetTypeInfo();
            return info.IsClass && info.IsPublic && info.IsAbstract == isAbstract &&
                   info.GetInterfaces().Any(i => i == typeof(TInterface));
        }
    }
}