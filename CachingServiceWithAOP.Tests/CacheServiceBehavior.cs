using System;
using System.Threading;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CachingServiceWithAOP.Tests
{
    [TestClass]
    public class CacheServiceBehavior
    {
        [TestMethod]
        public void MustBeAbleToPullAMissingValueWithoutError()
        {
            var key = string.Empty;

            var service = new MemoryCacheService();

            var result = service.Get<TestDomainObjA>(key);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void MustBeAbleToPullSomethingPutIntoIt()
        {
            var key = GenerateUniqueKey();

            var service = new MemoryCacheService();

            var testValue = new Guid();

            var testObj = new TestDomainObjA(testValue);

            service.Set(key, testObj);

            var result = service.Get<TestDomainObjA>(key);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.TestValue, testValue);
        }

        [TestMethod]
        public void ShouldBeAbleToHandleATypeMismatch()
        {
            var key = GenerateUniqueKey();

            var service = new MemoryCacheService();

            var testValue = new Guid();

            var testObj = new TestDomainObjA(testValue);

            service.Set(key, testObj);

            var result = service.Get<TestDomainObjB>(key);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void ShouldBeAbleToHandleATypesInAHeirarchy()
        {
            var key = GenerateUniqueKey();

            var service = new MemoryCacheService();

            var testValue = new Guid();

            var testObj = new TestDomainObjectC(testValue);

            service.Set(key, testObj);

            var result = service.Get<TestDomainObjA>(key);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.TestValue, testValue);
            Assert.IsInstanceOfType(result, typeof (TestDomainObjectC));
        }

        [TestMethod]
        public void ShouldBeAbleToHandleAPrimitiveType()
        {
            var key = GenerateUniqueKey();

            var service = new MemoryCacheService();

            const int testValue = int.MaxValue;

            service.Set(key, testValue);

            var result = service.Get<int>(key);

            Assert.IsNotNull(result);
            Assert.AreEqual(result, testValue);
            Assert.IsInstanceOfType(result, typeof (int));
        }

        [TestMethod]
        public void ShouldBeAbleToUseARetreivalFunction()
        {
            var key = GenerateUniqueKey();

            var passedValue = new Guid();

            var service = new MemoryCacheService();

            var testService = new TestExampleServiceImplementation();

            var firstResult = service.Get(key, () => testService.ReturnGuidThatWasPassedIn(passedValue));
            var secondResult = service.Get<Guid>(key);

            Assert.AreEqual(passedValue, firstResult);
            Assert.AreEqual(firstResult, secondResult);
        }


        [TestMethod]
        public void ShouldNotBeAbleToPullTheSameValueAfterTheCacheHasExpired()
        {
           var key = GenerateUniqueKey();

            var service = new MemoryCacheService(1);

            service.Set(key, int.MaxValue);

            Thread.Sleep(1);

            var result = service.Get<int>(key);

            Assert.AreEqual(default(int), result);
        }

        private static string GenerateUniqueKey()
        {
            return new Guid().ToString();
        }
    }
}