// Every case of the golden capture. Touches only public names of CachingServiceWithAOPSupport, Autofac and Castle.Core,
// so the golden test can compile this file unchanged against the new library and compare its answers.
// Each case starts from an empty MemoryCache.Default and zeroed call counters. Timing cases use wide margins
// (1-tick and 100 ms lifetimes, 50 to 600 ms sleeps) so they answer the same on every run.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using CachingServiceWithAOP;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Extensions;
using Castle.DynamicProxy;
using GoldenCapture.Fixtures;

namespace GoldenCapture
{
    public sealed class Case
    {
        public string Group;
        public string Name;
        public object Result;
    }

    public static class Cases
    {
        private static readonly List<Case> All = new List<Case>();

        public static List<Case> Run()
        {
            All.Clear();
            AttributeCases();
            ReflectionCases();
            MemoryCacheServiceCases();
            ExpirationCases();
            KeyCases();
            InterceptionCases();
            ModuleCases();
            return All;
        }

        // ---------- helpers ----------

        private static void Add(string group, string name, Func<object> call)
        {
            ClearDefaultCache();
            Calls.Reset();
            NamedSvc.ResetInstances();
            object result;
            try
            {
                result = call();
            }
            catch (Exception e)
            {
                result = Describe(e);
            }

            All.Add(new Case { Group = group, Name = name, Result = result });
        }

        public static JsonObject Describe(Exception e)
        {
            var o = new JsonObject();
            o.Add("$throws", e.GetType().FullName + ": " + FirstLine(e.Message));
            var inner = new List<object>();
            for (var x = e.InnerException; x != null; x = x.InnerException)
            {
                inner.Add(x.GetType().FullName + ": " + FirstLine(x.Message));
            }

            if (inner.Count > 0)
            {
                o.Add("$inner", inner);
            }

            return o;
        }

        private static string FirstLine(string message)
        {
            if (message == null)
            {
                return "";
            }

            var i = message.IndexOfAny(new[] { (char)13, (char)10 });
            return i < 0 ? message : message.Substring(0, i);
        }

        // Runs one step inside a case and records its value or its exception, so later steps still run.
        private static object Try(Func<object> step)
        {
            try
            {
                return step();
            }
            catch (Exception e)
            {
                return Describe(e);
            }
        }

        private static object Ticks(TimeSpan? t)
        {
            return t.HasValue ? (object)t.Value.Ticks : null;
        }

        private static void ClearDefaultCache()
        {
            foreach (var kv in MemoryCache.Default.ToList())
            {
                MemoryCache.Default.Remove(kv.Key);
            }
        }

        private static object Show(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is int || value is long || value is bool || value is string)
            {
                return value;
            }

            if (value is Animal a)
            {
                return value.GetType().Name + ":" + a.Name;
            }

            return value.GetType().FullName + ":" + Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static JsonObject Obj(params object[] pairs)
        {
            var o = new JsonObject();
            for (var i = 0; i < pairs.Length; i += 2)
            {
                o.Add((string)pairs[i], pairs[i + 1]);
            }

            return o;
        }

        // ---------- CacheAttribute ----------

