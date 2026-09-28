using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CachingServiceWithAOP.CachingServices
{
    /// <summary>
    /// Writes a value as .NET Framework's System.Web.Script.Serialization.JavaScriptSerializer.Serialize did (the 1.x cache
    /// keys were made with it, and that assembly exists only on .NET Framework): the same escaping, dates as
    /// \/Date(milliseconds)\/, enums as numbers, doubles in .NET Framework's round-trip form, objects as their public
    /// fields and then their public properties, the same limits and the same exception types. tests/Golden holds 1.0.1's keys.
    /// </summary>
    internal static class ScriptJson
    {
        internal const int RecursionLimit = 100;
        internal const int MaxJsonLength = 2097152;

        private const char Backslash = (char)92;

        // Set in Exception.Data on the exceptions JavaScriptSerializer threw for an argument it could not write (a cycle,
        // the recursion limit, a dictionary key that is not a string, the length limit, a long or ulong enum). The types and
        // messages stay 1.0.1's; the marker lets MemoryCacheService run such a call uncached without catching anything else.
        internal const string UncacheableMarker = "CachingServiceWithAOP.UncacheableArgument";

        private static readonly bool IsNetFramework = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal);
        private static readonly long UnixEpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

        public static bool IsUncacheable(Exception e)
        {
            return e.Data.Contains(UncacheableMarker);
        }

        private static T Uncacheable<T>(T e)
            where T : Exception
        {
            e.Data[UncacheableMarker] = true;
            return e;
        }

        public static string Serialize(object? value)
        {
            var sb = new StringBuilder();
            var inUse = new HashSet<object>(ReferenceComparer.Instance);
            Write(sb, value, 0, inUse);
            if (sb.Length > MaxJsonLength)
            {
                throw Uncacheable(new InvalidOperationException("Error during serialization or deserialization using the JSON JavaScriptSerializer. The length of the string exceeds the value set on the maxJsonLength property."));
            }

            return sb.ToString();
        }

        private static void Write(StringBuilder sb, object? o, int depth, HashSet<object> inUse)
        {
            if (++depth > RecursionLimit)
            {
                throw Uncacheable(new ArgumentException("RecursionLimit exceeded."));
            }

            switch (o)
            {
                case null:
                case DBNull:
                    sb.Append("null");
                    return;
                case string s:
                    Quote(sb, s);
                    return;
                case char c:
                    if (c == (char)0)
                    {
                        sb.Append("null");
                    }
                    else
                    {
                        Quote(sb, c.ToString());
                    }

                    return;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    return;
                case DateTime dt:
                    WriteDate(sb, dt.ToUniversalTime());
                    return;
                case DateTimeOffset dto:
                    // The Framework wrote the instant, so one instant at any offset is one key.
                    WriteDate(sb, dto.UtcDateTime);
                    return;
                case Guid g:
                    Quote(sb, g.ToString());
                    return;
                case Uri u:
                    // Written as the Framework did: quoted, without JavaScript escaping.
                    sb.Append('"').Append(u.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped)).Append('"');
                    return;
                case double d:
                    sb.Append(RoundTrip(d));
                    return;
                case float f:
                    sb.Append(RoundTrip(f));
                    return;
                case Enum e:
                    var underlying = Enum.GetUnderlyingType(e.GetType());
                    if (underlying == typeof(long) || underlying == typeof(ulong))
                    {
                        throw Uncacheable(new InvalidOperationException("Enums based on System.Int64 or System.UInt64 are not JSON-serializable because JavaScript does not support the necessary precision."));
                    }

                    sb.Append(e.ToString("D"));
                    return;
                case TimeSpan ts:
                    WriteTimeSpan(sb, ts, depth);
                    return;
            }

            var type = o.GetType();
            if (type.IsPrimitive || o is decimal)
            {
                // IntPtr and UIntPtr are primitive but not IConvertible.
                sb.Append(o is IConvertible convertible ? convertible.ToString(CultureInfo.InvariantCulture) : o.ToString());
                return;
            }

            if (inUse.Contains(o))
            {
                throw Uncacheable(new InvalidOperationException("A circular reference was detected while serializing an object of type '" + type.FullName + "'."));
            }

            inUse.Add(o);
            try
            {
                if (o is IDictionary dictionary)
                {
                    WriteDictionary(sb, dictionary, depth, inUse);
                }
                else if (o is IEnumerable enumerable)
                {
                    sb.Append('[');
                    var first = true;
                    foreach (var item in enumerable)
                    {
                        if (!first)
                        {
                            sb.Append(',');
                        }

                        first = false;
                        Write(sb, item, depth, inUse);
                    }

                    sb.Append(']');
                }
                else
                {
                    WriteObject(sb, o, type, depth, inUse);
                }
            }
            finally
            {
                inUse.Remove(o);
            }
        }

        private static void WriteDictionary(StringBuilder sb, IDictionary dictionary, int depth, HashSet<object> inUse)
        {
            const string ServerTypeFieldName = "__type";
            sb.Append('{');
            var first = true;

            // The Framework wrote a "__type" entry first.
            if (dictionary.Contains(ServerTypeFieldName))
            {
                first = false;
                Quote(sb, ServerTypeFieldName);
                sb.Append(':');
                Write(sb, dictionary[ServerTypeFieldName], depth, inUse);
            }

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not string key)
                {
                    throw Uncacheable(new ArgumentException("Type '" + dictionary.GetType().FullName + "' is not supported for serialization/deserialization of a dictionary, keys must be strings or objects."));
                }

                if (key == ServerTypeFieldName)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                Quote(sb, key);
                sb.Append(':');
                Write(sb, entry.Value, depth, inUse);
            }

            sb.Append('}');
        }

        private static void WriteObject(StringBuilder sb, object o, Type type, int depth, HashSet<object> inUse)
        {
            sb.Append('{');
            var first = true;
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsIgnored(field))
                {
                    continue;
                }

                Member(sb, ref first, field.Name);
                Write(sb, field.GetValue(o), depth, inUse);
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var getter = property.GetGetMethod();
                if (getter == null || getter.GetParameters().Length > 0 || IsIgnored(property))
                {
                    continue;
                }

                Member(sb, ref first, property.Name);
                Write(sb, getter.Invoke(o, null), depth, inUse);
            }

            sb.Append('}');
        }

        // TimeSpan as .NET Framework's reflection saw it: its eleven public properties, the totals computed as the Framework
        // computed them. .NET 7 added four properties and .NET Core computes TotalHours exactly, so reflection would give
        // other keys there (the golden case WithTimeSpan(90 s) records the Framework text).
        private static void WriteTimeSpan(StringBuilder sb, TimeSpan ts, int depth)
        {
            // The Framework wrote the TimeSpan's members one level deeper.
            if (depth + 1 > RecursionLimit)
            {
                throw Uncacheable(new ArgumentException("RecursionLimit exceeded."));
            }

            const long TicksPerMillisecond = 10000;
            const double MaxMilliseconds = long.MaxValue / TicksPerMillisecond;
            const double MinMilliseconds = long.MinValue / TicksPerMillisecond;
            double ticks = ts.Ticks;
            var totalMilliseconds = ticks * (1.0 / TicksPerMillisecond);
            totalMilliseconds = totalMilliseconds > MaxMilliseconds ? MaxMilliseconds : totalMilliseconds < MinMilliseconds ? MinMilliseconds : totalMilliseconds;

            sb.Append('{');
            var first = true;
            Member(sb, ref first, "Ticks");
            sb.Append(ts.Ticks.ToString(CultureInfo.InvariantCulture));
            Member(sb, ref first, "Days");
            sb.Append(ts.Days.ToString(CultureInfo.InvariantCulture));
            Member(sb, ref first, "Hours");
            sb.Append(ts.Hours.ToString(CultureInfo.InvariantCulture));
            Member(sb, ref first, "Milliseconds");
            sb.Append(ts.Milliseconds.ToString(CultureInfo.InvariantCulture));
            Member(sb, ref first, "Minutes");
            sb.Append(ts.Minutes.ToString(CultureInfo.InvariantCulture));
            Member(sb, ref first, "Seconds");
            sb.Append(ts.Seconds.ToString(CultureInfo.InvariantCulture));
            Member(sb, ref first, "TotalDays");
            sb.Append(RoundTrip(ticks * (1.0 / 864000000000)));
            Member(sb, ref first, "TotalHours");
            sb.Append(RoundTrip(ticks * (1.0 / 36000000000)));
            Member(sb, ref first, "TotalMilliseconds");
            sb.Append(RoundTrip(totalMilliseconds));
            Member(sb, ref first, "TotalMinutes");
            sb.Append(RoundTrip(ticks * (1.0 / 600000000)));
            Member(sb, ref first, "TotalSeconds");
            sb.Append(RoundTrip(ticks * (1.0 / 10000000)));
            sb.Append('}');
        }

        private static void WriteDate(StringBuilder sb, DateTime utc)
        {
            sb.Append('"').Append(Backslash).Append("/Date(")
                .Append(((utc.Ticks - UnixEpochTicks) / 10000).ToString(CultureInfo.InvariantCulture))
                .Append(')').Append(Backslash).Append("/\"");
        }

        private static void Member(StringBuilder sb, ref bool first, string name)
        {
            if (!first)
            {
                sb.Append(',');
            }

            first = false;
            Quote(sb, name);
            sb.Append(':');
        }

        // JavaScriptSerializer skipped members marked [ScriptIgnore] (System.Web.Extensions); matched by name here.
        private static bool IsIgnored(MemberInfo member)
        {
            return member.GetCustomAttributes(true).Any(a => a.GetType().Name == "ScriptIgnoreAttribute");
        }

        // JavaScriptSerializer wrote ToString("r"). On .NET Framework that is used as it is. .NET's "R" is the shortest
        // round-trip form and writes -0 as "-0", so there the Framework rule (15 significant digits when they round-trip,
        // else 17) is applied by hand. .NET formats the 17th digit exactly where the Framework rounded it, so a small share
        // of doubles and floats differ from 1.0.1 in their last digit on .NET; keys stay one per value.
        private static string RoundTrip(double d)
        {
            if (IsNetFramework)
            {
                return d.ToString("R", CultureInfo.InvariantCulture);
            }

            if (d == 0)
            {
                return "0";
            }

            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                return d.ToString(CultureInfo.InvariantCulture);
            }

            var s = d.ToString("G15", CultureInfo.InvariantCulture);
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var back) && back == d ? s : d.ToString("G17", CultureInfo.InvariantCulture);
        }

        private static string RoundTrip(float f)
        {
            if (IsNetFramework)
            {
                return f.ToString("R", CultureInfo.InvariantCulture);
            }

            if (f == 0)
            {
                return "0";
            }

            if (float.IsNaN(f) || float.IsInfinity(f))
            {
                return f.ToString(CultureInfo.InvariantCulture);
            }

            var s = f.ToString("G7", CultureInfo.InvariantCulture);
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var back) && back == f ? s : f.ToString("G9", CultureInfo.InvariantCulture);
        }

        private static void Quote(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\b':
                        sb.Append(Backslash).Append('b');
                        break;
                    case '\t':
                        sb.Append(Backslash).Append('t');
                        break;
                    case '\n':
                        sb.Append(Backslash).Append('n');
                        break;
                    case '\f':
                        sb.Append(Backslash).Append('f');
                        break;
                    case '\r':
                        sb.Append(Backslash).Append('r');
                        break;
                    case '"':
                        sb.Append(Backslash).Append('"');
                        break;
                    case Backslash:
                        sb.Append(Backslash).Append(Backslash);
                        break;
                    default:
                        if (c < ' ' || c == '\'' || c == '<' || c == '>' || c == '&' || c == (char)0x85 || c == (char)0x2028 || c == (char)0x2029)
                        {
                            sb.Append(Backslash).Append('u').Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            sb.Append('"');
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new();

            public new bool Equals(object? x, object? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
