---
title: AOP caching ecosystem research for CachingServiceWithAOPSupport
kind: note
date: 2026-09-27
verified: 2026-09-27
stale_after: 2026-12-27
tags: [research, nuget, autofac, castle, caching, dependents]
summary: "current versions and targets of Autofac, Autofac.Extras.DynamicProxy, Castle.Core, System.Runtime.Caching and friends; API compatibility from Autofac 3 to 9; maintained attribute-caching alternatives; dependents (none found); MemoryCache on .NET 10; JavaScriptSerializer vs System.Text.Json key differences"
---

# CachingServiceWithAOPSupport: modernize or deprecate (research)

## Summary

Every current building block (Autofac 9.3.4, Autofac.Extras.DynamicProxy 8.1.0, Castle.Core 5.2.1, System.Runtime.Caching 10.0.12, System.Text.Json 10.0.12) supports netstandard2.0 and has no advisories; the package's Autofac and Castle calls still exist apart from the DynamicProxy2 to DynamicProxy namespace. nuget.org lists no dependents. Maintained alternatives exist (Metalama caching, HybridCache or FusionCache without attributes).

Checked 2026-09-27. Unless a line says otherwise, registry facts come from the NuGet registration API
`https://api.nuget.org/v3/registration5-gz-semver2/<id>/index.json` (fields `listed`, `published`,
`vulnerabilities`, `deprecation`, `dependencyGroups`, `licenseExpression`), read 2026-09-27 with a script.
"TFMs" are the target frameworks of the latest stable listed version's dependency groups.

## 1. Dependency landscape

| Package | Latest stable (listed) | Published | TFMs | License | Vulnerable versions | Deprecation |
|---|---|---|---|---|---|---|
| Autofac | 9.3.4 | 2026-09-18 | netstandard2.0, netstandard2.1, net8.0, net10.0 | MIT | none flagged | none |
| Autofac.Extras.DynamicProxy | 8.1.0 | 2026-08-14 | netstandard2.0, netstandard2.1, net8.0, net10.0 | MIT | none | none |
| Autofac.Extras.DynamicProxy2 | 3.0.7 | 2015-07-07 | net40 only (lib/net40) | MIT (licenseUrl) | none | **not deprecated on nuget.org** (no `deprecation` in registration, no banner on the gallery page) |
| Castle.Core | 5.2.1 | 2025-03-09 | net462, net6.0, netstandard2.0, netstandard2.1 | Apache-2.0 | none | none |
| System.Runtime.Caching | 10.0.12 (11.0.0-rc.1 is prerelease, 2026-09-08) | 2026-09-08 | netstandard2.0, net8.0, net9.0, net10.0; `lib/net462/_._` (uses the in-box .NET Framework assembly) | MIT | none | only 5.0.0 is deprecated ("Legacy", .NET package deprecation effort) |
| Microsoft.Extensions.Caching.Memory | 10.0.12 | 2026-09-08 | net462, netstandard2.0, net8.0, net9.0, net10.0 | MIT | GHSA-qj66-m88j-hmgj on 29 versions: stable 6.0.0, 6.0.1, 8.0.0 plus 6.x-9.0 previews | many old versions deprecated (Legacy) |
| Microsoft.Extensions.Caching.Hybrid | 10.10.0 | 2026-09-09 | net462, netstandard2.0, netstandard2.1, net8.0, net9.0, net10.0 | MIT | none | none |
| System.Text.Json | 10.0.12 | 2026-09-08 | net462, netstandard2.0, net8.0, net9.0, net10.0 | MIT | GHSA-8g4q-xg66-9fp4 (6.0.0-6.0.9, 8.0.0-8.0.4 and previews), GHSA-hh2w-p6rv-4g7w (7.0.0-7.0.4, 8.0.0-8.0.3) | many old versions deprecated (Legacy) |
| Microsoft.NETFramework.ReferenceAssemblies | 1.0.3 | 2022-08-19 | meta-package, net20-net48x | https://github.com/Microsoft/dotnet/blob/master/LICENSE | none | none |

