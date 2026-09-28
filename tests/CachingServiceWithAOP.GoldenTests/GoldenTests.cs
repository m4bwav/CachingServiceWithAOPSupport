using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using GoldenCapture;
using NUnit.Framework;

namespace CachingServiceWithAOP.GoldenTests
{
    /// <summary>
    /// Runs the Phase 0 capture's 158 cases against 2.x and compares every answer with tests/Golden/1.0.1.net48-windows.json,
    /// on every runtime (plan D1; the net10.0 recording is only the record of 1.0.1 failing there, E3). The only allowed
    /// differences are the named exceptions below, each ruled on in the plan.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class GoldenTests
    {
        // E1: .NET Core runtimes add " (Parameter 'name')" to ArgumentException messages.
        private static readonly Regex ParameterSuffix = new(@" \(Parameter '[^']*'\)", RegexOptions.Compiled);

        // E2: errors raised by Autofac itself follow its current major; compared as "throws" only.
        private static readonly HashSet<string> AutofacErrorCases = new(StringComparer.Ordinal)
        {
            "Interception | interception without RegisterCachingModule",
            "AutofacCachingModule | RegisterCachingModule(null)",
        };

        // E4: the fixes of plan items 2 to 7, each case with the answer 2.x gives instead.
        private static readonly Dictionary<string, string> Fixed = new(StringComparer.Ordinal)
        {
            ["Interception | ReturnsNull twice"] = "[null, null, 1]",
            ["Interception | Echo<string> null"] = "[null, null, 1]",
            ["Interception | VoidCached twice"] = "[\"returned\", \"returned\", 2]",
            ["MemoryCacheService | Get(key, func) returning null"] = "[null, null, 1]",
            ["MemoryCacheService | Get(key, func) with a value of another type present"] = "[1, 1, null]",
            ["MemoryCacheService lifetime | ctor(ObjectCache Default) immediate"] = "[\"set\", \"v\"]",
            ["MemoryCacheService lifetime | ctor(ObjectCache new MemoryCache) immediate"] = "[\"set\", \"v\"]",
            ["MemoryCacheService lifetime | ctor(long MaxValue)"] = "[\"set\", \"v\"]",
            ["MemoryCacheService lifetime | ctor(TimeSpan MaxValue)"] = "[\"set\", \"v\"]",
            ["MemoryCacheService lifetime | ctor(long MinValue)"] = "[\"set\", null]",
        };

        private static List<Case>? _actual;
        private static Dictionary<string, JsonElement>? _recorded;

        [OneTimeSetUp]
        public static void RunCapture()
        {
            var saved = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                _actual = Cases.Run().ToList();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = saved;
            }

            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Golden", "1.0.1.net48-windows.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            _recorded = doc.RootElement.GetProperty("cases").EnumerateArray()
                .ToDictionary(c => Key(c.GetProperty("group").GetString()!, c.GetProperty("name").GetString()!), c => c.GetProperty("result").Clone(), StringComparer.Ordinal);
        }

        private static string Key(string group, string name)
        {
            return group + " | " + name;
        }

        [Test]
        public void Every_recorded_case_runs()
        {
            Assert.That(_actual!.Select(c => Key(c.Group, c.Name)), Is.EquivalentTo(_recorded!.Keys));
        }

        [Test]
        public void Named_exceptions_name_recorded_cases()
        {
            Assert.That(Fixed.Keys.Concat(AutofacErrorCases), Is.SubsetOf(_recorded!.Keys));
        }

        [Test]
        public void Every_answer_matches_1_0_1_on_net48_apart_from_the_named_exceptions()
        {
            var failures = new List<string>();
            foreach (var c in _actual!)
            {
                var key = Key(c.Group, c.Name);
                var actualText = ParameterSuffix.Replace(Json.Write(c.Result), string.Empty);
                using var actual = JsonDocument.Parse(actualText);

                if (AutofacErrorCases.Contains(key))
                {
                    if (!JsonEqual(_recorded![key], actual.RootElement, throwsOnly: true))
                    {
                        failures.Add(key + " (E2): expected " + _recorded[key].GetRawText() + " with any throw, got " + actualText.Trim());
                    }

                    continue;
                }

                if (Fixed.TryGetValue(key, out var fixedText))
                {
                    using var expectedFixed = JsonDocument.Parse(fixedText);
                    if (!JsonEqual(expectedFixed.RootElement, actual.RootElement))
                    {
                        failures.Add(key + " (E4): expected " + fixedText + ", got " + actualText.Trim());
                    }

                    continue;
                }

                if (!JsonEqual(_recorded![key], actual.RootElement))
                {
                    failures.Add(key + ": expected " + _recorded[key].GetRawText() + ", got " + actualText.Trim());
                }
            }

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }

        private static bool IsThrow(JsonElement e)
        {
            return e.ValueKind == JsonValueKind.Object && e.TryGetProperty("$throws", out _);
        }

        private static bool JsonEqual(JsonElement a, JsonElement b, bool throwsOnly = false)
        {
            if (throwsOnly && IsThrow(a) && IsThrow(b))
            {
                return true;
            }

            if (a.ValueKind != b.ValueKind)
            {
                return false;
            }

            switch (a.ValueKind)
            {
                case JsonValueKind.Object:
                    var ap = a.EnumerateObject().ToList();
                    var bp = b.EnumerateObject().ToList();
                    return ap.Count == bp.Count && ap.Zip(bp, (x, y) => x.Name == y.Name && JsonEqual(x.Value, y.Value, throwsOnly)).All(ok => ok);
                case JsonValueKind.Array:
                    var ai = a.EnumerateArray().ToList();
                    var bi = b.EnumerateArray().ToList();
                    return ai.Count == bi.Count && ai.Zip(bi, (x, y) => JsonEqual(x, y, throwsOnly)).All(ok => ok);
                case JsonValueKind.String:
                    return string.Equals(ParameterSuffix.Replace(a.GetString()!, string.Empty), b.GetString(), StringComparison.Ordinal);
                default:
                    return a.GetRawText() == b.GetRawText();
            }
        }
    }
}
