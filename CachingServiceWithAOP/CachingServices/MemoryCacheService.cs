using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using Castle.DynamicProxy;

namespace CachingServiceWithAOP.CachingServices
{
    public class MemoryCacheService : ICacheService
    {
        private static readonly IDictionary<string, object> CacheLock = new Dictionary<string, object>();
        private readonly ObjectCache _backingCache;
        private readonly TimeSpan _cacheTimeout;

        private readonly IKeyService _keyService = new DefaultCacheKeyService();

        public MemoryCacheService()
        {
            _cacheTimeout = new TimeSpan(0, 5, 0);

            _backingCache = MemoryCache.Default;
        }

        public MemoryCacheService(ObjectCache backingCache)
        {
            _backingCache = backingCache;
        }

        public MemoryCacheService(long absoluteTicks)
        {
            _cacheTimeout = new TimeSpan(absoluteTicks);

            _backingCache = MemoryCache.Default;
        }

        public MemoryCacheService(int absoluteHours, int absoluteMinutes, int absoluteSeconds)
        {
            _cacheTimeout = new TimeSpan(absoluteHours, absoluteMinutes, absoluteSeconds);
            _backingCache = MemoryCache.Default;
        }

        public MemoryCacheService(TimeSpan expiration)
        {
            _cacheTimeout = expiration;

            _backingCache = MemoryCache.Default;
        }


        public T Get<T>(string key)
        {
            var result = _backingCache.Get(key);

            return CastResultToTypeOrDefault<T>(result);
        }

        public T Get<T>(string key, Func<T> retrievalFunc)
        {
            var result = _backingCache.Get(key);

            if (result != null)
                return CastResultToTypeOrDefault<T>(result);

            var lockObj = GetOrCreateLockObjectForKey(key);

            lock (lockObj)
            {
                var postLockResult = _backingCache.Get(key);

                if (postLockResult != null)
                    return CastResultToTypeOrDefault<T>(postLockResult);

                var newResult = retrievalFunc();

                Set(key, newResult);

                return newResult;
            }
        }

        public void Set<T>(string key, T value)
        {
            var outTime = GenerateAbsoluteExpiration();

            _backingCache.Set(key, value, outTime);
        }

        public void GetByInvocation(IInvocation invocation, TimeSpan? duration = null)
        {
            var key = _keyService.GenerateUniqueKeyForCall(invocation);

            var result = _backingCache.Get(key);

            if (result != null)
            {
                invocation.ReturnValue = result;
                return;
            }

            invocation.Proceed();
            var value = invocation.ReturnValue;

            var outTime = GenerateAbsoluteExpiration(duration);

            _backingCache.Set(key, value, outTime);
        }

        private DateTimeOffset GenerateAbsoluteExpiration(TimeSpan? absoluteExpiration = null)
        {
            if (absoluteExpiration == null)
                absoluteExpiration = _cacheTimeout;

            return DateTimeOffset.UtcNow.Add(absoluteExpiration.Value);
        }

        private static T CastResultToTypeOrDefault<T>(object result)
        {
            if (!(result is T))
                return default(T);

            return (T) result;
        }

        private static object GetOrCreateLockObjectForKey(string key)
        {
            object lockObj;

            if (!CacheLock.TryGetValue(key, out lockObj))
            {
                CacheLock[key] = lockObj = new object();
            }
            return lockObj;
        }
    }
}