- Versions published on or after 2026-09-24 (younger than three days): **none** of the nine packages above.
  Closest: Autofac 9.3.4 (2026-09-18). Source: registration API, 2026-09-27.
- Autofac.Extras.DynamicProxy2 page: 1.1M total downloads, 13 dependent packages, no deprecation notice.
  Source: https://www.nuget.org/packages/Autofac.Extras.DynamicProxy2 (2026-09-27). The rename is documented in
  the DynamicProxy 4.0.0 release notes (2017-01-03): "Package renamed to Autofac.Extras.DynamicProxy."
  https://github.com/autofac/Autofac.Extras.DynamicProxy/releases/tag/v4.0.0 (checked 2026-09-27).
- DynamicProxy2 3.0.7 depends on Autofac [3.3.1, 4.0.0) and Castle.Core >= 3.2.2 (registration, 2026-09-27).
- CachingServiceWithAOPSupport itself: 1.0.0 and 1.0.1, both 2015-04-18, depends on Autofac.Extras.DynamicProxy2 >= 3.0.5,
  gallery owner `rogersm0`, 4.0K total downloads (registration + gallery page, 2026-09-27).

## 2. Autofac and Castle.Core API compatibility

- **Autofac 9 still targets netstandard2.0.** Autofac 9.0.0 (2025-11-18): "New current set of target frameworks:
  net10.0;net8.0;netstandard2.1;netstandard2.0"; dropped net6.0/net7.0. https://github.com/autofac/Autofac/releases/tag/v9.0.0 (checked 2026-09-27)
- **Autofac.Extras.DynamicProxy 8.1.0 requires Autofac >= 9.3.2 and Castle.Core >= 5.2.1** (all four TFMs).
  Registration API, 2026-09-27. 8.0.0 (2026-06-24) raised the minimum to Autofac 9.x and Castle.Core 5.1.1 -> 5.2.1, dropped net6.0,
  and added an optional `Func<Type,bool> shouldIntercept` predicate to `EnableInterfaceInterceptors`/`EnableClassInterceptors`
  (additive, binary-compatible). https://github.com/autofac/Autofac.Extras.DynamicProxy/releases/tag/v8.0.0 (checked 2026-09-27)
- Namespace changed: `Autofac.Extras.DynamicProxy2` -> `Autofac.Extras.DynamicProxy` (package rename in 4.0.0, 2017).
  Current source: `namespace Autofac.Extras.DynamicProxy;` in
  https://github.com/autofac/Autofac.Extras.DynamicProxy/blob/develop/src/Autofac.Extras.DynamicProxy/RegistrationExtensions.cs (checked 2026-09-27).
- APIs used by the package, as of current Autofac/DynamicProxy source (checked 2026-09-27):
  - `Autofac.Module` still `public abstract class Module : IModule` with `protected virtual void Load(ContainerBuilder)`,
    `AttachToComponentRegistration`, `AttachToRegistrationSource`. https://github.com/autofac/Autofac/blob/develop/src/Autofac/Module.cs
  - `RegisterModule<TModule>(this ContainerBuilder)` still exists, now returns `IModuleRegistrar` (was void-ish/fluent in 3.x;
    source-compatible for callers that ignore the result). https://github.com/autofac/Autofac/blob/develop/src/Autofac/ModuleRegistrationExtensions.cs
  - `IComponentRegistration.Activator` (type `IInstanceActivator`) and `IInstanceActivator.LimitType` still exist.
    https://github.com/autofac/Autofac/blob/develop/src/Autofac/Core/IComponentRegistration.cs , .../Core/IInstanceActivator.cs
  - `EnableInterfaceInterceptors()` (type parameters TLimit, TActivatorData, TSingleRegistrationStyle) exists with no generic constraints, and
    `InterceptedBy(params Type[])`, `InterceptedBy(params Service[])`, `InterceptedBy(params string[])` exist. So the
    `EnableCacheInterception` extension should compile after the namespace change.
  - `ContainerBuilder` is `sealed` since Autofac 6 (only matters if anyone subclassed it).
