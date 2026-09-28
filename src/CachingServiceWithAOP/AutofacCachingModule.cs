using Autofac;
using CachingServiceWithAOP.CachingServices;

namespace CachingServiceWithAOP
{
    /// <summary>
    /// Registers <see cref="MemoryCacheService"/> as <see cref="ICacheService"/> and <see cref="AOPCachingInterceptor"/>,
    /// which <c>EnableCacheInterception()</c> needs.
    /// </summary>
    public class AutofacCachingModule : Module
    {
        /// <summary>Builds a container holding only this module's registrations.</summary>
        /// <returns>The container.</returns>
        public static IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule<AutofacCachingModule>();

            return builder.Build();
        }

        /// <summary>Adds the registrations.</summary>
        /// <param name="builder">The container builder.</param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MemoryCacheService>()
                .As<ICacheService>();

            builder.RegisterType<AOPCachingInterceptor>()
                .As<AOPCachingInterceptor>();
        }
    }
}
