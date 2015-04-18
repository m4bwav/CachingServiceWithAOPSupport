using System;
using System.Linq;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extras.DynamicProxy2;

namespace CachingServiceWithAOP.Extensions
{
    public static class AutofacExtensions
    {
        public static void RegisterCachingModule(this ContainerBuilder builder)
        {
            builder.RegisterModule<AutofacCachingModule>();
        }

        public static bool HasCacheAttribute(this IComponentRegistration registration)
        {
            return registration.Activator.LimitType.GetMethods().Any(method => method.HasCacheAttribute());
        }

        public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrionStyle> EnableCacheInterception
            <TLimit, TActivatorData, TRegistrionStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TRegistrionStyle> builder)
        {
            return builder.EnableInterfaceInterceptors().InterceptedBy(typeof (AOPCachingInterceptor));
        }
    }
}