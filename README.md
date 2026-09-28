# CachingServiceWithAOPSupport

[![NuGet](https://img.shields.io/nuget/v/CachingServiceWithAOPSupport)](https://www.nuget.org/packages/CachingServiceWithAOPSupport)
[![ci](https://github.com/m4bwav/CachingServiceWithAOPSupport/actions/workflows/ci.yml/badge.svg)](https://github.com/m4bwav/CachingServiceWithAOPSupport/actions/workflows/ci.yml)
[![Downloads](https://img.shields.io/nuget/dt/CachingServiceWithAOPSupport)](https://www.nuget.org/packages/CachingServiceWithAOPSupport)

Method result caching for [Autofac](https://autofac.org/): mark a component's methods with `[Cache]`, register the component with `EnableCacheInterception()`, and calls through its interface are answered from `System.Runtime.Caching.MemoryCache` until the result expires. The interception is Castle DynamicProxy, through Autofac.Extras.DynamicProxy.

Runs on .NET Framework 4.6.2 and later, .NET 8 and .NET 10 (the package targets netstandard2.0 and net10.0). Works with Autofac 6.5 to 9.

## Install

```
dotnet add package CachingServiceWithAOPSupport
```

## Use

```csharp
using Autofac;
using CachingServiceWithAOP;
using CachingServiceWithAOP.Extensions;

public interface IPriceService
{
    decimal GetPrice(string sku);
}

public class PriceService : IPriceService
{
    // The attribute goes on the class's method; on the interface alone it does nothing.
    [Cache(0, 10, 0)] // ten minutes
    public decimal GetPrice(string sku) => LoadFromDatabase(sku);

    private static decimal LoadFromDatabase(string sku) => 9.99m;
}

var builder = new ContainerBuilder();
builder.RegisterCachingModule();                  // MemoryCacheService and the interceptor
builder.RegisterType<PriceService>()
    .As<IPriceService>()
    .EnableCacheInterception();                   // interface interception

using var container = builder.Build();
var prices = container.Resolve<IPriceService>();
prices.GetPrice("A-1");                           // runs the method
prices.GetPrice("A-1");                           // answered from the cache
```

`MemoryCacheService` also works on its own:

```csharp
var cache = new MemoryCacheService(TimeSpan.FromMinutes(1));
var report = cache.Get("report", () => BuildReport()); // BuildReport runs once per minute at most
```

## API

- `[Cache]`: the service's lifetime (five minutes for `MemoryCacheService`). `[Cache(ticks)]` in 100 ns ticks, `[Cache(hours, minutes, seconds)]`, `[Cache(days, hours, minutes, seconds)]`. A lifetime of zero or less keeps nothing; one too large for a date never expires.
- `builder.RegisterCachingModule()` or `builder.RegisterModule<AutofacCachingModule>()`: registers `MemoryCacheService` as `ICacheService` and `AOPCachingInterceptor`. Register your own `ICacheService` after it to replace the store.
- `registration.EnableCacheInterception()`: interface interceptors with `AOPCachingInterceptor`. Resolve the component through an interface.
- `MemoryCacheService`: constructors `()`, `(ObjectCache)`, `(long ticks)`, `(int hours, int minutes, int seconds)` and `(TimeSpan)`; `Get<T>(key)`, `Get<T>(key, func)`, `Set<T>(key, value)` and `GetByInvocation(invocation, duration)`.
- `DefaultCacheKeyService.GenerateUniqueKeyForCall(invocation)`, `ReflectionExtensions.HasCacheAttribute(MethodInfo)`, `AutofacExtensions.HasCacheAttribute(IComponentRegistration)`.

## Behaviour

- **Keys.** A cached call's key is the target object's `ToString()`, the method name, its generic arguments, its parameter types, and every argument written as JSON (public fields, then public properties, as .NET Framework's JavaScriptSerializer wrote them; 2.x makes byte-for-byte the keys 1.0.1 made). So:
  - two instances of a class share entries unless the class overrides `ToString()`;
  - arguments should be values or small data objects: building a key reads every public getter of every argument (a `Task` argument's `Result` would block, a lazy property would load);
  - an argument that cannot be written (a reference cycle, nesting deeper than 100, a dictionary with non-string keys, a key over 2 097 152 characters) makes the call run without caching.
  - arguments of a .NET struct type other than `TimeSpan` whose public properties differ between .NET Framework and .NET give different keys on the two runtimes; keys only need to agree within one process.
- **Store.** Every `MemoryCacheService` made without an `ObjectCache` shares `MemoryCache.Default`. Entries expire at an absolute time.
- **Results.** Null results are cached. Void methods and methods returning `ValueTask` always run and are never cached. A returned `Task` stays cached while it succeeds and is removed when it faults or is cancelled. An exception is never cached.
- **Concurrency.** Concurrent calls with one key run the method once; the others wait for its result.
- **Get with a function.** `Get(key, func)` runs `func` when the key is missing or holds a value of another type, and caches its result (null included). `Set(key, null)` throws `ArgumentNullException`.

## Migrating from 1.x

2.0.0 keeps every public name and parameter name of 1.0.1 and gives the same answers (the repository's golden test replays 158 recorded 1.0.1 calls on every build). What changes:

- It needs Autofac 6.5 or later, Autofac.Extras.DynamicProxy 7.1 or later (the successor of Autofac.Extras.DynamicProxy2) and Castle.Core 5.1 or later. The API names Autofac and Castle types, so callers recompile against those versions.
- It runs on .NET Core and .NET 5+; 1.0.1 failed there on the first proxy.
- Fixes: `[Cache]` methods that return null or return nothing (void) work instead of throwing ArgumentNullException after running; `MemoryCacheService(ObjectCache)` keeps values for five minutes instead of none; `Get(key, func)` replaces a value of another type instead of returning the default; lifetimes too large for a date never expire instead of throwing; concurrent callers run a method once; faulted tasks are not kept. The [CHANGELOG](CHANGELOG.md) lists each.

## What this package is not

Not a distributed cache, not a replacement for `IMemoryCache` or HybridCache in new code that does not use Autofac, and not a way to cache class members that are not called through an interface. It makes no network or file access. It stores whatever your methods return, in process memory, keyed by argument values; do not cache results that must not outlive a user's session in a shared process.

## Licence

MIT. Copyright (c) 2015-2026 Mark Rogers.
