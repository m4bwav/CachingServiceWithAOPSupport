using System.Linq;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extras.DynamicProxy;

namespace CachingServiceWithAOP.Extensions
{
    /// <summary>Autofac registration helpers for caching.</summary>
    public static class AutofacExtensions
    {
        /// <summary>Registers <see cref="AutofacCachingModule"/>.</summary>
        /// <param name="builder">The container builder.</param>
        public static void RegisterCachingModule(this ContainerBuilder builder)
        {
            builder.RegisterModule<AutofacCachingModule>();
        }

        /// <summary>Whether any public method of the registration's implementation type carries <see cref="CacheAttribute"/>.</summary>
        /// <param name="registration">A component registration.</param>
        /// <returns>True when a method is marked.</returns>
        public static bool HasCacheAttribute(this IComponentRegistration registration)
        {
            return registration.Activator.LimitType.GetMethods().Any(method => method.HasCacheAttribute());
        }

        /// <summary>
        /// Intercepts the registration's interfaces with <see cref="AOPCachingInterceptor"/>, so calls to methods marked
        /// <see cref="CacheAttribute"/> are cached. Resolve the component through an interface; the container must also
        /// have <see cref="RegisterCachingModule"/> (or other registrations of the interceptor and an <c>ICacheService</c>).
        /// </summary>
        /// <typeparam name="TLimit">The registration's limit type.</typeparam>
        /// <typeparam name="TActivatorData">The activator data type.</typeparam>
        /// <typeparam name="TRegistrionStyle">The registration style (the name is 1.x's, spelling included).</typeparam>
        /// <param name="builder">The registration builder.</param>
        /// <returns>The same builder.</returns>
        public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrionStyle> EnableCacheInterception
            <TLimit, TActivatorData, TRegistrionStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TRegistrionStyle> builder)
        {
            return builder.EnableInterfaceInterceptors().InterceptedBy(typeof(AOPCachingInterceptor));
        }
    }
}
