using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autofac;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Extensions;
using NUnit.Framework;

namespace CachingServiceWithAOP.Tests
{
    public enum LongBased : long
    {
        A = 1,
    }

    public interface IReviewService
    {
        int WithPointer(IntPtr p);

        int WithLongBased(LongBased e);

        int WithSequence(IEnumerable<int> values);

        int WithDouble(double d);
    }

    public class ReviewService : IReviewService
    {
        public int Calls;

        [Cache]
        public int WithPointer(IntPtr p)
        {
            return ++Calls;
        }

        [Cache]
        public int WithLongBased(LongBased e)
        {
            return ++Calls;
        }

        [Cache]
        public int WithSequence(IEnumerable<int> values)
        {
            return ++Calls;
        }

        [Cache]
        public int WithDouble(double d)
        {
            return ++Calls;
        }
    }

    // The Phase 3 review's findings (ai-docs/notes/2026-09-27-phase-3-review-findings.md), one test each.
    [TestFixture]
    public class ReviewTests
    {
        private static readonly string Backslash = ((char)92).ToString();

        private IContainer _container = null!;
        private ReviewService _target = null!;
        private IReviewService _service = null!;

        [SetUp]
        public void SetUp()
        {
            SimpleTestingHelpers.ClearKeysFromMemcache();
            _target = new ReviewService();
            var builder = new ContainerBuilder();
            builder.RegisterCachingModule();
            builder.RegisterInstance(_target).As<IReviewService>().EnableCacheInterception();
            _container = builder.Build();
            _service = _container.Resolve<IReviewService>();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }

        [Test]
        public void R1_a_cached_null_never_reaches_callers_as_the_internal_marker()
        {
            var cache = new MemoryCacheService();
            var key = SimpleTestingHelpers.GenerateUniqueKey();
            Assert.That(cache.Get<object>(key, () => null!), Is.Null);
            Assert.That(cache.Get<object>(key), Is.Null);
            Assert.That(cache.Get<object>(key, () => "x"), Is.Null, "the cached null answers");
            Assert.That(cache.Get<int>(key, () => 5), Is.EqualTo(5), "a value type cannot take the cached null: a miss");
        }

        [Test]
        public void R2_IntPtr_arguments_make_keys()
        {
            Assert.That(ScriptJson.Serialize(new IntPtr(7)), Is.EqualTo("7"));
            Assert.That(_service.WithPointer(new IntPtr(7)), Is.EqualTo(1));
            Assert.That(_service.WithPointer(new IntPtr(7)), Is.EqualTo(1));
        }

        [Test]
        public void R3_double_extremes_make_keys()
        {
            Assert.That(ScriptJson.Serialize(double.MaxValue), Is.EqualTo("1.7976931348623157E+308"));
            Assert.That(ScriptJson.Serialize(double.MinValue), Is.EqualTo("-1.7976931348623157E+308"));
            Assert.That(_service.WithDouble(double.MaxValue), Is.EqualTo(1));
            Assert.That(_service.WithDouble(double.MaxValue), Is.EqualTo(1));
        }

        [Test]
        public void R4_one_instant_at_any_offset_is_one_key()
        {
            var utc = new DateTimeOffset(2015, 4, 18, 12, 0, 0, TimeSpan.Zero);
            var plus3 = utc.ToOffset(TimeSpan.FromHours(3));
            var expected = "\"" + Backslash + "/Date(1429358400000)" + Backslash + "/\"";
            Assert.That(ScriptJson.Serialize(utc), Is.EqualTo(expected));
            Assert.That(ScriptJson.Serialize(plus3), Is.EqualTo(expected));
        }

        [Test]
        public void R5_uris_are_written_without_javascript_escaping()
        {
            Assert.That(ScriptJson.Serialize(new Uri("http://x.test/a?b=1&c=2")), Is.EqualTo("\"http://x.test/a?b=1&c=2\""));
        }

        [Test]
        public void R6_a_type_entry_is_written_first()
        {
            var d = new Dictionary<string, object> { ["a"] = 1, ["__type"] = "T", ["b"] = 2 };
            Assert.That(ScriptJson.Serialize(d), Is.EqualTo("{\"__type\":\"T\",\"a\":1,\"b\":2}"));
        }