        private static void AttributeCases()
        {
            const string g = "CacheAttribute";
            Add(g, "ctor()", () => Ticks(new CacheAttribute().CacheTimeout));
            foreach (var t in new[] { 0L, 1L, -1L, 10000000L, long.MaxValue, long.MinValue })
            {
                var ticks = t;
                Add(g, "ctor(long " + ticks.ToString(CultureInfo.InvariantCulture) + ")", () => Ticks(new CacheAttribute(ticks).CacheTimeout));
            }

            foreach (var hms in new[] { new[] { 0, 0, 0 }, new[] { 1, 2, 3 }, new[] { -1, 0, 0 }, new[] { 0, 0, -5 }, new[] { 24, 0, 0 }, new[] { 0, 90, 90 }, new[] { int.MaxValue, 0, 0 } })
            {
                var v = hms;
                Add(g, "ctor(" + string.Join(",", v) + ")", () => Ticks(new CacheAttribute(v[0], v[1], v[2]).CacheTimeout));
            }

            foreach (var dhms in new[] { new[] { 1, 0, 0, 0 }, new[] { 0, 25, 61, 61 }, new[] { -1, 0, 0, 0 }, new[] { int.MaxValue, 0, 0, 0 } })
            {
                var v = dhms;
                Add(g, "ctor(" + string.Join(",", v) + ")", () => Ticks(new CacheAttribute(v[0], v[1], v[2], v[3]).CacheTimeout));
            }

            Add(g, "CacheTimeout set then null", () =>
            {
                var a = new CacheAttribute(5);
                var before = Ticks(a.CacheTimeout);
                a.CacheTimeout = TimeSpan.FromSeconds(2);
                var mid = Ticks(a.CacheTimeout);
                a.CacheTimeout = null;
                return new List<object> { before, mid, Ticks(a.CacheTimeout) };
            });

            Add(g, "type shape", () =>
            {
                var t = typeof(CacheAttribute);
                var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(t, typeof(AttributeUsageAttribute), true);
                return Obj(
                    "baseType", t.BaseType.FullName,
                    "sealed", t.IsSealed,
                    "validOn", usage == null ? null : usage.ValidOn.ToString(),
                    "allowMultiple", usage == null ? (object)null : usage.AllowMultiple,
                    "inherited", usage == null ? (object)null : usage.Inherited);
            });
        }

        // ---------- ReflectionExtensions.HasCacheAttribute(MethodInfo) ----------

        private static void ReflectionCases()
        {
            const string g = "HasCacheAttribute(MethodInfo)";
            Add(g, "Svc.Cached", () => typeof(Svc).GetMethod("Cached").HasCacheAttribute());
            Add(g, "Svc.NotCached", () => typeof(Svc).GetMethod("NotCached").HasCacheAttribute());
            Add(g, "ISvc.Cached (attribute on the class only)", () => typeof(ISvc).GetMethod("Cached").HasCacheAttribute());
            Add(g, "ISvc.AttrOnInterfaceOnly", () => typeof(ISvc).GetMethod("AttrOnInterfaceOnly").HasCacheAttribute());
            Add(g, "Svc.AttrOnInterfaceOnly", () => typeof(Svc).GetMethod("AttrOnInterfaceOnly").HasCacheAttribute());
            Add(g, "DerivedSvc.V (override of a cached virtual)", () => typeof(DerivedSvc).GetMethod("V").HasCacheAttribute());
            Add(g, "Svc.Echo generic definition", () => typeof(Svc).GetMethod("Echo").HasCacheAttribute());
            Add(g, "Svc.Echo<int>", () => typeof(Svc).GetMethod("Echo").MakeGenericMethod(typeof(int)).HasCacheAttribute());
            Add(g, "null", () => ((MethodInfo)null).HasCacheAttribute());
        }

        // ---------- MemoryCacheService: Get and Set ----------

