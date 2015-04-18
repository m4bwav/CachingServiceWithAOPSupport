using System.Threading;
using Autofac;
using CachingServiceWithAOP.Extensions;
using CachingServiceWithAOP.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CachingServiceWithAOP.Tests
{
    [TestClass]
    public class CacheAttributeBehavior
    {
        [TestMethod]
        public void CanSetLowerCacheDurationViaAttributeParameter()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            var builder = new ContainerBuilder();

            builder.RegisterCachingModule();

            builder.RegisterType<TestExampleServiceImplementation>()
                .As<IExampleService>()
                .EnableCacheInterception();

            var container = builder.Build();

            var service = container.Resolve<IExampleService>();

            var firstResult = service.GetCurrentTimeWithSingleTickCachingViaAttribute().Ticks;

            Thread.Sleep(1000);

            var secondResult = service.GetCurrentTimeWithSingleTickCachingViaAttribute().Ticks;

            Assert.AreNotEqual(firstResult, secondResult);

        }

        [TestMethod]
        public void CanSetHigherCacheDurationViaAttributeParameter()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            var builder = new ContainerBuilder();

            builder.RegisterCachingModule();

            builder.RegisterType<TestExampleServiceImplementation>()
                .As<IExampleService>()
                .EnableCacheInterception();

            var container = builder.Build();

            var service = container.Resolve<IExampleService>();

            var firstResult = service.GetCurrentTimeWithOneDayCacheAttribute().Ticks;

            Thread.Sleep(1000);

            var secondResult = service.GetCurrentTimeWithOneDayCacheAttribute().Ticks;

            Assert.AreEqual(firstResult, secondResult);

        }
    }
}
