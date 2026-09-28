using System;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Extensions;

namespace CachingServiceWithAOP.Tests
{
    // The 2015 test fixtures (CachingServiceWithAOP.Tests/Infrastructure in 1.x), with a counter added so tests can
    // count calls instead of comparing clock readings.
    public interface IExampleService
    {
        int ReturnIntThatWasPassedIn(int input);

        DateTime GetCurrentTime();

        DateTime GetCurrentTimeWithSingleTickCachingViaAttribute();

        DateTime GetCurrentTimeWithOneDayCacheAttribute();
    }

    public class TestExampleServiceImplementation : IExampleService
    {
        private static long _clock;

        [Cache]
        public int ReturnIntThatWasPassedIn(int input)
        {
            return input;
        }

        [Cache]
        public DateTime GetCurrentTime()
        {
            return Tick();
        }

        [Cache(1)]
        public DateTime GetCurrentTimeWithSingleTickCachingViaAttribute()
        {
            return Tick();
        }

        [Cache(1, 0, 0, 0)]
        public DateTime GetCurrentTimeWithOneDayCacheAttribute()
        {
            return Tick();
        }

        public Guid ReturnGuidThatWasPassedIn(Guid input)
        {
            return input;
        }

        // A clock that moves on every call, so "same value" means "answered from the cache".
        private static DateTime Tick()
        {
            return new DateTime(2015, 4, 18, 0, 0, 0, DateTimeKind.Utc).AddTicks(Interlocked.Increment(ref _clock));
        }
    }

    public class TestDomainObjA
    {
        public TestDomainObjA(Guid testValue)
        {
            TestValue = testValue;
        }

        public Guid TestValue { get; }
    }

    public class TestDomainObjB
    {
    }

    public class TestDomainObjectC : TestDomainObjA
    {
        public TestDomainObjectC(Guid testValue)
            : base(testValue)
        {
        }
    }

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

    public static class SimpleTestingHelpers
    {
        public static void ClearKeysFromMemcache()
        {
            foreach (var cacheItem in MemoryCache.Default.ToList())
            {
                MemoryCache.Default.Remove(cacheItem.Key);
            }
        }

        // 1.x's tests used new Guid(), the all-zero Guid, so every "unique" key was the same string.
        public static string GenerateUniqueKey()
        {
            return Guid.NewGuid().ToString();
        }
    }

    // Methods for the 2.x fixes: counts calls, returns null, void, tasks, ValueTasks, uncacheable arguments.
    public interface IFixService
    {
        string? Null();

        void Void();

        Task<int> Succeeds();

        Task<int> Faults();

        Task<int> FaultsLater();

        ValueTask<int> Value();

        int Slow(int x);

        int WithCycle(Node node);

        int WithIntKeys(System.Collections.Generic.Dictionary<int, int> values);

        int Forever();

        int WithTimeSpan(TimeSpan value);
    }

    public class Node
    {
        public Node? Next { get; set; }
    }

    public class FixService : IFixService
    {
        public int Calls;

        [Cache]
        public string? Null()
        {
            Interlocked.Increment(ref Calls);
            return null;
        }

        [Cache]
        public void Void()
        {
            Interlocked.Increment(ref Calls);
        }

        [Cache]
        public Task<int> Succeeds()
        {
            return Task.FromResult(Interlocked.Increment(ref Calls));
        }

        [Cache]
        public Task<int> Faults()
        {
            Interlocked.Increment(ref Calls);
            return Task.FromException<int>(new InvalidOperationException("fault"));
        }

        // Not an argument: building a key reads an argument's public getters, and Task.Result blocks while it runs.
        public TaskCompletionSource<int> Pending = new();

        [Cache]
        public Task<int> FaultsLater()
        {
            Interlocked.Increment(ref Calls);
            return Pending.Task;
        }

        [Cache]
        public ValueTask<int> Value()
        {
            return new ValueTask<int>(Interlocked.Increment(ref Calls));
        }

        [Cache]
        public int Slow(int x)
        {
            Interlocked.Increment(ref Calls);
            Thread.Sleep(100);
            return x;
        }

        [Cache]
        public int WithCycle(Node node)
        {
            return Interlocked.Increment(ref Calls);
        }

        [Cache]
        public int WithIntKeys(System.Collections.Generic.Dictionary<int, int> values)
        {
            return Interlocked.Increment(ref Calls);
        }

        [Cache(long.MaxValue)]
        public int Forever()
        {
            return Interlocked.Increment(ref Calls);
        }

        [Cache]
        public int WithTimeSpan(TimeSpan value)
        {
            return Interlocked.Increment(ref Calls);
        }
    }
}