- Breaking changes between 3.x and 9.x that matter to this package: none of the members above were removed. Relevant
  breaking changes elsewhere: Autofac 6 moved activation events to a resolve pipeline, `IInstanceActivator.ActivateInstance`
  replaced by `ConfigurePipeline` (affects only custom activators), `IRegistrationSource` signature change, sealed `ContainerBuilder`.
  https://autofac.readthedocs.io/en/latest/whats-new/upgradingfrom5to6.html (checked 2026-09-27). Autofac 7: `required`
  properties injected by default, `ILifetimeScope.BeginLoadContextLifetimeScope` added.
  https://github.com/autofac/Autofac/releases/tag/v7.0.0 . Autofac 8: `ResolveRequest` became a readonly struct.
  https://github.com/autofac/Autofac/releases/tag/v8.0.0 . Autofac 4 and 5 notes were not read in full (uncertain: 5.0 made
  containers immutable; not relevant to this package's surface).
- **Castle.Core 5.x `IInvocation`** still has `Arguments`, `GenericArguments`, `InvocationTarget`, `Method`,
  `MethodInvocationTarget`, `Proxy`, `ReturnValue {get;set;}`, `TargetType`, `GetArgumentValue(int)`, `GetConcreteMethod()`,
  `GetConcreteMethodInvocationTarget()`, `Proceed()`, `CaptureProceedInfo()`, `SetArgumentValue`. Since 5.2.x the members carry
  nullable annotations (`MethodInvocationTarget`, `InvocationTarget`, `GenericArguments`, `ReturnValue` are nullable).
  https://github.com/castleproject/Core/blob/master/src/Castle.Core/DynamicProxy/IInvocation.cs (checked 2026-09-27)
- `IInvocation.CaptureProceedInfo()` was added in **4.4.0 (2019-04-05)** "to enable better implementations of asynchronous
  interceptors". Castle.Core 5.0.0 (2022-05-11) removed net<4.6.2 and netstandard1.x, CAS and Remoting support; no async-related
  change to `IInvocation` in 5.x. Castle 6.0.0 is unreleased (changelog: net8/net9/netstandard2.0/net462; `ref struct` support;
  fix for `MethodInvocationTarget` throwing on default interface methods).
  https://github.com/castleproject/Core/blob/master/CHANGELOG.md (checked 2026-09-27)
- Consequence for async: the old interceptor caches `invocation.ReturnValue` after `Proceed()`. For `Task<T>`-returning methods that
  caches the Task object (works once completed, but also caches faulted tasks). A modern version needs explicit Task handling;
  `Castle.Core.AsyncInterceptor` 2.1.0 (2022-03-17, 51.7M downloads, not updated since) is the common helper. Registration API, 2026-09-27.

## 3. Current alternatives for attribute-driven method result caching

Download counts from the NuGet search API (azuresearch-usnc.nuget.org), dates from the registration API, both 2026-09-27.

