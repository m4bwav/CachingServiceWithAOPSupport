using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Extensions;
using NUnit.Framework;

namespace CachingServiceWithAOP.Tests
{
    // The 2.x fixes (plan items 2 to 10), beyond what the golden replay records.
    [TestFixture]
    public class FixTests
    {
        private IContainer _container = null!;
        private FixService _target = null!;
        private IFixService _service = null!;

        [SetUp]
        public void SetUp()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();
            _target = new FixService();
            var builder = new ContainerBuilder();
            builder.RegisterCachingModule();
            builder.RegisterInstance(_target).As<IFixService>().EnableCacheInterception();
            _container = builder.Build();
            _service = _container.Resolve<IFixService>();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }

        [Test]
        public void A_null_result_is_cached()
        {
            Assert.That(_service.Null(), Is.Null);
            Assert.That(_service.Null(), Is.Null);
            Assert.That(_target.Calls, Is.EqualTo(1));
        }

        [Test]
        public void A_void_method_runs_every_time()
        {
            _service.Void();
            _service.Void();
            Assert.That(_target.Calls, Is.EqualTo(2));
        }

        [Test]
        public async Task A_successful_task_is_cached()
        {
            Assert.That(await _service.Succeeds(), Is.EqualTo(1));
            Assert.That(await _service.Succeeds(), Is.EqualTo(1));
            Assert.That(_target.Calls, Is.EqualTo(1));
        }

        [Test]
        public void A_faulted_task_is_not_cached()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => _service.Faults());
            Assert.ThrowsAsync<InvalidOperationException>(() => _service.Faults());
            Assert.That(_target.Calls, Is.EqualTo(2));
        }

        [Test]
        public void A_task_that_faults_later_is_evicted()
        {
            var pending = _service.FaultsLater();
            Assert.That(_service.FaultsLater(), Is.SameAs(pending), "cached while running");

            _target.Pending.SetException(new InvalidOperationException("later"));
            Assert.ThrowsAsync<InvalidOperationException>(() => pending);

            _target.Pending = new TaskCompletionSource<int>();
            Assert.That(_service.FaultsLater(), Is.Not.SameAs(pending), "evicted after the fault");
            Assert.That(_target.Calls, Is.EqualTo(2));
        }

        [Test]
        public async Task A_ValueTask_method_is_never_cached()
        {
            Assert.That(await _service.Value(), Is.EqualTo(1));
            Assert.That(await _service.Value(), Is.EqualTo(2));
        }

        [Test]
        public void A_cyclic_argument_runs_the_call_uncached()
        {
            var node = new Node();
            node.Next = node;
            Assert.That(_service.WithCycle(node), Is.EqualTo(1));
            Assert.That(_service.WithCycle(node), Is.EqualTo(2));
        }

        [Test]
        public void A_dictionary_with_non_string_keys_runs_the_call_uncached()
        {
            var values = new Dictionary<int, int> { [1] = 2 };
            Assert.That(_service.WithIntKeys(values), Is.EqualTo(1));
            Assert.That(_service.WithIntKeys(values), Is.EqualTo(2));
        }

        [Test]
        public void An_overflowing_lifetime_never_expires()
        {
            Assert.That(_service.Forever(), Is.EqualTo(1));
            Assert.That(_service.Forever(), Is.EqualTo(1));
        }

        [Test]
        public void TimeSpan_arguments_make_distinct_keys()
        {
            Assert.That(_service.WithTimeSpan(TimeSpan.FromSeconds(1)), Is.EqualTo(1));
            Assert.That(_service.WithTimeSpan(TimeSpan.FromSeconds(1)), Is.EqualTo(1));
            Assert.That(_service.WithTimeSpan(TimeSpan.FromSeconds(2)), Is.EqualTo(2));
        }

        [Test]
        public void Concurrent_intercepted_calls_with_one_key_run_the_method_once()
        {
            var start = new ManualResetEventSlim();
            var tasks = Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
            {
                start.Wait();
                return _service.Slow(7);
            })).ToArray();
            start.Set();
            Task.WaitAll(tasks);

            Assert.That(tasks.Select(t => t.Result), Is.All.EqualTo(7));
            Assert.That(_target.Calls, Is.EqualTo(1));
            Assert.That(MemoryCacheService.PendingLockCount, Is.EqualTo(0), "the lock table empties");
        }

        [Test]
        public void Concurrent_retrievals_with_one_key_run_the_function_once()
        {
            var service = new MemoryCacheService();
            var key = SimpleTestingHelpers.GenerateUniqueKey();
            var calls = 0;
            var start = new ManualResetEventSlim();
            var tasks = Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
            {
                start.Wait();
                return service.Get(key, () =>
                {
                    Interlocked.Increment(ref calls);
                    Thread.Sleep(100);
                    return "value";
                });
            })).ToArray();
            start.Set();
            Task.WaitAll(tasks);

            Assert.That(tasks.Select(t => t.Result), Is.All.EqualTo("value"));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(MemoryCacheService.PendingLockCount, Is.EqualTo(0));
        }

        [Test]
        public void Get_with_a_function_replaces_a_value_of_another_type()
        {
            var service = new MemoryCacheService();
            var key = SimpleTestingHelpers.GenerateUniqueKey();
            service.Set(key, "text");
            Assert.That(service.Get(key, () => 5), Is.EqualTo(5));
            Assert.That(service.Get<int>(key), Is.EqualTo(5));
        }

        [Test]
        public void Get_with_a_function_caches_null()
        {
            var service = new MemoryCacheService();
            var key = SimpleTestingHelpers.GenerateUniqueKey();
            var calls = 0;
            string? Make()
            {
                calls++;
                return null;
            }

            Assert.That(service.Get(key, Make), Is.Null);
            Assert.That(service.Get(key, Make), Is.Null);
            Assert.That(service.Get<string>(key), Is.Null);
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Set_with_a_null_value_still_throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MemoryCacheService().Set<string?>("k", null));
        }

        [Test]
        public void The_ObjectCache_constructor_keeps_values()
        {
            using var cache = new MemoryCache("fix-tests");
            var service = new MemoryCacheService(cache);
            service.Set("k", "v");
            Assert.That(service.Get<string>("k"), Is.EqualTo("v"));
            Assert.That(MemoryCache.Default.Get("k"), Is.Null, "stored in the given cache, not the default one");
        }
    }
}
