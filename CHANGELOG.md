# Changelog

All notable changes to this package. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/).

## [2.0.0] - Unreleased

2.0.0 keeps every public type, member and parameter name of 1.0.1 and answers every call as 1.0.1 did on .NET Framework, with the exceptions listed under Fixed: `tests/Golden` holds 158 calls recorded from the published 1.0.1, and the golden test replays them against every build on .NET Framework 4.8 and .NET 10. Cache keys are byte-for-byte 1.0.1's. The major version is for the dependencies: the API names Autofac and Castle.Core types, whose majors move.

### Changed

- Targets netstandard2.0 and net10.0 (1.0.1: net45). Runs on .NET Framework 4.6.2+, .NET 8 and .NET 10; 1.0.1 could not create a proxy on any .NET Core runtime (Castle.Core 3.2.2 needs System.Security.Permissions).
- Depends on Autofac.Extras.DynamicProxy 7.1.0 or later (replacing Autofac.Extras.DynamicProxy2 3.0.5), Autofac 6.5.0 or later, Castle.Core 5.1.1 or later and System.Runtime.Caching 10.0.12. Works with Autofac 6.5 to 9.
- Cache keys are written by a built-in JavaScriptSerializer-compatible writer (System.Web.Extensions exists only on .NET Framework).
- Package icon, README, Source Link, symbols package, deterministic build.

### Fixed

- A `[Cache]` method that returns null ran on every call and then threw ArgumentNullException at the caller; the null is now returned and cached.
- A `[Cache]` void method threw ArgumentNullException after running; void methods now run and are not cached.
- `MemoryCacheService.Get(key, func)` threw ArgumentNullException when `func` returned null, and ran it again next time; the null is now returned and cached.
- `MemoryCacheService.Get(key, func)` returned the default without running `func` when the key held a value of another type; it now runs `func` and replaces the value.
- `new MemoryCacheService(ObjectCache)` gave every entry a lifetime of zero, so it kept nothing; it now uses the five-minute default.
- A lifetime too large for a date (`long.MaxValue` ticks, `TimeSpan.MaxValue`) threw ArgumentOutOfRangeException after the intercepted method ran; it now never expires. One too small for a date keeps nothing, like any negative lifetime.
- The per-key lock table was an unsynchronised static Dictionary that grew with every key, and intercepted calls took no lock: concurrent callers all ran the method. Concurrent calls with one key now run it once, and the table only holds keys being computed.
- A `[Cache]` method returning a Task kept a faulted or cancelled task forever; such tasks are now removed. Methods returning `ValueTask` are not cached (a ValueTask may be awaited only once).
- An argument that cannot be written into a key (a reference cycle, for example) made the call throw InvalidOperationException before the method ran; the call now runs uncached. `DefaultCacheKeyService.GenerateUniqueKeyForCall` itself still throws, as in 1.0.1.

### Removed

- The .NET Framework 4.5 project files, the nuspec, and the nupkgs that were committed in the repository.

## [1.0.1] - 2015-04-18

- Published with lib/net45 and a dependency on Autofac.Extras.DynamicProxy2 3.0.5.

## [1.0.0] - 2015-04-18

- First release. The package put its files under bin/Debug and obj/Debug, so it installed no assembly reference.
