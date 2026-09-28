using System.Linq;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Extensions;
using Castle.DynamicProxy;

namespace CachingServiceWithAOP
{
    /// <summary>
    /// A Castle DynamicProxy interceptor that sends calls to methods marked <see cref="CacheAttribute"/> through an
    /// <see cref="ICacheService"/>; every other call proceeds unchanged.
    /// </summary>
    public class AOPCachingInterceptor : IInterceptor
    {
        private readonly ICacheService _cacheService;

        /// <summary>Creates the interceptor over a cache service.</summary>
        /// <param name="cacheService">Where results are kept.</param>
        public AOPCachingInterceptor(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>Proceeds, or answers the call from the cache when its target method carries <see cref="CacheAttribute"/>.</summary>
        /// <param name="invocation">The intercepted call.</param>
        public void Intercept(IInvocation invocation)
        {
            if (!DoesInvocationHaveTheCacheAttribute(invocation))
            {
                invocation.Proceed();
                return;
            }

            var cacheAttribute = (CacheAttribute?)invocation.MethodInvocationTarget.GetCustomAttributes(typeof(CacheAttribute), false).FirstOrDefault();

            if (cacheAttribute != null && cacheAttribute.CacheTimeout != null)
            {
                var duration = cacheAttribute.CacheTimeout;

                _cacheService.GetByInvocation(invocation, duration);
            }
            else
            {
                _cacheService.GetByInvocation(invocation);
            }
        }

        private static bool DoesInvocationHaveTheCacheAttribute(IInvocation invocation)
        {
            var methodInvocationTarget = invocation.MethodInvocationTarget;

            return methodInvocationTarget.HasCacheAttribute();
        }
    }
}