        private static void MemoryCacheServiceCases()
        {
            const string g = "MemoryCacheService";
            Add(g, "Get<string> missing", () => new MemoryCacheService().Get<string>("missing"));
            Add(g, "Get<int> missing", () => new MemoryCacheService().Get<int>("missing"));
            Add(g, "Get<int?> missing", () => new MemoryCacheService().Get<int?>("missing"));
            Add(g, "Get<string> null key", () => new MemoryCacheService().Get<string>(null));
            Add(g, "Get<string> empty key", () => new MemoryCacheService().Get<string>(""));
            Add(g, "Set and Get string", () =>
            {
                var s = new MemoryCacheService();
                s.Set("k", "v");
                return s.Get<string>("k");
            });
            Add(g, "Set empty key", () =>
            {
                var s = new MemoryCacheService();
                s.Set("", "v");
                return s.Get<string>("");
            });
            Add(g, "Set int, Get as int, long, int?, object, string", () =>
            {
                var s = new MemoryCacheService();
                s.Set("k", 42);
                return new List<object> { s.Get<int>("k"), s.Get<long>("k"), Show(s.Get<int?>("k")), Show(s.Get<object>("k")), s.Get<string>("k") };
            });
            Add(g, "Set int 0 then Get<int> and Get<int?>", () =>
            {
                var s = new MemoryCacheService();
                s.Set("k", 0);
                return new List<object> { s.Get<int>("k"), Show(s.Get<int?>("k")) };
            });
            Add(g, "Set Dog, Get as Animal, Dog, object, string", () =>
            {
                var s = new MemoryCacheService();
                s.Set<Animal>("k", new Dog { Name = "rex", Legs = 4 });
                return new List<object> { Show(s.Get<Animal>("k")), Show(s.Get<Dog>("k")), Show(s.Get<object>("k")), s.Get<string>("k") };
            });
            Add(g, "Set same instance returns same reference", () =>
            {
                var s = new MemoryCacheService();
                var d = new Dog { Name = "rex" };
                s.Set("k", d);
                return ReferenceEquals(d, s.Get<Dog>("k"));
            });
            Add(g, "Set null value", () =>
            {
                var s = new MemoryCacheService();
                s.Set<string>("k", null);
                return s.Get<string>("k");
            });
            Add(g, "Set null key", () =>
            {
                new MemoryCacheService().Set(null, "v");
                return "no throw";
            });
            Add(g, "Set twice overwrites", () =>
            {
                var s = new MemoryCacheService();
                s.Set("k", "v");
                s.Set("k", "w");
                return s.Get<string>("k");
            });
            Add(g, "two services share MemoryCache.Default", () =>
            {
                new MemoryCacheService().Set("k", "from-a");
                var viaB = new MemoryCacheService(TimeSpan.FromDays(1)).Get<string>("k");
                var viaDefault = MemoryCache.Default.Get("k");
                return new List<object> { viaB, Show(viaDefault) };
            });
            Add(g, "Get(key, func) runs func once", () =>
            {
                var s = new MemoryCacheService();
                var a = s.Get("k", () => Calls.Hit("f"));
                var b = s.Get("k", () => Calls.Hit("f"));
                var c = s.Get<int>("k");
                return new List<object> { a, b, c, Calls.Count("f") };
            });
            Add(g, "Get(key, func) returning null", () =>
            {
                var s = new MemoryCacheService();
                var first = Try(() => s.Get<string>("k", () =>
                {
                    Calls.Hit("f");
                    return null;
                }));
                var second = Try(() => s.Get<string>("k", () =>
                {
                    Calls.Hit("f");
                    return null;
                }));
                return new List<object> { first, second, Calls.Count("f") };
            });
            Add(g, "Get(key, func) returning 0 is cached", () =>
            {
                var s = new MemoryCacheService();
                var first = s.Get("k", () =>
                {
                    Calls.Hit("f");
                    return 0;
                });
                var second = s.Get("k", () =>
                {
                    Calls.Hit("f");
                    return 0;
                });
                return new List<object> { first, second, Calls.Count("f") };
            });
            Add(g, "Get(key, func) that throws", () =>
            {
                var s = new MemoryCacheService();
                Func<int> f = () =>
                {
                    Calls.Hit("f");
                    throw new InvalidOperationException("fixture failure");
                };
                return new List<object> { Try(() => s.Get("k", f)), Try(() => s.Get("k", f)), Calls.Count("f") };
            });
            Add(g, "Get(key, null func)", () => new MemoryCacheService().Get<string>("k", null));
            Add(g, "Get(null key, func)", () => new MemoryCacheService().Get<string>(null, () => "v"));
            Add(g, "Get(key, func) with a value of another type present", () =>
            {
                var s = new MemoryCacheService();
                s.Set("k", "text");
                var r = s.Get("k", () => Calls.Hit("f"));
                return new List<object> { r, Calls.Count("f"), s.Get<string>("k") };
            });
            Add(g, "Get(key, func) nested for the same key on one thread", () =>
            {
                var s = new MemoryCacheService();
                var r = s.Get("k", () => s.Get("k", () => Calls.Hit("inner")) + 100);
                return new List<object> { r, s.Get<int>("k"), Calls.Count("inner") };
            });
            Add(g, "GetByInvocation(null)", () =>
            {
                new MemoryCacheService().GetByInvocation(null);
                return "no throw";
            });
            Add(g, "ctor(ObjectCache null) then Get", () => new MemoryCacheService((ObjectCache)null).Get<string>("k"));
        }

