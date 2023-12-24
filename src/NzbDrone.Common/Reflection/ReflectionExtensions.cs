using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NzbDrone.Common.Reflection
{
    public static class ReflectionExtensions
    {
        public static List<PropertyInfo> GetSimpleProperties(this Type type)
        {
            var properties = type.GetProperties();
            return properties.Where(c => c.PropertyType.IsSimpleType()).ToList();
        }

        public static List<Type> ImplementationsOf<T>()
        {
            return GetAllTypes().Where(c => typeof(T).IsAssignableFrom(c)).ToList();
        }

        public static bool IsSimpleType(this Type type)
        {
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>) ||
                                       type.GetGenericTypeDefinition() == typeof(List<>) ||
                                       type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                type = type.GetGenericArguments()[0];
            }

            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(DateTime)
                   || type == typeof(Version)
                   || type == typeof(decimal);
        }

        public static bool IsReadable(this PropertyInfo propertyInfo)
        {
            return propertyInfo.CanRead && propertyInfo.GetGetMethod(false) != null;
        }

        public static bool IsWritable(this PropertyInfo propertyInfo)
        {
            return propertyInfo.CanWrite && propertyInfo.GetSetMethod(false) != null;
        }

        public static T GetAttribute<T>(this MemberInfo member, bool isRequired = true)
            where T : Attribute
        {
            var attribute = member.GetCustomAttributes(typeof(T), false).SingleOrDefault();

            if (attribute == null && isRequired)
            {
                throw new ArgumentException(string.Format("The {0} attribute must be defined on member {1}", typeof(T).Name, member.Name));
            }

            return (T)attribute;
        }

        public static T[] GetAttributes<T>(this MemberInfo member)
            where T : Attribute
        {
            return member.GetCustomAttributes(typeof(T), false).OfType<T>().ToArray();
        }

        public static Type FindTypeByName(this Assembly assembly, string name)
        {
            return assembly.GetExportedTypes().SingleOrDefault(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Type FindTypeByName(string name)
        {
            return GetAllTypes()
                .SingleOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        private static IEnumerable<Type> GetAllTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            return assemblies
                .Where(x => ShouldUseAssembly(x))
                .SelectMany(x => x.GetExportedTypes());
        }

        private static bool ShouldUseAssembly(Assembly assembly)
        {
            if (assembly.IsDynamic)
            {
                return false;
            }

            var name = assembly.GetName();
            return name.Name == "Lidarr.Core" || name.Name.Contains("Lidarr.Plugin");
        }

        public static bool HasAttribute<TAttribute>(this Type type)
        {
            return type.GetCustomAttributes(typeof(TAttribute), true).Any();
        }
    }
}