        [Test]
        public void R7_long_enums_throw_in_the_key_service_and_run_uncached()
        {
            var e = Assert.Throws<InvalidOperationException>(() => ScriptJson.Serialize(LongBased.A));
            Assert.That(e!.Message, Does.StartWith("Enums based on System.Int64 or System.UInt64"));
            Assert.That(_service.WithLongBased(LongBased.A), Is.EqualTo(1));
            Assert.That(_service.WithLongBased(LongBased.A), Is.EqualTo(2));
        }

        [Test]
        public void R9_an_argument_that_throws_its_own_error_still_throws()
        {
            static IEnumerable<int> Broken()
            {
                yield return 1;
                throw new ArgumentException("the caller's own error");
            }

            var e = Assert.Throws<ArgumentException>(() => _service.WithSequence(Broken()));
            Assert.That(e!.Message, Does.StartWith("the caller's own error"));
            Assert.That(_target.Calls, Is.EqualTo(0));
        }

        [Test]
        public void R11_a_TimeSpan_at_the_depth_limit_throws_like_an_object()
        {
            object value = TimeSpan.FromSeconds(1);
            for (var i = 0; i < ScriptJson.RecursionLimit - 1; i++)
            {
                value = new[] { value };
            }

            Assert.Throws<ArgumentException>(() => ScriptJson.Serialize(value));
        }

#if NETFRAMEWORK
        // The differential test the review asked for: ScriptJson against the real JavaScriptSerializer on .NET Framework,
        // over edge values and seeded random numbers, strings and structures.
        [Test]
        public void R12_ScriptJson_writes_what_JavaScriptSerializer_wrote()
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var random = new Random(20260927);
            var values = new List<object?>
            {
                null, "", "a", 'x', (char)0, true, 0, -1, int.MinValue, long.MaxValue, ulong.MaxValue, (byte)7, (sbyte)-7, (short)3,
                1.5m, 1.10m, decimal.MaxValue, 0.1, -0.0, 1e-7, double.Epsilon, double.MaxValue, double.MinValue, double.NaN,
                double.PositiveInfinity, 0.3f, float.MaxValue, 3004.953125f, new IntPtr(7), new UIntPtr(8),
                new DateTime(2015, 4, 18, 1, 2, 3, DateTimeKind.Utc), DateTime.MinValue.ToUniversalTime(),
                new DateTimeOffset(2015, 4, 18, 1, 2, 3, TimeSpan.FromHours(-5)), Guid.Empty, new Uri("http://x.test/a?b=1&c=<2>'"),
                DayOfWeek.Friday, (DayOfWeek)99, AttributeTargets.Class | AttributeTargets.Method, TimeSpan.FromMinutes(-90), TimeSpan.MaxValue,
                new byte[] { 1, 2 }, new List<string?> { "a", null }, new Hashtable { ["k"] = 1 },
                new Dictionary<string, object> { ["a"] = 1, ["__type"] = "T" }, new KeyValuePair<string, int>("k", 1),
                Tuple.Create(1, "a"), new { A = 1, B = "b" }, (int?)3, new Dto2 { Field = 1, Prop = "p" },
            };

            var chars = Enumerable.Range(0, 0x100).Select(c => (char)c).Concat(new[] { (char)0x2028, (char)0x2029, (char)0xD800, (char)0xE9 }).ToArray();
            for (var i = 0; i < 1000; i++)
            {
                values.Add(new string(Enumerable.Range(0, random.Next(0, 12)).Select(_ => chars[random.Next(chars.Length)]).ToArray()));
                values.Add(BitConverter.Int64BitsToDouble((long)(random.NextDouble() * long.MaxValue)) * (random.Next(2) == 0 ? 1 : -1));
                values.Add((float)(random.NextDouble() * Math.Pow(10, random.Next(-10, 20))));
            }

            var failures = new List<string>();
            foreach (var v in values)
            {
                string expected;
                try
                {
                    expected = serializer.Serialize(v);
                }
                catch (Exception e)
                {
                    expected = "throws " + e.GetType().FullName;
                }

                string actual;
                try
                {
                    actual = ScriptJson.Serialize(v);
                }
                catch (Exception e)
                {
                    actual = "throws " + e.GetType().FullName;
                }

                if (expected != actual)
                {
                    failures.Add(Convert.ToString(v, CultureInfo.InvariantCulture) + ": expected " + expected + ", got " + actual);
                }
            }

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures.Take(20)));
        }

        public class Dto2
        {
            public int Field;

            public string? Prop { get; set; }

            public int this[int i] => i;

            public static int Static => 1;
        }
#endif
    }
}