        // ---------- MemoryCacheService: lifetimes ----------

        private static object SetThenGet(MemoryCacheService s, int sleepMs)
        {
            var set = Try(() =>
            {
                s.Set("k", "v");
                return "set";
            });
            if (sleepMs > 0)
            {
                Thread.Sleep(sleepMs);
            }

            return new List<object> { set, Try(() => s.Get<string>("k")) };
        }

        private static void ExpirationCases()
        {
            const string g = "MemoryCacheService lifetime";
            Add(g, "ctor() immediate", () => SetThenGet(new MemoryCacheService(), 0));
            Add(g, "ctor(ObjectCache Default) immediate", () => SetThenGet(new MemoryCacheService(MemoryCache.Default), 0));
            Add(g, "ctor(ObjectCache new MemoryCache) immediate", () =>
            {
                using (var own = new MemoryCache("golden"))
                {
                    return SetThenGet(new MemoryCacheService(own), 0);
                }
            });
            Add(g, "ctor(long 1) after 50 ms", () => SetThenGet(new MemoryCacheService(1L), 50));
            Add(g, "ctor(long 0) immediate", () => SetThenGet(new MemoryCacheService(0L), 0));
            Add(g, "ctor(long -1) immediate", () => SetThenGet(new MemoryCacheService(-1L), 0));
            Add(g, "ctor(long one day) immediate", () => SetThenGet(new MemoryCacheService(TimeSpan.FromDays(1).Ticks), 0));
            Add(g, "ctor(long MaxValue)", () => SetThenGet(new MemoryCacheService(long.MaxValue), 0));
            Add(g, "ctor(long MinValue)", () => SetThenGet(new MemoryCacheService(long.MinValue), 0));
            Add(g, "ctor(0,0,1) immediate", () => SetThenGet(new MemoryCacheService(0, 0, 1), 0));
            Add(g, "ctor(0,0,0) immediate", () => SetThenGet(new MemoryCacheService(0, 0, 0), 0));
            Add(g, "ctor(-1,0,0) immediate", () => SetThenGet(new MemoryCacheService(-1, 0, 0), 0));
            Add(g, "ctor(int.MaxValue,0,0)", () => SetThenGet(new MemoryCacheService(int.MaxValue, 0, 0), 0));
            Add(g, "ctor(TimeSpan 100 ms) immediate, then after 600 ms", () =>
            {
                var s = new MemoryCacheService(TimeSpan.FromMilliseconds(100));
                s.Set("k", "v");
                var now = s.Get<string>("k");
                Thread.Sleep(600);
                return new List<object> { now, s.Get<string>("k") };
            });
            Add(g, "ctor(TimeSpan Zero) immediate", () => SetThenGet(new MemoryCacheService(TimeSpan.Zero), 0));
            Add(g, "ctor(TimeSpan -5 min) immediate", () => SetThenGet(new MemoryCacheService(TimeSpan.FromMinutes(-5)), 0));
            Add(g, "ctor(TimeSpan MaxValue)", () => SetThenGet(new MemoryCacheService(TimeSpan.MaxValue), 0));
            Add(g, "ctor(TimeSpan 400 days) immediate", () => SetThenGet(new MemoryCacheService(TimeSpan.FromDays(400)), 0));
        }

        // ---------- DefaultCacheKeyService keys ----------

        private static object Key(Func<IKeyed, int> call)
        {
            return Key(new Keyed(), call);
        }

        private static object Key(Keyed target, Func<IKeyed, int> call)
        {
            var capture = new KeyCapture();
            var proxy = new ProxyGenerator().CreateInterfaceProxyWithTarget<IKeyed>(target, capture);
            call(proxy);
            return capture.LastKey;
        }

