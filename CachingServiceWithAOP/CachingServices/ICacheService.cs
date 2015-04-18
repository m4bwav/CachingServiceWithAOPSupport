using System;
using Castle.DynamicProxy;

namespace CachingServiceWithAOP.CachingServices
{
    public interface ICacheService
    {
        T Get<T>(string key);
        T Get<T>(string key, Func<T> retrievalFunc);
        void Set<T>(string key, T value);
        void GetByInvocation(IInvocation invocation, TimeSpan? duration = null);
    }
}
