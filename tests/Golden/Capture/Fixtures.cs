// Fixture types for the golden capture. Their full names are part of the recorded cache keys
// (the old key starts with the intercepted target's ToString(), which is the type's full name by default),
// so the namespace and type names here must never change.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CachingServiceWithAOP;
using CachingServiceWithAOP.CachingServices;
using Castle.DynamicProxy;

namespace GoldenCapture.Fixtures
{
    public static class Calls
    {
        private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();

        public static int Hit(string name)
        {
            int n;
            Counts.TryGetValue(name, out n);
            n++;
            Counts[name] = n;
            return n;
        }

        public static int Count(string name)
        {
            int n;
            Counts.TryGetValue(name, out n);
            return n;
        }

        public static void Reset()
        {
            Counts.Clear();
        }
    }

    public interface ISvc
    {
        int Cached();

        int NotCached();

        int CachedOneTick();

        int CachedOneDay();

        int CachedOneSecond();

        int CachedZeroTicks();

        int CachedNegativeTicks();

        int WithArg(int x);

        int WithTwo(string s, int x);

        string ReturnsNull();

        void VoidCached();

        int Throws();

        T Echo<T>(T value);

        Task<int> CachedTask();

        [Cache]
        int AttrOnInterfaceOnly();
    }

    public class Svc : ISvc
    {
        [Cache]
        public int Cached()
        {
            return Calls.Hit("Cached");
        }

        public int NotCached()
        {
            return Calls.Hit("NotCached");
        }

        [Cache(1)]
        public int CachedOneTick()
        {
            return Calls.Hit("CachedOneTick");
        }

        [Cache(1, 0, 0, 0)]
        public int CachedOneDay()
        {
            return Calls.Hit("CachedOneDay");
        }

        [Cache(0, 0, 1)]
        public int CachedOneSecond()
        {
            return Calls.Hit("CachedOneSecond");
        }

        [Cache(0)]
        public int CachedZeroTicks()
        {
            return Calls.Hit("CachedZeroTicks");
        }

        [Cache(-1)]
        public int CachedNegativeTicks()
        {
            return Calls.Hit("CachedNegativeTicks");
        }

        [Cache]
        public int WithArg(int x)
        {
            return Calls.Hit("WithArg") * 1000 + x;
        }

        [Cache]
        public int WithTwo(string s, int x)
        {
            return Calls.Hit("WithTwo") * 1000 + x;
        }

        [Cache]
        public string ReturnsNull()
        {
            Calls.Hit("ReturnsNull");
            return null;
        }

        [Cache]
        public void VoidCached()
        {
            Calls.Hit("VoidCached");
        }

        [Cache]
        public int Throws()
        {
            Calls.Hit("Throws");
            throw new InvalidOperationException("fixture failure");
        }

        [Cache]
        public T Echo<T>(T value)
        {
            Calls.Hit("Echo");
            return value;
        }

        [Cache]
        public Task<int> CachedTask()
        {
            return Task.FromResult(Calls.Hit("CachedTask"));
        }

        public int AttrOnInterfaceOnly()
        {
            return Calls.Hit("AttrOnInterfaceOnly");
        }
    }

    public interface INamed
    {
        int Cached();
    }

    public class NamedSvc : INamed
    {
        private static int _instances;
        private readonly int _id;

        public NamedSvc()
        {
            _id = ++_instances;
        }

        public static void ResetInstances()
        {
            _instances = 0;
        }

        [Cache]
        public int Cached()
        {
            return Calls.Hit("NamedSvc.Cached");
        }

        public override string ToString()
        {
            return "named-" + _id;
        }
    }

    public interface INoCache
    {
        int Plain();
    }

    public class NoCacheSvc : INoCache
    {
        public int Plain()
        {
            return Calls.Hit("Plain");
        }
    }

    public class BaseSvc
    {
        [Cache]
        public virtual int V()
        {
            return Calls.Hit("BaseSvc.V");
        }

        public virtual int Plain()
        {
            return Calls.Hit("BaseSvc.Plain");
        }
    }

    public class DerivedSvc : BaseSvc
    {
        public override int V()
        {
            return Calls.Hit("DerivedSvc.V");
        }
    }

