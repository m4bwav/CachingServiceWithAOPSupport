using Autofac;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Extensions;

namespace CachingServiceWithAOP.Tests.Infrastructure
{
    public class AutofacTestModule : Module
    {
        public static IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule<AutofacTestModule>();

            return builder.Build();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MemoryCacheService>()
                .As<ICacheService>();

            builder.RegisterType<AOPCachingInterceptor>()
                .As<AOPCachingInterceptor>();

            builder.RegisterType<TestExampleServiceImplementation>()
                .As<IExampleService>()
                .EnableCacheInterception();
        }
    }
}