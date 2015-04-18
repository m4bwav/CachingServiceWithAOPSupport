using System;
using System.Runtime.Caching;
using System.Threading;
using Autofac;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CachingServiceWithAOP.Tests
{
    [TestClass]
    public class DynamicProxyBehavior
    {
        [TestMethod]
        public void CanExecuteBaseInputWithoutErroring()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            const int input = 5;

            var container = AutofacTestModule.BuildContainer();

            var service = container.Resolve<IExampleService>();

            var result = service.ReturnIntThatWasPassedIn(input);

            Assert.AreEqual(input, result);
        }

        [TestMethod]
        public void CanRetrieveAnOldValueFromTheCache()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            var builder = new ContainerBuilder();

            builder.RegisterModule<AutofacTestModule>();
            builder.RegisterType<MemoryCacheService>()
                .WithParameter("expiration", new TimeSpan(0, 0, 1))
                .As<ICacheService>();

            var container = builder.Build();

            var service = container.Resolve<IExampleService>();

            var firstResult = service.GetCurrentTime();

            Thread.Sleep(1);

            var secondResult = service.GetCurrentTime();

            Assert.AreEqual(firstResult.Ticks, secondResult.Ticks);
        }

        [TestMethod]
        public void CannotRetrieveAnOldValueFromTheCacheAfterTime()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            var builder = new ContainerBuilder();

            builder.RegisterModule<AutofacTestModule>();

            var cachePeriod = new TimeSpan(1);

            builder.RegisterType<MemoryCacheService>()
                .WithParameter("expiration", cachePeriod)
                .As<ICacheService>();

            var container = builder.Build();


            var service = container.Resolve<IExampleService>();

            var firstResult = service.GetCurrentTime().Ticks;

            Thread.Sleep(1);

            var secondResult = service.GetCurrentTime().Ticks;

            Assert.AreNotEqual(firstResult, secondResult);
        }
    }
}