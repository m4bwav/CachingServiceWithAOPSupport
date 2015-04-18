using System.Runtime.Caching;

namespace CachingServiceWithAOP.Tests.Infrastructure
{
    public static class SimpleTestingHelpers
    {
        public static void ClearKeysFromMemcache()
        {
            foreach (var cacheItem in MemoryCache.Default)
            {
                MemoryCache.Default.Remove(cacheItem.Key);
            }
        }
    }
}