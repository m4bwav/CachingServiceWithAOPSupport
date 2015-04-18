using System.Threading;
using Autofac;
using CachingServiceWithAOP.Extensions;
using CachingServiceWithAOP.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CachingServiceWithAOP.Tests
{
    [TestClass]
    public class AutofacAOPBehavior
    {
        [TestMethod]
        public void CanExecuteAfterAutofacExtensionsAreUsed()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();

            var builder = new ContainerBuilder();

            builder.RegisterCachingModule();

            builder.RegisterType<TestExampleServiceImplementation>()
                .As<IExampleService>()
                .EnableCacheInterception();

            var container = builder.Build();

            var service = container.Resolve<IExampleService>();

            var firstResult = service.GetCurrentTime().Ticks;

            Thread.Sleep(1000);

            var secondResult = service.GetCurrentTime().Ticks;

            Assert.AreEqual(firstResult, secondResult);
        }
    }
}