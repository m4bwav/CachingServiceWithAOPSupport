using System;
using System.Threading;
using Autofac;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Extensions;
using NUnit.Framework;

namespace CachingServiceWithAOP.Tests
{
    // The 13 MSTest tests of 2015 (AutofacAOPBehavior, CacheAttributeBehavior, CacheServiceBehavior, DynamicProxyBehavior),
    // ported to NUnit with the same arrangement and assertions. Changes: unique keys are unique; the one-second sleeps
    // are 50 ms (a one-day or five-minute lifetime is not near either); results are compared through a moving clock.
    [TestFixture]
    public class AutofacAOPBehavior
    {
        [Test]
        public void CanExecuteAfterAutofacExtensionsAreUsed()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            var builder = new ContainerBuilder();

            builder.RegisterCachingModule();

            builder.RegisterType<TestExampleServiceImplementation>()
                .As<IExampleService>()
                .EnableCacheInterception();

            using var container = builder.Build();

            var service = container.Resolve<IExampleService>();

            var firstResult = service.GetCurrentTime().Ticks;

            Thread.Sleep(50);

            var secondResult = service.GetCurrentTime().Ticks;

            Assert.That(secondResult, Is.EqualTo(firstResult));
        }
    }

    [TestFixture]
    public class CacheAttributeBehavior
    {
        [Test]
        public void CanSetLowerCacheDurationViaAttributeParameter()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            var builder = new ContainerBuilder();

            builder.RegisterCachingModule();

            builder.RegisterType<TestExampleServiceImplementation>()
                .As<IExampleService>()
                .EnableCacheInterception();

            using var container = builder.Build();

            var service = container.Resolve<IExampleService>();

            var firstResult = service.GetCurrentTimeWithSingleTickCachingViaAttribute().Ticks;

            Thread.Sleep(50);

            var secondResult = service.GetCurrentTimeWithSingleTickCachingViaAttribute().Ticks;

            Assert.That(secondResult, Is.Not.EqualTo(firstResult));
        }

        [Test]
        public void CanSetHigherCacheDurationViaAttributeParameter()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            var builder = new ContainerBuilder();

            builder.RegisterCachingModule();

            builder.RegisterType<TestExampleServiceImplementation>()
                .As<IExampleService>()
                .EnableCacheInterception();

            using var container = builder.Build();

            var service = container.Resolve<IExampleService>();

            var firstResult = service.GetCurrentTimeWithOneDayCacheAttribute().Ticks;

            Thread.Sleep(50);

            var secondResult = service.GetCurrentTimeWithOneDayCacheAttribute().Ticks;

            Assert.That(secondResult, Is.EqualTo(firstResult));
        }
    }

    [TestFixture]
    public class CacheServiceBehavior
    {
        [Test]
        public void MustBeAbleToPullAMissingValueWithoutError()
        {
            var key = string.Empty;

            var service = new MemoryCacheService();

            var result = service.Get<TestDomainObjA>(key);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void MustBeAbleToPullSomethingPutIntoIt()
        {
            var key = SimpleTestingHelpers.GenerateUniqueKey();

            var service = new MemoryCacheService();

            var testValue = Guid.NewGuid();

            var testObj = new TestDomainObjA(testValue);

            service.Set(key, testObj);

            var result = service.Get<TestDomainObjA>(key);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.TestValue, Is.EqualTo(testValue));
        }

        [Test]
        public void ShouldBeAbleToHandleATypeMismatch()
        {
            var key = SimpleTestingHelpers.GenerateUniqueKey();

            var service = new MemoryCacheService();

            var testObj = new TestDomainObjA(Guid.NewGuid());

            service.Set(key, testObj);

            var result = service.Get<TestDomainObjB>(key);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ShouldBeAbleToHandleATypesInAHeirarchy()
        {
            var key = SimpleTestingHelpers.GenerateUniqueKey();

            var service = new MemoryCacheService();

            var testValue = Guid.NewGuid();

            var testObj = new TestDomainObjectC(testValue);

            service.Set(key, testObj);

            var result = service.Get<TestDomainObjA>(key);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.TestValue, Is.EqualTo(testValue));
            Assert.That(result, Is.InstanceOf<TestDomainObjectC>());
        }

        [Test]
        public void ShouldBeAbleToHandleAPrimitiveType()
        {
            var key = SimpleTestingHelpers.GenerateUniqueKey();

            var service = new MemoryCacheService();

            const int testValue = int.MaxValue;

            service.Set(key, testValue);

            var result = service.Get<int>(key);

            Assert.That(result, Is.EqualTo(testValue));
        }

        [Test]
        public void ShouldBeAbleToUseARetreivalFunction()
        {
            var key = SimpleTestingHelpers.GenerateUniqueKey();

            var passedValue = Guid.NewGuid();

            var service = new MemoryCacheService();

            var testService = new TestExampleServiceImplementation();

            var firstResult = service.Get(key, () => testService.ReturnGuidThatWasPassedIn(passedValue));
            var secondResult = service.Get<Guid>(key);

            Assert.That(firstResult, Is.EqualTo(passedValue));
            Assert.That(secondResult, Is.EqualTo(firstResult));
        }

        [Test]
        public void ShouldNotBeAbleToPullTheSameValueAfterTheCacheHasExpired()
        {
            var key = SimpleTestingHelpers.GenerateUniqueKey();

            var service = new MemoryCacheService(1);

            service.Set(key, int.MaxValue);

            Thread.Sleep(20);

            var result = service.Get<int>(key);

            Assert.That(result, Is.EqualTo(default(int)));
        }
    }

    [TestFixture]
    public class DynamicProxyBehavior
    {
        [Test]
        public void CanExecuteBaseInputWithoutErroring()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            const int input = 5;

            using var container = AutofacTestModule.BuildContainer();

            var service = container.Resolve<IExampleService>();

            var result = service.ReturnIntThatWasPassedIn(input);

            Assert.That(result, Is.EqualTo(input));
        }

        [Test]
        public void CanRetrieveAnOldValueFromTheCache()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            var builder = new ContainerBuilder();

            builder.RegisterModule<AutofacTestModule>();
            builder.RegisterType<MemoryCacheService>()
                .WithParameter("expiration", new TimeSpan(0, 0, 1))
                .As<ICacheService>();

            using var container = builder.Build();

            var service = container.Resolve<IExampleService>();

            var firstResult = service.GetCurrentTime();

            Thread.Sleep(1);

            var secondResult = service.GetCurrentTime();

            Assert.That(secondResult.Ticks, Is.EqualTo(firstResult.Ticks));
        }

        [Test]
        public void CannotRetrieveAnOldValueFromTheCacheAfterTime()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            var builder = new ContainerBuilder();

            builder.RegisterModule<AutofacTestModule>();

            var cachePeriod = new TimeSpan(1);

            builder.RegisterType<MemoryCacheService>()
                .WithParameter("expiration", cachePeriod)
                .As<ICacheService>();

            using var container = builder.Build();

            var service = container.Resolve<IExampleService>();

            var firstResult = service.GetCurrentTime().Ticks;

            Thread.Sleep(20);

            var secondResult = service.GetCurrentTime().Ticks;

            Assert.That(secondResult, Is.Not.EqualTo(firstResult));
        }
    }
}