| Option | Latest | Date | Downloads | TFMs / notes |
|---|---|---|---|---|
| Metalama.Patterns.Caching.Aspects (`[Cache]` aspect) | 2026.1.28 | 2026-09-16 | 82K (Patterns.Caching 117K) | net472, netstandard2.0, net8.0; MIT; compile-time (Roslyn) weaving. Framework core open-sourced under MIT 2025-04-04; Redis/Azure distributed caching adapters are proprietary. https://postsharp.net/blog/metalama-open-source , https://doc.metalama.net/patterns/caching |
| AspectCore.Core / .Extensions.DependencyInjection | 3.0.0 | 2026-07-26 | 10.1M / 8.2M | net6.0-net10.0 only (no netstandard2.0 in 3.0.0); runtime interception, general AOP |
| EasyCaching.Interceptor.Castle / .AspectCore | 1.9.2 (prerelease 1.9.4-alpha 2025-03-17) | 2023-10-31 | 225K / 426K | netstandard2.0, net6.0; `[EasyCachingAble]` attributes over Castle or AspectCore. Stale: no stable release in almost 3 years |
| Rougamo.Fody (method interception attribute, build caching yourself) | 5.0.2 | 2025-10-13 | 802K | netstandard2.0+; MIT |
| AspectInjector (compile-time aspects, generic) | 2.9.0 | 2025-09-13 | 20.4M | netstandard2.0; Apache-2.0 |
| ZeroAlloc.Cache (source generator, `[Cache]` on an interface, IMemoryCache or HybridCache) | 1.1.49 | **2026-09-27** (under three days old) | 7.6K | net8.0-net10.0; repo created 2026-04-20, 0 stars. https://github.com/ZeroAlloc-Net/ZeroAlloc.Cache . Too new to rely on |
| ZiggyCreatures.FusionCache (no attributes; GetOrSet API) | 2.9.0 | 2026-09-22 | 46.4M | netstandard2.0, net8-10 |
| Microsoft.Extensions.Caching.Hybrid (no attributes; `GetOrCreateAsync`, stampede protection, tags) | 10.10.0 | 2026-09-09 | 34.1M | net462, netstandard2.0/2.1, net8-10. https://learn.microsoft.com/en-us/dotnet/core/extensions/caching (ms.date 2026-07-28) |

- Dead options: MethodCache.Fody 1.5.1 (2017) is deprecated ("Legacy"), pointing to SpatialFocus.MethodCache.Fody, whose last release
  is 1.1.0 (2020-11-30, repo last pushed 2020-12-10). CacheSourceGenerator 0.5.0-preview-01 (2024-04-19), Zoxive.MemoizeSourceGenerator 0.0.10 (2023-02-24): unmaintained.
- `System.Reflection.DispatchProxy`: in-box on .NET Core/.NET 5+; the NuGet package 4.8.2 (2025-03-19, 678M downloads, MIT) targets
  net462, netcoreapp2.0, netstandard2.0, netstandard2.1. Interface-only proxies (`DispatchProxy.Create(TInterface, TProxy)()`);
  would remove the Castle and Autofac dependencies for an interface-caching decorator. Registration API, 2026-09-27.
- No first-party Microsoft attribute-based method caching exists; HybridCache and IMemoryCache are imperative APIs (learn page above).

## 4. Dependents

- nuget.org: "This package is not used by any NuGet packages." and "This package is not used by any popular GitHub repositories."
  https://www.nuget.org/packages/CachingServiceWithAOPSupport (checked 2026-09-27)
- GitHub code search (`gh search code`, 2026-09-27) for `CachingServiceWithAOPSupport`: only m4bwav/CachingServiceWithAOPSupport,
  m4bwav/package-modernization (inventory), and NuGet catalog mirrors (aboutcode-org, joelverhagen). Searches for `AutofacCachingModule`,
  `AOPCachingInterceptor`, `RegisterCachingModule` found only same-named but unrelated classes (OSTEPHAN/Thrarin uses
  Microsoft.Extensions.Caching.Memory; ZeeLyn/AopCaching has its own `AopCachingInterceptor`). No consumers found.
  grep.app was blocked by a Vercel bot check; Sourcegraph not tried (uncertain: private repos and non-GitHub code are invisible).
- Fork WingStudio/CachingServiceWithAOPSupport (created 2016-03-10): compare API reports `"status":"identical"`, ahead 0, behind 0.
  No changes. Upstream m4bwav repo last pushed 2015-04-18, 2 stars, 1 fork. (GitHub API, 2026-09-27)

## 5. System.Runtime.Caching MemoryCache on .NET 10 vs .NET Framework

