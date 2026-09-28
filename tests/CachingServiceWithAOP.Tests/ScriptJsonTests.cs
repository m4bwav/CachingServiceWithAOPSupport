using System;
using System.Collections.Generic;
using System.Linq;
using CachingServiceWithAOP.CachingServices;
using NUnit.Framework;

namespace CachingServiceWithAOP.Tests
{
    // The key writer beyond the golden cases; expected strings are what JavaScriptSerializer.Serialize returned on .NET Framework.
    [TestFixture]
    public class ScriptJsonTests
    {
        private static readonly string Backslash = ((char)92).ToString();

        [Test]
        public void Line_and_paragraph_separators_and_next_line_are_escaped()
        {
            var text = new string(new[] { (char)0x2028, (char)0x2029, (char)0x85, (char)1 });
            var expected = "\"" + string.Concat(new[] { "2028", "2029", "0085", "0001" }.Select(h => Backslash + "u" + h)) + "\"";
            Assert.That(ScriptJson.Serialize(text), Is.EqualTo(expected));
        }

        [Test]
        public void Floats_use_the_framework_round_trip_form()
        {
            Assert.That(ScriptJson.Serialize(0.1f), Is.EqualTo("0.1"));
            Assert.That(ScriptJson.Serialize(-0.0f), Is.EqualTo("0"));
            Assert.That(ScriptJson.Serialize(float.NaN), Is.EqualTo("NaN"));
            Assert.That(ScriptJson.Serialize(double.NegativeInfinity), Is.EqualTo("-Infinity"));
        }

        [Test]
        public void Negative_and_large_TimeSpans_keep_the_framework_shape()
        {
            var json = ScriptJson.Serialize(TimeSpan.MaxValue);
            Assert.That(json, Does.StartWith("{\"Ticks\":9223372036854775807,\"Days\":10675199,"));
            Assert.That(json, Does.Contain("\"TotalMilliseconds\":922337203685477,"));
            Assert.That(ScriptJson.Serialize(TimeSpan.FromMinutes(-1)), Does.Contain("\"TotalMinutes\":-1,"));
        }

        [Test]
        public void Nesting_deeper_than_the_recursion_limit_throws_ArgumentException()
        {
            object value = 1;
            for (var i = 0; i < ScriptJson.RecursionLimit; i++)
            {
                value = new[] { value };
            }

            Assert.Throws<ArgumentException>(() => ScriptJson.Serialize(value));
        }

        [Test]
        public void Output_longer_than_maxJsonLength_throws_InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => ScriptJson.Serialize(new string('a', ScriptJson.MaxJsonLength)));
        }

        [Test]
        public void A_value_seen_twice_but_not_nested_is_not_a_cycle()
        {
            var shared = new List<int> { 1 };
            Assert.That(ScriptJson.Serialize(new[] { shared, shared }), Is.EqualTo("[[1],[1]]"));
        }
    }
}