        private static void KeyCases()
        {
            const string g = "DefaultCacheKeyService";
            var smile = char.ConvertFromUtf32(0x1F600);
            var eAcute = ((char)0xE9).ToString();

            Add(g, "None()", () => Key(k => k.None()));
            Add(g, "OneInt(1)", () => Key(k => k.OneInt(1)));
            Add(g, "OneInt(-1)", () => Key(k => k.OneInt(-1)));
            Add(g, "OneInt(int.MinValue)", () => Key(k => k.OneInt(int.MinValue)));
            Add(g, "Str(\"a\")", () => Key(k => k.Str("a")));
            Add(g, "Str(null)", () => Key(k => k.Str(null)));
            Add(g, "Str(\"\")", () => Key(k => k.Str("")));
            Add(g, "Str(special characters)", () => Key(k => k.Str("h" + eAcute + "llo " + smile + " <tag> & 'q' \"dq\" back" + (char)92 + "slash|semi;colon" + (char)10 + "line")));
            Add(g, "Str(\"a|b\") versus Two", () => Key(k => k.Str("a|b")));
            Add(g, "Two(1, \"x\")", () => Key(k => k.Two(1, "x")));
            Add(g, "Obj(null)", () => Key(k => k.Obj(null)));
            Add(g, "Obj(1)", () => Key(k => k.Obj(1)));
            Add(g, "Obj(\"1\")", () => Key(k => k.Obj("1")));
            Add(g, "Obj(new object())", () => Key(k => k.Obj(new object())));
            Add(g, "Generic<int>(1)", () => Key(k => k.Generic(1)));
            Add(g, "Generic<string>(\"1\")", () => Key(k => k.Generic("1")));
            Add(g, "Generic<object>(1)", () => Key(k => k.Generic<object>(1)));
            Add(g, "Generic2<int, List<string>>", () => Key(k => k.Generic2(1, new List<string> { "a" })));
            Add(g, "Arr(1,2,3)", () => Key(k => k.Arr(new[] { 1, 2, 3 })));
            Add(g, "Arr(empty)", () => Key(k => k.Arr(new int[0])));
            Add(g, "Arr(null)", () => Key(k => k.Arr(null)));
            Add(g, "ListOf(a, null)", () => Key(k => k.ListOf(new List<string> { "a", null })));
            Add(g, "Dict(b=2, a=1)", () => Key(k => k.Dict(new Dictionary<string, int> { { "b", 2 }, { "a", 1 } })));
            Add(g, "WithDto", () => Key(k => k.WithDto(new Dto { A = 1, B = "b", Field = 6 })));
            Add(g, "WithNode(chain)", () => Key(k => k.WithNode(new Node { V = 1, Next = new Node { V = 2 } })));
            Add(g, "WithNode(cycle)", () =>
            {
                var n = new Node { V = 1 };
                n.Next = n;
                return Key(k => k.WithNode(n));
            });
            Add(g, "WithAnimal(Dog)", () => Key(k => k.WithAnimal(new Dog { Name = "rex", Legs = 4 })));
            Add(g, "Date(2015-04-18 UTC)", () => Key(k => k.Date(new DateTime(2015, 4, 18, 12, 30, 45, 123, DateTimeKind.Utc))));
            Add(g, "Date(MinValue UTC)", () => Key(k => k.Date(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc))));
            Add(g, "WithGuid", () => Key(k => k.WithGuid(new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"))));
            Add(g, "WithEnum(Blue)", () => Key(k => k.WithEnum(Colour.Blue)));
            Add(g, "WithEnum(undefined 7)", () => Key(k => k.WithEnum((Colour)7)));
            Add(g, "WithDayOfWeek(Friday)", () => Key(k => k.WithDayOfWeek(DayOfWeek.Friday)));
            Add(g, "Dbl(0.1)", () => Key(k => k.Dbl(0.1)));
            Add(g, "Dbl(1e300)", () => Key(k => k.Dbl(1e300)));
            Add(g, "Dbl(-0.0)", () => Key(k => k.Dbl(-0.0)));
            Add(g, "Dbl(NaN)", () => Key(k => k.Dbl(double.NaN)));
            Add(g, "Dbl(PositiveInfinity)", () => Key(k => k.Dbl(double.PositiveInfinity)));
            Add(g, "Dbl(0.5) under de-DE", () =>
            {
                var saved = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                try
                {
                    return Key(k => k.Dbl(0.5));
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = saved;
                }
            });
            Add(g, "Dec(1.10)", () => Key(k => k.Dec(1.10m)));
            Add(g, "Chr('x')", () => Key(k => k.Chr('x')));
            Add(g, "Chr(NUL)", () => Key(k => k.Chr((char)0)));
            Add(g, "NullableInt(null)", () => Key(k => k.NullableInt(null)));
            Add(g, "NullableInt(3)", () => Key(k => k.NullableInt(3)));
            Add(g, "Overload(int 1)", () => Key(k => k.Overload(1)));
            Add(g, "Overload(long 1)", () => Key(k => k.Overload(1L)));
            Add(g, "WithTimeSpan(90 s)", () => Key(k => k.WithTimeSpan(TimeSpan.FromSeconds(90))));
            Add(g, "WithBool(true)", () => Key(k => k.WithBool(true)));
            Add(g, "WithParams(1,2)", () => Key(k => k.WithParams(1, 2)));
            Add(g, "WithOut", () => Key(k =>
            {
                int v;
                return k.WithOut(out v);
            }));
            Add(g, "WithRef(4)", () => Key(k =>
            {
                var v = 4;
                return k.WithRef(ref v);
            }));
            Add(g, "target with ToString override", () => Key(new NamedKeyed(), k => k.OneInt(1)));
            Add(g, "GenerateUniqueKeyForCall(null)", () => new DefaultCacheKeyService().GenerateUniqueKeyForCall(null));
        }

        // ---------- Interception through Autofac ----------

        private static IContainer Build()
        {
            var b = new ContainerBuilder();
            b.RegisterCachingModule();
            b.RegisterType<Svc>().As<ISvc>().EnableCacheInterception();
            b.RegisterType<NamedSvc>().As<INamed>().EnableCacheInterception();
            b.RegisterType<Svc>().AsSelf();
            return b.Build();
        }

        private static object Twice(Func<ISvc, object> call, string counter, int sleepMs = 0)
        {
            using (var c = Build())
            {
                var s = c.Resolve<ISvc>();
                var first = Try(() => Show(call(s)));
                if (sleepMs > 0)
                {
                    Thread.Sleep(sleepMs);
                }

                var second = Try(() => Show(call(s)));
                return new List<object> { first, second, Calls.Count(counter) };
            }
        }

        private static void InterceptionCases()
        {
            const string g = "Interception";
            Add(g, "Cached twice", () => Twice(s => s.Cached(), "Cached"));
            Add(g, "NotCached twice", () => Twice(s => s.NotCached(), "NotCached"));
            Add(g, "CachedOneTick twice, 50 ms apart", () => Twice(s => s.CachedOneTick(), "CachedOneTick", 50));
            Add(g, "CachedOneDay twice, 50 ms apart", () => Twice(s => s.CachedOneDay(), "CachedOneDay", 50));
            Add(g, "CachedOneSecond twice", () => Twice(s => s.CachedOneSecond(), "CachedOneSecond"));
            Add(g, "CachedZeroTicks twice", () => Twice(s => s.CachedZeroTicks(), "CachedZeroTicks"));
            Add(g, "CachedNegativeTicks twice", () => Twice(s => s.CachedNegativeTicks(), "CachedNegativeTicks"));
            Add(g, "WithArg 1, 1, 2", () =>
            {
                using (var c = Build())
                {
                    var s = c.Resolve<ISvc>();
                    return new List<object> { Try(() => s.WithArg(1)), Try(() => s.WithArg(1)), Try(() => s.WithArg(2)), Calls.Count("WithArg") };
                }
            });
            Add(g, "WithTwo (\"a|\", 1) and (\"a\", 1)", () =>
            {
                using (var c = Build())
                {
                    var s = c.Resolve<ISvc>();
                    return new List<object> { Try(() => s.WithTwo("a|", 1)), Try(() => s.WithTwo("a", 1)), Try(() => s.WithTwo(null, 1)), Calls.Count("WithTwo") };
                }
            });
            Add(g, "ReturnsNull twice", () => Twice(s => s.ReturnsNull(), "ReturnsNull"));
            Add(g, "VoidCached twice", () => Twice(s =>
            {
                s.VoidCached();
                return "returned";
            }, "VoidCached"));
            Add(g, "Throws twice", () => Twice(s => s.Throws(), "Throws"));
            Add(g, "Echo<int> 5, Echo<string> \"5\", Echo<long> 5, Echo<int> 5", () =>
            {
                using (var c = Build())
                {
                    var s = c.Resolve<ISvc>();
                    return new List<object>
                    {
                        Try(() => Show(s.Echo(5))), Try(() => Show(s.Echo("5"))), Try(() => Show(s.Echo(5L))), Try(() => Show(s.Echo(5))), Calls.Count("Echo"),
                    };
                }
            });
            Add(g, "Echo<string> null", () => Twice(s => s.Echo<string>(null), "Echo"));
            Add(g, "CachedTask twice", () =>
            {
                using (var c = Build())
                {
                    var s = c.Resolve<ISvc>();
                    var t1 = s.CachedTask();
                    var t2 = s.CachedTask();
                    return new List<object> { t1.Result, t2.Result, ReferenceEquals(t1, t2), Calls.Count("CachedTask") };
                }
            });
            Add(g, "AttrOnInterfaceOnly twice", () => Twice(s => s.AttrOnInterfaceOnly(), "AttrOnInterfaceOnly"));
            Add(g, "two resolutions share entries", () =>
            {
                using (var c = Build())
                {
                    var a = c.Resolve<ISvc>().Cached();
                    var b = c.Resolve<ISvc>().Cached();
                    return new List<object> { a, b, Calls.Count("Cached") };
                }
            });
            Add(g, "two containers share entries", () =>
            {
                int a, b;
                using (var c = Build())
                {
                    a = c.Resolve<ISvc>().Cached();
                }

                using (var c = Build())
                {
                    b = c.Resolve<ISvc>().Cached();
                }

                return new List<object> { a, b, Calls.Count("Cached") };
            });
            Add(g, "target ToString override splits entries per instance", () =>
            {
                using (var c = Build())
                {
                    var x = c.Resolve<INamed>();
                    var a = x.Cached();
                    var a2 = x.Cached();
                    var b = c.Resolve<INamed>().Cached();
                    return new List<object> { a, a2, b, Calls.Count("NamedSvc.Cached") };
                }
            });
            Add(g, "concrete Svc resolved as itself is not intercepted", () =>
            {
                using (var c = Build())
                {
                    var s = c.Resolve<Svc>();
                    return new List<object> { s.Cached(), s.Cached(), Calls.Count("Cached"), s.GetType().FullName };
                }
            });
            Add(g, "resolved ISvc is a proxy", () =>
            {
                using (var c = Build())
                {
                    var s = c.Resolve<ISvc>();
                    return new List<object> { s.GetType() == typeof(Svc), s.GetType().Namespace ?? "(none)" };
                }
            });
            Add(g, "interception without RegisterCachingModule", () =>
            {
                var b = new ContainerBuilder();
                b.RegisterType<Svc>().As<ISvc>().EnableCacheInterception();
                using (var c = b.Build())
                {
                    var resolved = Try(() =>
                    {
                        var s = c.Resolve<ISvc>();
                        return Show(s.Cached());
                    });
                    return new List<object> { resolved, Calls.Count("Cached") };
                }
            });
            Add(g, "durations handed to ICacheService", () =>
            {
                var rec = new RecordingCacheService();
                var b = new ContainerBuilder();
                b.RegisterCachingModule();
                b.RegisterInstance(rec).As<ICacheService>();
                b.RegisterType<Svc>().As<ISvc>().EnableCacheInterception();
                using (var c = b.Build())
                {
                    var s = c.Resolve<ISvc>();
                    s.Cached();
                    s.CachedOneTick();
                    s.CachedOneDay();
                    s.CachedOneSecond();
                    s.CachedZeroTicks();
                    s.CachedNegativeTicks();
                    s.NotCached();
                    return rec.Durations;
                }
            });
            Add(g, "Castle interface proxy with AOPCachingInterceptor, no Autofac", () =>
            {
                var proxy = new ProxyGenerator().CreateInterfaceProxyWithTarget<ISvc>(new Svc(), new AOPCachingInterceptor(new MemoryCacheService()));
                return new List<object> { proxy.Cached(), proxy.Cached(), Calls.Count("Cached") };
            });
            Add(g, "Castle class proxy of BaseSvc", () =>
            {
                var proxy = new ProxyGenerator().CreateClassProxy<BaseSvc>(new AOPCachingInterceptor(new MemoryCacheService()));
                return new List<object> { Try(() => proxy.V()), Try(() => proxy.V()), Calls.Count("BaseSvc.V"), Try(() => proxy.Plain()), Try(() => proxy.Plain()), Calls.Count("BaseSvc.Plain") };
            });
            Add(g, "Castle class proxy of DerivedSvc (inherited attribute)", () =>
            {
                var proxy = new ProxyGenerator().CreateClassProxy<DerivedSvc>(new AOPCachingInterceptor(new MemoryCacheService()));
                return new List<object> { Try(() => proxy.V()), Try(() => proxy.V()), Calls.Count("DerivedSvc.V") };
            });
            Add(g, "AOPCachingInterceptor(null) on a plain and a cached method", () =>
            {
                var proxy = new ProxyGenerator().CreateInterfaceProxyWithTarget<ISvc>(new Svc(), new AOPCachingInterceptor(null));
                return new List<object> { Try(() => proxy.NotCached()), Try(() => proxy.Cached()), Calls.Count("NotCached"), Calls.Count("Cached") };
            });
            Add(g, "cached entry visible in MemoryCache.Default", () =>
            {
                using (var c = Build())
                {
                    c.Resolve<ISvc>().WithArg(7);
                    return MemoryCache.Default.Select(kv => (object)(kv.Key + " => " + Convert.ToString(kv.Value, CultureInfo.InvariantCulture))).ToList();
                }
            });
        }

        // ---------- AutofacCachingModule and AutofacExtensions ----------

        private static void ModuleCases()
        {
            const string g = "AutofacCachingModule";
            Add(g, "BuildContainer resolves", () =>
            {
                using (var c = AutofacCachingModule.BuildContainer())
                {
                    return Obj(
                        "ICacheService", Try(() => c.Resolve<ICacheService>().GetType().FullName),
                        "AOPCachingInterceptor", Try(() => c.Resolve<AOPCachingInterceptor>().GetType().FullName),
                        "IKeyService registered", c.IsRegistered<IKeyService>(),
                        "MemoryCacheService registered as itself", c.IsRegistered<MemoryCacheService>(),
                        "ICacheService is a new instance per resolve", !ReferenceEquals(c.Resolve<ICacheService>(), c.Resolve<ICacheService>()));
                }
            });
            Add(g, "RegisterCachingModule twice", () =>
            {
                var b = new ContainerBuilder();
                b.RegisterCachingModule();
                b.RegisterCachingModule();
                using (var c = b.Build())
                {
                    return c.Resolve<ICacheService>().GetType().FullName;
                }
            });
            Add(g, "RegisterCachingModule(null)", () =>
            {
                AutofacExtensions.RegisterCachingModule(null);
                return "no throw";
            });
            Add(g, "HasCacheAttribute(IComponentRegistration)", () =>
            {
                var b = new ContainerBuilder();
                b.RegisterType<Svc>().As<ISvc>();
                b.RegisterType<NoCacheSvc>().As<INoCache>();
                b.RegisterType<DerivedSvc>().AsSelf();
                using (var c = b.Build())
                {
                    var regs = c.ComponentRegistry.Registrations;
                    return Obj(
                        "Svc", regs.First(r => r.Activator.LimitType == typeof(Svc)).HasCacheAttribute(),
                        "NoCacheSvc", regs.First(r => r.Activator.LimitType == typeof(NoCacheSvc)).HasCacheAttribute(),
                        "DerivedSvc", regs.First(r => r.Activator.LimitType == typeof(DerivedSvc)).HasCacheAttribute());
                }
            });
            Add(g, "HasCacheAttribute(IComponentRegistration null)", () => ((Autofac.Core.IComponentRegistration)null).HasCacheAttribute());
        }
    }
}
