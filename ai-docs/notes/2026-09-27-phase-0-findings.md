---
title: Phase 0 findings for CachingServiceWithAOPSupport 1.0.1
kind: note
date: 2026-09-27
verified: 2026-09-27
stale_after: never
tags: [survey, phase-0, nuget, aop, autofac, castle]
summary: "what the 2015 package is, how it does AOP, what the published 1.0.1 contains, the bugs found by reading the source, and the baseline build result; read before re-surveying"
---

# Phase 0 findings: CachingServiceWithAOPSupport 1.0.1

## Summary

CachingServiceWithAOPSupport 1.0.1 caches method results through Castle DynamicProxy interface interceptors wired by Autofac 3, over System.Runtime.Caching. It has no dependents, no GitHub activity since 2015, a broken 1.0.0 package, and does nothing useful on .NET 10 (Castle.Core 3.2.2 cannot load). The golden capture confirmed a set of bugs listed below; the baseline build fails on the .NET 10 SDK.

Read this before surveying the package again. Raw survey output: [2026-09-27-survey.txt](2026-09-27-survey.txt). Registry and ecosystem research (versions, alternatives, dependents): [2026-09-27-aop-ecosystem-research.md](2026-09-27-aop-ecosystem-research.md).

## What it is

- nuget.org id `CachingServiceWithAOPSupport`, versions 1.0.0 and 1.0.1, both published and listed on 2015-04-18. 4 008 total downloads (1 709 and 2 299). Owner `rogersm0`. No deprecation, no vulnerabilities.
- The published 1.0.1 nupkg holds one file of code: lib/net45/CachingServiceWithAOP.dll (12 288 bytes; assembly name `CachingServiceWithAOP`, not the package id). Repository signature added by nuget.org on 2018-12-14.
- **1.0.0 is broken as a package**: its files sit under `bin/Debug/` and `obj/Debug/` instead of `lib/`, so installing it adds no assembly reference at all; it also bundles Autofac.dll, Autofac.Extras.DynamicProxy2.dll and Castle.Core.dll and declares no dependency. Its CachingServiceWithAOP.dll is byte-identical to 1.0.1's (SHA-256 2f956a8bc3d97a8a...). The two nupkgs committed in the repository are byte-for-byte what nuget.org serves (DLL hashes compared 2026-09-27).
- A consumer restoring 1.0.1 gets Autofac.Extras.DynamicProxy2 3.0.5, which pins Autofac `[3.3.1, 4.0.0)` and Castle.Core `[3.2.2, )`: Autofac 3.3.1 and Castle.Core 3.2.2, the versions of the old `packages.config`.
- Declared dependency: `Autofac.Extras.DynamicProxy2 [3.0.5, )` only (no target-framework group). Autofac and Castle.Core come in through it. The DLL also references the Framework assemblies `System.Runtime.Caching` and `System.Web.Extensions`.
- Repository m4bwav/CachingServiceWithAOPSupport: 5 commits, all 2015-04-18. The repository also commits both nupkgs (`CachingServiceWithAOP/CachingServiceWithAOPSupport.1.0.nupkg` and `.1.0.1.nupkg`).
- GitHub side: no issues, no pull requests, no workflows, no webhooks, no secrets, no releases or tags, one branch (`master`), 0 Dependabot alerts, every security feature off, default workflow permissions `write`. One fork, WingStudio/CachingServiceWithAOPSupport, not pushed since the fork date (2015-04-18).
- README: two lines, no images or badges. Description text has typos ("an an", nuspec "all-prupose").

## The AOP mechanism

Castle DynamicProxy **interface** interceptors, wired through Autofac:

- `AutofacCachingModule : Autofac.Module` registers `MemoryCacheService` as `ICacheService` and `AOPCachingInterceptor` as itself.
- `AutofacExtensions.EnableCacheInterception()` calls `EnableInterfaceInterceptors().InterceptedBy(typeof(AOPCachingInterceptor))` (Autofac.Extras.DynamicProxy2).
- `AOPCachingInterceptor : Castle.DynamicProxy.IInterceptor` checks `[Cache]` on `invocation.MethodInvocationTarget` (the implementation method, so the attribute goes on the class, not the interface) and calls `ICacheService.GetByInvocation(invocation, duration)`.
- Not PostSharp, not Unity interception, not RealProxy/ContextBoundObject.

## Public API of 1.0.1 (from the source; the reflection listing is `tests/Golden/PublicApi-1.0.1.txt`)

Namespace `CachingServiceWithAOP`: `CacheAttribute` (ctors `()`, `(long ticks)`, `(int hours, int minutes, int seconds)`, `(int days, int hours, int minutes, int seconds)`; `TimeSpan? CacheTimeout { get; set; }`; no `AttributeUsage`, so it is allowed on any target, not inherited-checked), `AOPCachingInterceptor` (ctor `(ICacheService)`, `Intercept(IInvocation)`), `AutofacCachingModule` (`static IContainer BuildContainer()`, protected `Load`).
Namespace `CachingServiceWithAOP.CachingServices`: `ICacheService` (`Get<T>(string)`, `Get<T>(string, Func<T>)`, `Set<T>(string, T)`, `GetByInvocation(IInvocation, TimeSpan? duration = null)`), `IKeyService` (`GenerateUniqueKeyForCall(IInvocation)`), `DefaultCacheKeyService`, `MemoryCacheService` (ctors `()`, `(ObjectCache backingCache)`, `(long absoluteTicks)`, `(int absoluteHours, int absoluteMinutes, int absoluteSeconds)`, `(TimeSpan expiration)`).
Namespace `CachingServiceWithAOP.Extensions`: `AutofacExtensions` (`RegisterCachingModule(this ContainerBuilder)`, `HasCacheAttribute(this IComponentRegistration)`, `EnableCacheInterception(this IRegistrationBuilder)` with the type parameters TLimit, TActivatorData and TRegistrionStyle; note the misspelt type parameter `TRegistrionStyle`), `ReflectionExtensions` (`HasCacheAttribute(this MethodInfo)`).
Public types from Castle (`IInvocation`) and Autofac appear in the API, so their majors are part of this package's contract.

