using System;
using System.Reflection;

namespace CachingServiceWithAOP.Extensions
{
    public static class ReflectionExtensions
    {
        public static bool HasCacheAttribute(this MethodInfo method)
        {
            return Attribute.IsDefined(method, typeof(CacheAttribute));
        }
    }
}
