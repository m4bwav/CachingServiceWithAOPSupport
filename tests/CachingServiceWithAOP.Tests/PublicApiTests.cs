using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CachingServiceWithAOP.Extensions;
using NUnit.Framework;

namespace CachingServiceWithAOP.Tests
{
    // Every line of PublicApi-1.0.1.txt (listed by reflection from the published 1.0.1 DLL, tests/Golden/ApiList) must
    // still be true of 2.x: type names, members and parameter names, which callers use in named arguments.
    // The listing logic is the same as tests/Golden/ApiList/Program.cs.
    [TestFixture]
    public class PublicApiTests
    {
        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        [Test]
        public void Every_1_0_1_api_line_still_exists()
        {
            var baseline = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, "PublicApi-1.0.1.txt"))
                .Where(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal));
            var current = new HashSet<string>(List(typeof(ReflectionExtensions).Assembly), StringComparer.Ordinal);

            var missing = baseline.Where(l => !current.Contains(l)).ToList();
#if NET
            // System.Attribute implements _Attribute only on .NET Framework; the line comes from the runtime, not the package.
            missing.Remove("CachingServiceWithAOP.CacheAttribute implements System.Runtime.InteropServices._Attribute");
#endif
            Assert.That(missing, Is.Empty, string.Join(Environment.NewLine, missing));
        }

        [Test]
        public void No_public_member_was_added()
        {
            var baseline = new HashSet<string>(File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, "PublicApi-1.0.1.txt")), StringComparer.Ordinal);
            var added = List(typeof(ReflectionExtensions).Assembly).Where(l => !baseline.Contains(l)).ToList();
            Assert.That(added, Is.Empty, string.Join(Environment.NewLine, added));
        }

        [Test]
        public void CacheAttribute_keeps_1_0_1_usage()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(typeof(CacheAttribute), typeof(AttributeUsageAttribute))!;
            Assert.That(usage.ValidOn, Is.EqualTo(AttributeTargets.All));
            Assert.That(usage.AllowMultiple, Is.False);
            Assert.That(usage.Inherited, Is.True);
            Assert.That(typeof(CacheAttribute).IsSealed, Is.False);
        }

        [Test]
        public void No_public_type_was_added()
        {
            var types = typeof(ReflectionExtensions).Assembly.GetExportedTypes().Select(t => t.FullName).OrderBy(n => n, StringComparer.Ordinal);
            Assert.That(types, Is.EqualTo(new[]
            {
                "CachingServiceWithAOP.AOPCachingInterceptor",
                "CachingServiceWithAOP.AutofacCachingModule",
                "CachingServiceWithAOP.CacheAttribute",
                "CachingServiceWithAOP.CachingServices.DefaultCacheKeyService",
                "CachingServiceWithAOP.CachingServices.ICacheService",
                "CachingServiceWithAOP.CachingServices.IKeyService",
                "CachingServiceWithAOP.CachingServices.MemoryCacheService",
                "CachingServiceWithAOP.Extensions.AutofacExtensions",
                "CachingServiceWithAOP.Extensions.ReflectionExtensions",
            }));
        }

        private static List<string> List(Assembly asm)
        {
            var lines = new List<string>();
            foreach (var t in asm.GetExportedTypes())
            {
                var kind = t.IsInterface ? "interface" : t.IsEnum ? "enum" : t.IsValueType ? "struct" : t.IsAbstract && t.IsSealed ? "static class" : "class";
                lines.Add(Name(t) + " type " + kind);
                if (t.BaseType != null && t.BaseType != typeof(object))
                {
                    lines.Add(Name(t) + " base " + Name(t.BaseType));
                }

                foreach (var i in t.GetInterfaces())
                {
                    lines.Add(Name(t) + " implements " + Name(i));
                }

                foreach (var c in t.GetConstructors(Declared))
                {
                    lines.Add(Name(t) + " constructor .ctor(" + Params(c) + ")");
                }

                foreach (var m in t.GetMethods(Declared | BindingFlags.NonPublic).Where(m => (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly) && !m.IsSpecialName))
                {
                    var access = m.IsPublic ? "" : "protected ";
                    var stat = m.IsStatic ? "static " : "";
                    var ext = m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false) ? "extension " : "";
                    var generic = m.IsGenericMethodDefinition ? "<" + string.Join(", ", m.GetGenericArguments().Select(a => a.Name)) + ">" : "";
                    lines.Add(Name(t) + " method " + access + stat + ext + Name(m.ReturnType) + " " + m.Name + generic + "(" + Params(m) + ")");
                }

                foreach (var p in t.GetProperties(Declared))
                {
                    var acc = (p.GetGetMethod() != null ? "get; " : "") + (p.GetSetMethod() != null ? "set; " : "");
                    lines.Add(Name(t) + " property " + Name(p.PropertyType) + " " + p.Name + " { " + acc + "}");
                }

                foreach (var f in t.GetFields(Declared))
                {
                    lines.Add(Name(t) + " field " + Name(f.FieldType) + " " + f.Name);
                }
            }

            return lines;
        }

        private static string Params(MethodBase m)
        {
            return string.Join(", ", m.GetParameters().Select(p =>
                Name(p.ParameterType) + " " + p.Name + (p.HasDefaultValue ? " = " + (p.DefaultValue ?? "null") : "")));
        }

        private static string Name(Type t)
        {
            if (t.IsGenericParameter)
            {
                return t.Name;
            }

            if (t.IsByRef)
            {
                return "ref " + Name(t.GetElementType()!);
            }

            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                var baseName = (def.Namespace == null ? "" : def.Namespace + ".") + def.Name.Substring(0, def.Name.IndexOf('`'));
                if (def == typeof(Nullable<>))
                {
                    return Name(t.GetGenericArguments()[0]) + "?";
                }

                return baseName + "<" + string.Join(", ", t.GetGenericArguments().Select(Name)) + ">";
            }

            return t.FullName ?? t.Name;
        }
    }
}
