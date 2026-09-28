using System;
using System.Reflection;

namespace CachingServiceWithAOP.Extensions
{
    /// <summary>Reflection helpers.</summary>
    public static class ReflectionExtensions
    {
        /// <summary>Whether the method, or a method it overrides, carries <see cref="CacheAttribute"/>.</summary>
        /// <param name="method">The method.</param>
        /// <returns>True when marked.</returns>
        public static bool HasCacheAttribute(this MethodInfo method)
        {
            return Attribute.IsDefined(method, typeof(CacheAttribute));
        }
    }
}
