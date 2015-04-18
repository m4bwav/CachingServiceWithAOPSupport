using System;
using System.Linq;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Extensions;
using Castle.DynamicProxy;

namespace CachingServiceWithAOP
{
    public class AOPCachingInterceptor : IInterceptor
    {
        private readonly ICacheService _cacheService;

        public AOPCachingInterceptor(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public void Intercept(IInvocation invocation)
        {
            if (!DoesInvocationHaveTheCacheAttribute(invocation))
            {
                invocation.Proceed();
                return;
            }

            var cacheAttribute = (CacheAttribute)invocation.MethodInvocationTarget.GetCustomAttributes(typeof(CacheAttribute), false).FirstOrDefault();

            if (cacheAttribute != null && cacheAttribute.CacheTimeout != null)
            {
                var duration = cacheAttribute.CacheTimeout;

                _cacheService.GetByInvocation(invocation, duration);
            }
            else
                _cacheService.GetByInvocation(invocation);
        }

        private static bool DoesInvocationHaveTheCacheAttribute(IInvocation invocation)
        {
            var methodInvocationTarget = invocation.MethodInvocationTarget;

            return methodInvocationTarget.HasCacheAttribute();
        }
    }
}