    public enum Colour
    {
        Red = 1,
        Blue = 2,
    }

    public class Dto
    {
        public int Field = 5;
        private int _hidden = 7;

        public int A { get; set; }

        public string B { get; set; }

        public int ReadOnly
        {
            get { return 3 + _hidden - 7; }
        }
    }

    public class Node
    {
        public int V { get; set; }

        public Node Next { get; set; }
    }

    public class Animal
    {
        public string Name { get; set; }
    }

    public class Dog : Animal
    {
        public int Legs { get; set; }
    }

    public interface IKeyed
    {
        int None();

        int OneInt(int x);

        int Str(string s);

        int Two(int x, string s);

        int Obj(object o);

        int Generic<T>(T value);

        int Generic2<T1, T2>(T1 a, T2 b);

        int Arr(int[] values);

        int ListOf(List<string> values);

        int Dict(Dictionary<string, int> values);

        int WithDto(Dto dto);

        int WithNode(Node node);

        int WithAnimal(Animal animal);

        int Date(DateTime value);

        int WithGuid(Guid value);

        int WithEnum(Colour value);

        int WithDayOfWeek(DayOfWeek value);

        int Dbl(double value);

        int Dec(decimal value);

        int Chr(char value);

        int NullableInt(int? value);

        int Overload(int value);

        int Overload(long value);

        int WithTimeSpan(TimeSpan value);

        int WithBool(bool value);

        int WithParams(params int[] values);

        int WithOut(out int value);

        int WithRef(ref int value);
    }

    public class Keyed : IKeyed
    {
        public int None() { return 0; }

        public int OneInt(int x) { return 0; }

        public int Str(string s) { return 0; }

        public int Two(int x, string s) { return 0; }

        public int Obj(object o) { return 0; }

        public int Generic<T>(T value) { return 0; }

        public int Generic2<T1, T2>(T1 a, T2 b) { return 0; }

        public int Arr(int[] values) { return 0; }

        public int ListOf(List<string> values) { return 0; }

        public int Dict(Dictionary<string, int> values) { return 0; }

        public int WithDto(Dto dto) { return 0; }

        public int WithNode(Node node) { return 0; }

        public int WithAnimal(Animal animal) { return 0; }

        public int Date(DateTime value) { return 0; }

        public int WithGuid(Guid value) { return 0; }

        public int WithEnum(Colour value) { return 0; }

        public int WithDayOfWeek(DayOfWeek value) { return 0; }

        public int Dbl(double value) { return 0; }

        public int Dec(decimal value) { return 0; }

        public int Chr(char value) { return 0; }

        public int NullableInt(int? value) { return 0; }

        public int Overload(int value) { return 0; }

        public int Overload(long value) { return 0; }

        public int WithTimeSpan(TimeSpan value) { return 0; }

        public int WithBool(bool value) { return 0; }

        public int WithParams(params int[] values) { return 0; }

        public int WithOut(out int value)
        {
            value = 9;
            return 0;
        }

        public int WithRef(ref int value)
        {
            value++;
            return 0;
        }
    }

    public class NamedKeyed : Keyed
    {
        public override string ToString()
        {
            return "custom-target";
        }
    }

    // Records the key DefaultCacheKeyService makes for each intercepted call.
    public class KeyCapture : IInterceptor
    {
        public string LastKey;

        public void Intercept(IInvocation invocation)
        {
            LastKey = new DefaultCacheKeyService().GenerateUniqueKeyForCall(invocation);
            invocation.Proceed();
        }
    }

    // Records the duration AOPCachingInterceptor hands to the cache service.
    public class RecordingCacheService : ICacheService
    {
        public readonly List<object> Durations = new List<object>();

        public T Get<T>(string key)
        {
            return default(T);
        }

        public T Get<T>(string key, Func<T> retrievalFunc)
        {
            return retrievalFunc();
        }

        public void Set<T>(string key, T value)
        {
        }

        public void GetByInvocation(IInvocation invocation, TimeSpan? duration = null)
        {
            Durations.Add(duration.HasValue ? (object)duration.Value.Ticks : null);
            invocation.Proceed();
        }
    }
}
