using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace CachingServiceWithAOP.CachingServices
{
    /// <summary>
    /// An <see cref="ICacheService"/> over System.Runtime.Caching. Every instance made without an <see cref="ObjectCache"/>
    /// shares <see cref="MemoryCache.Default"/>, and entries expire at an absolute time: five minutes after they are stored
    /// unless a lifetime is given. A lifetime of zero or less stores nothing retrievable; one past the end of time never expires.
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private static readonly TimeSpan DefaultTimeout = new(0, 5, 0);

        // One lock object per key while a value is being computed; removed once it is stored, so the table stays small.
        private static readonly ConcurrentDictionary<string, object> KeyLocks = new(StringComparer.Ordinal);

        private readonly ObjectCache _backingCache;
        private readonly TimeSpan _cacheTimeout;

        private readonly DefaultCacheKeyService _keyService = new();

        /// <summary>Uses <see cref="MemoryCache.Default"/> and a five-minute lifetime.</summary>
        public MemoryCacheService()
        {
            _cacheTimeout = DefaultTimeout;

            _backingCache = MemoryCache.Default;
        }

        /// <summary>Uses the given cache and a five-minute lifetime (1.x used a lifetime of zero here, so it kept nothing).</summary>
        /// <param name="backingCache">The cache to store in.</param>
        public MemoryCacheService(ObjectCache backingCache)
        {
            _cacheTimeout = DefaultTimeout;

            _backingCache = backingCache;
        }

        /// <summary>Uses <see cref="MemoryCache.Default"/> and a lifetime in 100-nanosecond ticks.</summary>
        /// <param name="absoluteTicks">The lifetime in ticks.</param>
        public MemoryCacheService(long absoluteTicks)
        {
            _cacheTimeout = new TimeSpan(absoluteTicks);

            _backingCache = MemoryCache.Default;
        }

        /// <summary>Uses <see cref="MemoryCache.Default"/> and a lifetime in hours, minutes and seconds.</summary>
        /// <param name="absoluteHours">Hours.</param>
        /// <param name="absoluteMinutes">Minutes.</param>
        /// <param name="absoluteSeconds">Seconds.</param>
        public MemoryCacheService(int absoluteHours, int absoluteMinutes, int absoluteSeconds)
        {
            _cacheTimeout = new TimeSpan(absoluteHours, absoluteMinutes, absoluteSeconds);
            _backingCache = MemoryCache.Default;
        }

        /// <summary>Uses <see cref="MemoryCache.Default"/> and the given lifetime.</summary>
        /// <param name="expiration">The lifetime.</param>
        public MemoryCacheService(TimeSpan expiration)
        {
            _cacheTimeout = expiration;

            _backingCache = MemoryCache.Default;
        }

        /// <inheritdoc />
        public T? Get<T>(string key)
        {
            var result = _backingCache.Get(key);

            return CastResultToTypeOrDefault<T>(result);
        }

        /// <inheritdoc />
        /// <remarks>
        /// A null result is cached too. A value of another type under the key counts as a miss and is replaced
        /// (1.x returned the default without running <paramref name="retrievalFunc"/>). Concurrent callers for one key run
        /// <paramref name="retrievalFunc"/> once.
        /// </remarks>
        public T? Get<T>(string key, Func<T> retrievalFunc)
        {
            var result = _backingCache.Get(key);

            if (IsHit<T>(result))
            {
                return CastResultToTypeOrDefault<T>(result);
            }

            var lockObj = KeyLocks.GetOrAdd(key, _ => new object());

            lock (lockObj)
            {
                try
                {
                    var postLockResult = _backingCache.Get(key);

                    if (IsHit<T>(postLockResult))
                    {
                        return CastResultToTypeOrDefault<T>(postLockResult);
                    }

                    var newResult = retrievalFunc();

                    Store(key, newResult, null);

                    return newResult;
                }
                finally
                {
                    ReleaseLock(key, lockObj);
                }
            }
        }

        /// <inheritdoc />
        /// <remarks>A null <paramref name="value"/> throws ArgumentNullException, as in 1.x.</remarks>
        public void Set<T>(string key, T value)
        {
            var outTime = GenerateAbsoluteExpiration();

            _backingCache.Set(key, value, outTime);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Void and ValueTask-returning methods always proceed and are never cached. A null result is cached. A returned Task
        /// is cached while it succeeds and evicted when it faults or is cancelled. When no key can be made from the arguments
        /// (a reference cycle, for example), the call proceeds without caching. Concurrent calls with one key run the method once.
        /// </remarks>
        public void GetByInvocation(IInvocation invocation, TimeSpan? duration = null)
        {
            var returnType = invocation.Method.ReturnType;

            if (returnType == typeof(void) || IsValueTask(returnType))
            {
                invocation.Proceed();
                return;
            }

            string key;
            try
            {
                key = _keyService.GenerateUniqueKeyForCall(invocation);
            }
            catch (Exception e) when (ScriptJson.IsUncacheable(e))
            {
                invocation.Proceed();
                return;
            }

            if (TryAnswer(invocation, key))
            {
                return;
            }

            var lockObj = KeyLocks.GetOrAdd(key, _ => new object());

            lock (lockObj)
            {
                try
                {
                    if (TryAnswer(invocation, key))
                    {
                        return;
                    }

                    invocation.Proceed();
                    var value = invocation.ReturnValue;

                    if (value is Task task)
                    {
                        StoreTask(key, task, duration);
                    }
                    else
                    {
                        Store(key, value, duration);
                    }
                }
                finally
                {
                    ReleaseLock(key, lockObj);
                }
            }
        }

        private bool TryAnswer(IInvocation invocation, string key)
        {
            var result = _backingCache.Get(key);

            if (result == null)
            {
                return false;
            }

            invocation.ReturnValue = result is NullValue ? null : result;
            return true;
        }

        private void StoreTask(string key, Task task, TimeSpan? duration)
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                return;
            }

            Store(key, task, duration);

            // Always: a task that faults between the check above and here must still be evicted; on a completed task the
            // continuation runs at once.
            task.ContinueWith(
                t =>
                {
                    if ((t.IsFaulted || t.IsCanceled) && ReferenceEquals(_backingCache.Get(key), t))
                    {
                        _backingCache.Remove(key);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private void Store(string key, object? value, TimeSpan? duration)
        {
            _backingCache.Set(key, value ?? NullValue.Instance, GenerateAbsoluteExpiration(duration));
        }

        private DateTimeOffset GenerateAbsoluteExpiration(TimeSpan? absoluteExpiration = null)
        {
            var lifetime = absoluteExpiration ?? _cacheTimeout;
            var now = DateTimeOffset.UtcNow;

            // 1.x threw ArgumentOutOfRangeException here, after the intercepted method had run.
            if (lifetime > DateTimeOffset.MaxValue - now)
            {
                return ObjectCache.InfiniteAbsoluteExpiration;
            }

            if (lifetime < DateTimeOffset.MinValue - now)
            {
                return now;
            }

            return now.Add(lifetime);
        }

        // A cached null answers only a caller whose type can hold null; for a value type it is a miss.
        private static bool IsHit<T>(object? result)
        {
            return result is NullValue ? default(T) is null : result is T;
        }

        private static T? CastResultToTypeOrDefault<T>(object? result)
        {
            if (result is NullValue || result is not T typed)
            {
                return default;
            }

            return typed;
        }

        private static void ReleaseLock(string key, object lockObj)
        {
            // Removes the entry only if it is still this lock; ConcurrentDictionary does that atomically through ICollection.
            ((ICollection<KeyValuePair<string, object>>)KeyLocks).Remove(new KeyValuePair<string, object>(key, lockObj));
        }

        private static bool IsValueTask(Type type)
        {
            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            return definition.FullName == "System.Threading.Tasks.ValueTask" || definition.FullName == "System.Threading.Tasks.ValueTask`1";
        }

        internal static int PendingLockCount => KeyLocks.Count;

        // Stands for a cached null, which ObjectCache cannot hold.
        private sealed class NullValue
        {
            public static readonly NullValue Instance = new();
        }
    }
}
