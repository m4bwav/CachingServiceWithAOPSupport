using System;

namespace CachingServiceWithAOP
{
    /// <summary>
    /// Marks a method whose results <see cref="AOPCachingInterceptor"/> caches. Put it on the implementing class's method,
    /// not on the interface: the interceptor reads the attribute from the invocation target. Without a lifetime the cache
    /// service's own lifetime applies (five minutes for <see cref="CachingServices.MemoryCacheService"/>); a lifetime of zero
    /// or less means "do not keep the result".
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)] // 1.x's implicit defaults, written out
    public class CacheAttribute : Attribute
    {
        /// <summary>Caches with the cache service's default lifetime.</summary>
        public CacheAttribute()
        {
        }

        /// <summary>Caches for <paramref name="ticks"/> ticks of 100 nanoseconds (10 000 000 ticks are one second).</summary>
        /// <param name="ticks">The lifetime in 100-nanosecond ticks.</param>
        public CacheAttribute(long ticks)
        {
            CacheTimeout = new TimeSpan(ticks);
        }

        /// <summary>Caches for the given hours, minutes and seconds.</summary>
        /// <param name="hours">Hours.</param>
        /// <param name="minutes">Minutes.</param>
        /// <param name="seconds">Seconds.</param>
        public CacheAttribute(int hours, int minutes, int seconds)
        {
            CacheTimeout = new TimeSpan(hours, minutes, seconds);
        }

        /// <summary>Caches for the given days, hours, minutes and seconds.</summary>
        /// <param name="days">Days.</param>
        /// <param name="hours">Hours.</param>
        /// <param name="minutes">Minutes.</param>
        /// <param name="seconds">Seconds.</param>
        public CacheAttribute(int days, int hours, int minutes, int seconds)
        {
            CacheTimeout = new TimeSpan(days, hours, minutes, seconds);
        }

        /// <summary>The lifetime of a cached result, or null for the cache service's default.</summary>
        public TimeSpan? CacheTimeout { get; set; }
    }
}
