using System;

namespace CachingServiceWithAOP
{
    public class CacheAttribute : Attribute
    {
        public CacheAttribute()
        {
        }

        public CacheAttribute(long ticks)
        {
            CacheTimeout = new TimeSpan(ticks);
        }


        public CacheAttribute(int hours, int minutes, int seconds)
        {
            CacheTimeout = new TimeSpan(hours, minutes, seconds);
        }

        public CacheAttribute(int days, int hours, int minutes, int seconds)
        {
            CacheTimeout = new TimeSpan(days, hours, minutes, seconds);
        }

        public TimeSpan? CacheTimeout { get; set; }
    }
}