using Autofac;
using CachingServiceWithAOP.CachingServices;

namespace CachingServiceWithAOP
{
    public class AutofacCachingModule : Module
    {
        public static IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule<AutofacCachingModule>();

            return builder.Build();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MemoryCacheService>()
                .As<ICacheService>();

            builder.RegisterType<AOPCachingInterceptor>()
                .As<AOPCachingInterceptor>();
        }
    }
}