- The .NET 10 source is the same design as the Framework one (ported code). Checked in dotnet/runtime release/10.0,
  `src/libraries/System.Runtime.Caching/src/System/Runtime/Caching/` (2026-09-27):
  - `MemoryCache.Default` exists (static, lazily created).
  - `Set(key, null, ...)`: `MemoryCacheEntry` constructor calls `ArgumentNullException.ThrowIfNull(value)`, so a null value still
    throws `ArgumentNullException`, as on .NET Framework. Null key also throws.
  - Absolute expiration at or before now: `Set` does not reject it; the entry is stored, and `MemoryCacheStore.Get` treats
    `UtcAbsExp <= DateTime.UtcNow` as expired, removes it and returns null. Same observable behaviour as .NET Framework
    (item effectively never retrievable). `Add`/`AddOrGetExisting` likewise replace an expired existing entry.
  - Invalid policy combinations (absolute plus sliding, sliding > 1 year, both callbacks) throw as before.
  - Performance counters are skipped only on Browser/WASI (`_countersSupported`).
- Documented differences: none found in the API reference (applies to netframework-4.0 through 4.8.1, netstandard-2.0, net-10.0, net-11.0;
  https://learn.microsoft.com/en-us/dotnet/api/system.runtime.caching.memorycache , updated 2026-07-01). The caching overview says it
  "doesn't include the System.Runtime.Caching NuGet package" and steers to Microsoft.Extensions.Caching.Memory / HybridCache for new work
  (https://learn.microsoft.com/en-us/dotnet/core/extensions/caching , ms.date 2026-07-28).
- Uncertain: memory-limit configuration (`cacheMemoryLimitMegabytes` in app.config) goes through System.Configuration.ConfigurationManager on
  .NET 10 and memory-pressure monitoring on Linux/macOS was not verified; irrelevant unless callers configure limits.

## 6. JavaScriptSerializer -> System.Text.Json for cache keys

Sources: JavaScriptSerializer type mapping table,
https://learn.microsoft.com/en-us/dotnet/api/system.web.script.serialization.javascriptserializer (updated 2026-05-27; the page itself says
to use System.Text.Json on .NET Framework 4.7.2+), and
https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft (updated 2026-03-30). Checked 2026-09-27.

- **DateTime**: JSS writes `"\/Date(ms since 1970)\/"` (Kind and sub-millisecond ticks lost); STJ writes ISO 8601-1:2019 (`2026-09-27T12:00:00.1234567Z`,
  keeps Kind/offset and full precision). Keys will differ, and two DateTimes JSS collapsed can now differ.
- **Guid**: both write a string; STJ uses the "D" format. Same key text in practice.
- **Enums**: both write the integer by default (STJ needs `JsonStringEnumConverter` for names).
- **Dictionaries**: JSS serialises IDictionary as a JSON object (string keys expected); STJ supports string and primitive keys (int, Guid, enum, etc.) and throws `NotSupportedException` for other key types.
- **Cycles**: JSS throws `InvalidOperationException` ("circular reference", RecursionLimit default 100); STJ throws `JsonException` at depth 64 unless
  `ReferenceHandler.IgnoreCycles` (writes null) or `Preserve` is set.
- **Members**: JSS writes public readable properties and **public fields**; STJ writes public properties only. Fields need `IncludeFields`/`[JsonInclude]`,
  non-public accessors need `[JsonInclude]`. So argument types with public fields would give colliding keys under STJ defaults. Set `IncludeFields = true`.
- **Runtime type**: STJ serialises the declared type unless it is `object`. Serialising an `object[]` of arguments uses runtime types, which matches JSS.
- **Escaping**: STJ escapes non-ASCII and HTML-sensitive characters by default. Only the key text changes; uniqueness is unaffected.
- Cache keys are never deserialised, so the asymmetries only matter for key uniqueness and stability. Any cached entries from an old process are lost anyway (in-memory).