## What the golden capture confirmed (158 cases, 2026-09-27)

- **On .NET 10 the published 1.0.1 cannot intercept anything.** Every proxy (Autofac or plain Castle) fails with `FileNotFoundException: Could not load file or assembly 'System.Security.Permissions'` from Castle.Core 3.2.2. `CacheAttribute`, `MemoryCacheService` (with the System.Runtime.Caching package), `HasCacheAttribute` and the Autofac module work, and answer as on net48 apart from the `(Parameter 'x')` suffix of ArgumentException messages. 2 net10 runs, byte-identical.
- On net48 (the contract), confirmed: items 1, 2 (a `[Cache]` method returning null, and a `[Cache]` void method, run on every call and then throw ArgumentNullException at the caller), 3, 7, 8 below; `Get(key, func)` returns `default` **without calling func** when the key holds a value of another type; `[Cache]` on a `Task<int>` method returns the same Task object on the second call; a lifetime of 0 or below means "not cached"; an overflowing lifetime (`long.MaxValue` ticks, `TimeSpan.MaxValue`) throws ArgumentOutOfRangeException at `Set` time, after the intercepted method ran; the attribute on the interface method alone does nothing; a class proxy (Castle `CreateClassProxy`) of a class with a `[Cache]` virtual method caches, and so does an override that inherits the attribute.
- Keys (JavaScriptSerializer): `char` NUL and `null` give the same key (`values:null|`); `-0.0` and `0.0` the same; dictionaries key by insertion order; public fields are included; a cyclic argument throws InvalidOperationException, so the call fails; `DateTime` UTC is `\/Date(ms)\/`; a subclass argument serialises its own properties; `out` parameters key by their value on entry. Keys of `("a|", 1)`, `("a", 1)` and `(null, 1)` differ (strings are JSON-quoted), so the `|` separator causes no collisions.
- 2 net48 runs byte-identical; the in-repo rerun byte-identical to the scratch runs; the header's DLL SHA-256 equals the nupkg's.

## Suspected bugs and oddities (read from the source before the capture)

1. `MemoryCacheService(ObjectCache)` never sets `_cacheTimeout`, so every `Set` expires at `UtcNow + 0`: a cache built on a caller's `ObjectCache` never returns anything.
2. `MemoryCache.Set` refuses a null value (ArgumentNullException), so `Set(key, null)`, `Get(key, () => null)` and a `[Cache]` method returning null throw; in the interceptor the method has already run when it throws.
3. A cached `null` is indistinguishable from a miss, and a cached default value type (0) is returned; `Get<T>` returns `default(T)` on a type mismatch rather than failing.
4. `CacheLock` is a static, unsynchronised `Dictionary<string, object>` written from many threads (corruption under concurrency) and never trimmed (grows with every key ever used).
5. The `Get<T>(key, func)` lock path is per key but the interceptor path `GetByInvocation` takes no lock (no stampede protection for intercepted methods).
6. Cache key = `invocation.InvocationTarget.ToString()` + method name + generic arguments + parameter types + `JavaScriptSerializer` JSON of each argument. Keys therefore depend on the target's `ToString()` (type name by default, so every instance shares entries; a `ToString` override splits or merges them), and on JavaScriptSerializer (Framework only; throws on cycles; ignores fields; serialises DateTime as `\/Date(...)\/`).
7. `MemoryCache.Default` is process-wide: every `MemoryCacheService` shares it, so two services with different timeouts share keys.
8. `CacheAttribute(long ticks)` and `MemoryCacheService(long)` take ticks (100 ns units), easy to mistake for milliseconds.
9. `HasCacheAttribute(IComponentRegistration)` is public but unused.
10. `[Cache]` on a method returning `Task` would cache the Task object (first call's task, including a faulted one): the 2015 code has no async handling.

## Old tests (MSTest, `CachingServiceWithAOP.Tests`)

13 tests over the cache service, the attribute and the Autofac wiring. Test bug: `GenerateUniqueKey()` returns `new Guid().ToString()`, the all-zero Guid, so every "unique" key is the same string and tests depend on order. Timing tests sleep 1 ms or 1 s.

## Baseline (2026-09-27, this machine: SDK 9.0.317 and 10.0.401, no Visual Studio, no .NET Framework targeting packs)

- `dotnet restore CachingServiceWithAOP.sln`: exit 0 (restores nothing; `packages.config` is ignored by `dotnet restore`).
- `dotnet build CachingServiceWithAOP.sln`: exit 1, MSB3644 in both projects (reference assemblies for .NETFramework v4.5 not found). The old projects are non-SDK `ToolsVersion 4.0` csproj files with `HintPath`s into a `packages/` folder that is not committed, and MSTest from Visual Studio's `QualityTools`.
- So the old tests are run instead against the published package in a scratch net48 SDK project (see the log).
