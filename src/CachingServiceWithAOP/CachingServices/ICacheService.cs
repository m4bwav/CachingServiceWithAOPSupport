using System;
using Castle.DynamicProxy;

namespace CachingServiceWithAOP.CachingServices
{
    /// <summary>A cache for values and for intercepted method results.</summary>
    public interface ICacheService
    {
        /// <summary>The value stored under <paramref name="key"/>, or the default of <typeparamref name="T"/> when there is none or it is of another type.</summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The value or the default.</returns>
        T? Get<T>(string key);

        /// <summary>The value stored under <paramref name="key"/>; when there is none, runs <paramref name="retrievalFunc"/> once, stores and returns its result.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="retrievalFunc">Produces the value on a miss.</param>
        /// <returns>The cached or new value.</returns>
        T? Get<T>(string key, Func<T> retrievalFunc);

        /// <summary>Stores <paramref name="value"/> under <paramref name="key"/> with the service's lifetime.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        void Set<T>(string key, T value);

        /// <summary>Answers an intercepted call from the cache, or proceeds and caches its result.</summary>
        /// <param name="invocation">The intercepted call.</param>
        /// <param name="duration">The result's lifetime, or null for the service's default.</param>
        void GetByInvocation(IInvocation invocation, TimeSpan? duration = null);
    }
}
