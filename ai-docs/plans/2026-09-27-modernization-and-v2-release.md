---
title: Modernization and v2 release
kind: plan
status: active
date: 2026-09-27
verified: 2026-09-27
stale_after: never
tags: [v2, plan, nuget, autofac, castle, github-actions, tests, release]
summary: "the living plan for CachingServiceWithAOPSupport 2.0.0: modernize rather than deprecate, survey, what 1.0.1 gets wrong, decisions D0-D16, named golden exceptions E1-E4, the v2 API, test strategy, phases 0-7, security, verification checklist"
---

# Modernization and v2.0.0 release plan: CachingServiceWithAOPSupport

The plan for taking CachingServiceWithAOPSupport from 1.0.1 (2015, net45, Autofac 3, Castle.Core 3) to 2.0.0 on current .NET, run with the package-modernize skill (SKILL.md, references/nuget.md; worked examples TrailerClipperLib and IsImageUrlDotNet). Evidence goes to [../log.md](../log.md) as it lands; this file's checkboxes and [../HANDOFF.md](../HANDOFF.md) are current at every stop. Phase 0 facts: [../notes/2026-09-27-phase-0-findings.md](../notes/2026-09-27-phase-0-findings.md) and [../notes/2026-09-27-aop-ecosystem-research.md](../notes/2026-09-27-aop-ecosystem-research.md).

## Status

Active. Phase 2 started 2026-09-27. Rulings (2026-09-27): every recommendation D0-D16 stands ("just modernize, it can at least function as a code example"); the D16 settings are approved. Still owed by the maintainer: the Trusted Publishing policy (D13), needed before Phase 5.

## Goal

- The same `[Cache]` attribute, Autofac registration and `MemoryCacheService` API as 1.0.1, working on .NET Framework 4.6.2+ and on .NET 8 and 10 (1.0.1 cannot create a proxy on .NET 10 at all).
- Every answer 1.0.1 gave on .NET Framework 4.8 kept (the golden recording), apart from the named exceptions E1 to E4, each a changelog line.
- Current, supported dependencies: Autofac, Autofac.Extras.DynamicProxy, Castle.Core, System.Runtime.Caching; no Framework-only assembly (System.Web.Extensions).
- Thread-safe, and no longer caching failures (null returns, void methods, faulted tasks).
- Released through nuget.org Trusted Publishing with the maintainer's approval, a beta rehearsal first, verified from the registry.

## Modernize or deprecate (the kickoff's first question)

| Question | Answer (evidence) |
|---|---|
| Which AOP mechanism? | Castle DynamicProxy **interface interceptors** wired through Autofac.Extras.DynamicProxy2 (`EnableInterfaceInterceptors().InterceptedBy(typeof(AOPCachingInterceptor))`). Not PostSharp, not Unity interception, not RealProxy or ContextBoundObject. |
| Does it exist on .NET 10 and netstandard2.0? | Yes, all of it, in current majors: Autofac 9.3.4 (2026-09-18), Autofac.Extras.DynamicProxy 8.1.0 (2026-08-14; the package was renamed from DynamicProxy2 in 4.0.0, 2017), Castle.Core 5.2.1 (2025-03-09), System.Runtime.Caching 10.0.12 (2026-09-08): each targets netstandard2.0, none has an advisory. Every Autofac and Castle member the package calls still exists; only the namespace `Autofac.Extras.DynamicProxy2` becomes `Autofac.Extras.DynamicProxy`. The one Framework-only piece is JavaScriptSerializer (cache keys). |
| Does 1.0.1 work there today? | No. On .NET 10 every proxy fails with `FileNotFoundException: System.Security.Permissions` from Castle.Core 3.2.2 (golden recording `1.0.1.net10.0-windows.json`). And 1.0.0 installs nothing on any runtime (its DLL sits under `bin/Debug/`). |
| Current replacements? | Maintained attribute caching: Metalama.Patterns.Caching.Aspects (2026.1.28, compile-time weaving, about 82K downloads). Building blocks without attributes: HybridCache (Microsoft.Extensions.Caching.Hybrid 10.10.0), FusionCache 2.9.0, IMemoryCache. Interception alone: System.Reflection.DispatchProxy, AspectCore (net6+ only), AspectInjector, Rougamo.Fody. MethodCache.Fody is deprecated, EasyCaching's interceptors stale since 2023. None is a drop-in for an Autofac user who wants `[Cache]` on a registered component. |
| Does anyone depend on it? | nuget.org lists no dependent packages and no GitHub repositories; GitHub code search finds only the maintainer's own repositories and catalog mirrors; the one fork (WingStudio) is identical to upstream. About 4 000 downloads in eleven years, most of them mirrors and scanners. |

**Recommendation: modernize as 2.0.0** (D0). The whole library is about 300 lines, every building block is current and multi-targeted, Autofac plus DynamicProxy is still a maintained, popular combination (DynamicProxy 8.1.0 shipped last month), and the result turns a package that fails on every current .NET into a working one, fixing its thread-safety and null bugs on the way. The cost is one run of the ritual. The honest counter-argument is demand: no known dependents. If the maintainer rules "deprecate" instead, the exact nuget.org fields are in the appendix; the run then stops after setting nothing but the README note and the repository archive question.

## Where it stands (survey 2026-09-27)

| Fact | Value | Evidence |
|---|---|---|
| Published versions, dates, downloads, dependents | 1.0.0 and 1.0.1, both 2015-04-18, listed; 4 008 downloads (1 709 and 2 299); no dependents | notes/2026-09-27-survey.txt, research note |
| Package contents | 1.0.1: lib/net45/CachingServiceWithAOP.dll only, dependency `Autofac.Extras.DynamicProxy2 [3.0.5, )` (resolves Autofac 3.3.1, Castle.Core 3.2.2); 1.0.0: DLLs under bin/Debug and obj/Debug (installs nothing), bundled Autofac and Castle DLLs, no dependency | survey, log |
| Source, build, tests, language | Non-SDK ToolsVersion 4.0 csproj, net45, packages.config (packages/ not committed), C# 5; MSTest from Visual Studio QualityTools, 13 tests | findings note |
| Baseline | `dotnet build` exit 1 (MSB3644, no v4.5 reference assemblies); the 13 old tests pass unchanged against the published 1.0.1 on net48 in a scratch SDK project | log |
| How the README says to use it | Two lines, no usage | README.md |
| Issues, pull requests, forks | none, none, 1 fork (WingStudio, 0 ahead, 0 behind) | survey |
| Alerts, webhooks, secrets, security features | 0 alerts, 0 webhooks, 0 secrets, no workflows; secret scanning, push protection, Dependabot security updates off; default workflow permissions write | survey |
| Dead services | none (no CI, badges or config files) | survey |
| README images and badges | 0 images (`check-readme-images.mjs README.md --registry nuget` exit 0); the published package has no README | log |
| Leaked credentials | none (history grep finds only assembly public-key tokens) | log |
| Golden capture | 158 cases on net48 and net10.0, each recording twice byte-identical; PublicApi-1.0.1.txt 46 lines | tests/Golden, log |

## What the old version gets wrong, confirmed, and what v2 does

Case names are the recording's `group | name`.

1. **Nothing works on .NET Core or .NET 5+** (every net10.0 proxy case: FileNotFoundException for System.Security.Permissions). Fix: current Castle.Core and Autofac; v2 answers the net48 contract on every runtime (E3). Changelog: "Runs on .NET 8 and 10 and .NET Framework 4.6.2+ (1.0.1 failed on every .NET Core runtime)."
2. **A `[Cache]` method that returns null runs on every call and then throws ArgumentNullException at the caller** (`Interception | ReturnsNull twice`: two throws, 2 calls; `Echo<string> null` the same). Fix: the null is returned and cached (the method runs once). E4.
3. **A `[Cache]` void method throws ArgumentNullException after running** (`VoidCached twice`). Fix: a void method runs every time and is never cached (there is nothing to cache and its side effects must happen). E4.
4. **`Get(key, func)` whose func returns null throws, and runs func again next time** (`MemoryCacheService | Get(key, func) returning null`). Fix: returns null and caches it. E4. `Set(key, null)` keeps throwing ArgumentNullException (unchanged).
5. **`Get(key, func)` returns `default` without calling func when the key holds a value of another type** (`Get(key, func) with a value of another type present`: `[0, 0, "text"]`). Fix: treated as a miss: func runs and its value replaces the entry. E4.
6. **`MemoryCacheService(ObjectCache)` caches nothing**: its lifetime is never set, so every entry expires on arrival (`lifetime | ctor(ObjectCache Default) immediate` and `new MemoryCache`: `["set", null]`). Fix: the same five-minute default as `new MemoryCacheService()`. E4.
7. **An overflowing lifetime throws ArgumentOutOfRangeException at `Set`, after the intercepted method ran** (`ctor(long MaxValue)`, `ctor(TimeSpan MaxValue)`, `ctor(long MinValue)`). Fix: a lifetime past the end of time means "never expires"; one below the start of time means "not cached", like any other negative lifetime. E4.
8. **Thread safety**: the per-key lock table is a static, unsynchronised Dictionary written from every thread, and never shrinks; intercepted calls take no lock at all, so concurrent callers all run the method (not in the recording: single-threaded). Fix: a concurrent lock table whose entries are removed once the value is stored; intercepted calls use it too (one execution per key under concurrency). New tests.
9. **`[Cache]` on a Task-returning method caches the Task, faulted or cancelled ones included** (`CachedTask twice`: the same Task object, 1 call, which v2 keeps). Fix: the Task stays cached while it succeeds and is evicted when it faults or is cancelled; `ValueTask` and `ValueTask<T>` methods are not cached (a ValueTask may be awaited once). New tests.
10. **A cyclic argument makes the call fail before the method runs** (JavaScriptSerializer's InvalidOperationException; `DefaultCacheKeyService | WithNode(cycle)`). Fix at the interceptor: when no key can be made, the call proceeds uncached. `GenerateUniqueKeyForCall` itself keeps throwing InvalidOperationException, so the recorded case stays exact. New test.
11. Kept, documented in the README (no change): keys start with the target's `ToString()` (a `ToString` override splits or merges entries: `target ToString override splits entries per instance`); every `MemoryCacheService` shares `MemoryCache.Default`; `[Cache]` on the interface method alone does nothing; lifetimes of zero or below mean "not cached"; the tick constructors take 100 ns ticks; `char` NUL and null give the same key; dictionaries key by insertion order.

## Decisions (recommendation first; the maintainer rules in the plan review, silence means the recommendation stands)

| # | Question | Recommendation | Why | Alternative |
|---|---|---|---|---|
| D0 | Modernize or deprecate | **Modernize as 2.0.0**, then deprecate 1.0.0 (Critical bugs) and 1.0.1 (Legacy) pointing at 2.0.0 | See "Modernize or deprecate" above: cheap, every building block current, fixes a package that fails on all current .NET | Deprecate only (fields in the appendix), archive the repository |
| D1 | The compatibility promise | The net48 recording (158 cases) is the contract **on every runtime**, compared case by case as JSON text; differences allowed only in one exception table (E1 to E4); `PublicApi-1.0.1.txt` must hold line for line (one runtime-only line named). **The fixes of items 2 to 7 go into the old names** (E4), not under new names | The skill's default rule (a fix that changes an old result goes under a new name, the old name exact for a major) exists to protect callers who rely on an answer. Every E4 case is one where 1.0.1 threw after the work had already run, or cached nothing: no caller can rely on that except by catching ArgumentNullException, and keeping it would ship a v2 whose `[Cache]` still breaks null-returning and void methods | The default rule: `[Cache]` and `MemoryCacheService` stay exact, the fixes behind an opt-in (a `CacheOptions` or a second attribute); twice the surface for no caller |
| D2 | Export shape | Package id `CachingServiceWithAOPSupport`, assembly and root namespace `CachingServiceWithAOP` (unchanged), same namespaces and type names | Every `using` and reference keeps compiling | Rename the assembly to match the id: breaks every reference for nothing |
| D3 | Behaviour at the edges | Items 1 to 11 above: fix 1 to 10, keep 11 | Per item | Keep any fix exact instead (name it) |
| D4 | Is a major warranted | **Yes, 2.0.0** | The public API exposes Castle's `IInvocation` and Autofac's `ContainerBuilder`, `IComponentRegistration`, `IRegistrationBuilder`, `IContainer`, `Module`; their assemblies move from 3.x to current majors, a binary break for any caller. No patch can fix .NET 10 without moving them | none |
| D5 | Runtime dependencies | **Autofac.Extras.DynamicProxy `[7.1.0, )`, Autofac `[6.5.0, )`, Castle.Core `[5.1.1, )`** (the floors DynamicProxy 7.1.0 declares), System.Runtime.Caching `10.0.12`; no System.Text.Json (D6 keys). Condition: a consumer test with Autofac 9.3.4 plus DynamicProxy 7.1.0 resolved must pass; if it fails, the floors become DynamicProxy 8.1.0, Autofac 9.3.2, Castle.Core 5.2.1 | The widest audience: Autofac 6.5 to 9 users can take v2 without upgrading their container; NuGet resolves the lowest applicable version, so the mixed pair is what an Autofac 9 user gets unless they reference DynamicProxy 8 themselves, and CI proves it | Floors at the latest (DynamicProxy 8.1.0, Autofac 9.3.2): simpler, but every Autofac 6 to 8 user is shut out |
| D6 | Names; cache keys | Keep every public name (46 API lines). **Keys stay byte-identical to 1.0.1**: a small built-in writer reproduces JavaScriptSerializer's output for what the recording covers (primitives, strings with its escaping, DateTime as `\/Date(ms)\/`, Guid, enums as numbers, char, decimal, double in round-trip form, NaN and Infinity, arrays, lists, dictionaries, objects as public fields then properties, TimeSpan as its properties, null) and keeps its InvalidOperationException for cycles. No additions | `GenerateUniqueKeyForCall` is public and its 50 recorded answers stay exact without a System.Text.Json dependency; the misspelt type parameter `TRegistrionStyle` stays (invisible to callers, in the API file) | System.Text.Json keys with `IncludeFields`: a new key format, a named exception over 50 cases, and one more dependency |
| D7 | Errors | Keep 1.0.1's exceptions everywhere the recording shows them (NullReferenceException on a null invocation or null `ObjectCache`, ArgumentNullException from MemoryCache); add none | Exactness; tidier guard clauses gain callers nothing | ArgumentNullException guards (named E cases) |
| D8 | Targets and CI matrix | Library `netstandard2.0;net10.0`; tests `net10.0;net48`; CI on Ubuntu and Windows, packed-package consumers on net48 and net10.0 (Windows) plus the Autofac floor, current and mixed combinations; verify-published on three OSes | Overlay default; netstandard2.0 reaches .NET Framework 4.6.2+ and .NET 8; net10.0 gets the current build | Add net8.0 (only if a consumer needs it) |
| D9 | Tooling | SDK 10.0.401 `global.json` `latestFeature`; C# `LangVersion latest`, `Nullable enable`, warnings as errors, .NET analyzers `latest-recommended`, `dotnet format` in CI; NUnit 4.6.1 (5.0.0 came out 2026-09-27, inside the cooldown), Microsoft.NET.Test.Sdk 18.10.1, NUnit3TestAdapter 6.3.0; lock files with `--locked-mode`; Source Link, snupkg, deterministic; `EnablePackageValidation` without a baseline (1.0.1 is net45 with other dependency majors) plus the PublicApi test; **not** `IsAotCompatible` (DynamicProxy emits code at run time); `.slnx` | The skill's NuGet defaults and the runs' choices | xunit.v3 |
| D10 | Lockfile, bot pull requests | New `packages.lock.json` per project; there are no bot pull requests | | |
| D11 | Dead services, badges, images | No dead services; README gets three badges (NuGet version, CI, downloads) and no other image; the package gets a generated icon (the skill's scripts/make-icon.py, white glyph on #1E4078, 128 px) and `PackageReadmeFile` | Overlay icon rule (L-070 `generate-missing-icon`) | ComfyUI-drawn icon |
| D12 | Old files to remove | `CachingServiceWithAOP.sln`, both old csproj files, `CachingServiceWithAOP.nuspec`, both committed nupkgs, `packages.config` files, `app.config`, the Properties/AssemblyInfo.cs files, the MSTest project (its 13 tests ported to NUnit, the all-zero "unique" Guid fixed), the 60-line `.gitignore` of build paths (replaced by the template) | Dead build system; the nupkgs are on nuget.org | Keep the old tests as they are: they cannot build on the .NET 10 SDK |
| D13 | Release | `2.0.0-beta.1` rehearsal, then `2.0.0`; `release.yml` with the master-ancestry check and the Windows net48 job, gated by environment `nuget` (created 2026-09-27: reviewer m4bwav, tag rule `v*`, secret NUGET_USER). **Needed from the maintainer now: the Trusted Publishing policy on nuget.org**: Repository Owner `m4bwav`, Repository `CachingServiceWithAOPSupport`, Workflow File `release.yml`, Environment `nuget`, scope "push only new package versions" with the glob `CachingServiceWithAOPSupport` | The skill's NuGet release path | |
| D14 | Default branch, extras | Keep `master`. No extras (a HybridCache or IMemoryCache backend, async-aware keys, attribute on interfaces) in this release; listed in the HANDOFF as optional | Do not gold-plate | Add an `IMemoryCache`-backed `ICacheService` |
| D15 | Dependents | None known; after 2.0.0 the deprecations of D0 point the two old versions at it | | |
| D16 | GitHub settings (the one question below) | Apply before the pull-request stop (L-077 `settings-before-the-pull-request`) | | |

## Named golden exceptions (the only allowed differences from `1.0.1.net48-windows.json`)

- **E1** ArgumentException messages on .NET Core runtimes end with ` (Parameter 'name')`: compared up to that suffix (7 cases).
- **E2** Errors raised by Autofac itself (interception without `RegisterCachingModule`, `RegisterCachingModule(null)`): Autofac's exception type and wording follow its current major; compared as "throws" plus the innermost exception's type where it names this package's types.
- **E3** `1.0.1.net10.0-windows.json` is a record of failure, not a contract: on .NET 10, v2 is compared with the net48 recording (plus E1).
- **E4** The fixes of items 2 to 7, case by case: `Interception | ReturnsNull twice` → `[null, null, 1]`; `Interception | Echo<string> null` → `[null, null, 1]`; `Interception | VoidCached twice` → `["returned", "returned", 2]`; `MemoryCacheService | Get(key, func) returning null` → `[null, null, 1]`; `MemoryCacheService | Get(key, func) with a value of another type present` → `[1, 1, null]`; `lifetime | ctor(ObjectCache Default) immediate` and `ctor(ObjectCache new MemoryCache) immediate` → `["set", "v"]`; `lifetime | ctor(long MaxValue)` and `ctor(TimeSpan MaxValue)` → `["set", "v"]`; `lifetime | ctor(long MinValue)` → `["set", null]`.

## Proposed public API (v2)

Unchanged: the 46 lines of `tests/Golden/PublicApi-1.0.1.txt`, with Castle and Autofac types resolved to their current majors. The only runtime-dependent line, `CacheAttribute implements System.Runtime.InteropServices._Attribute`, comes from the runtime's `System.Attribute`, not from this package; if Phase 2 finds it absent on .NET 10, the API test checks that line on net48 only. No additions.

## Build and package specifics

- Layout: src/CachingServiceWithAOP/CachingServiceWithAOP.csproj (PackageId `CachingServiceWithAOPSupport`, AssemblyName and RootNamespace `CachingServiceWithAOP`), `tests/CachingServiceWithAOP.Tests/` (unit, ported old tests, PublicApi, concurrency and async tests; NUnit, `net10.0;net48`), `tests/CachingServiceWithAOP.Golden.Tests/` (the golden replay alone in its own project, compiling `tests/Golden/Capture/{Cases,Fixtures,Json}.cs` by link, unchanged; L-072 `golden-alone-in-process`), `tests/consumers/` (packed-package consumer fixtures), `CachingServiceWithAOPSupport.slnx`, `Directory.Build.props`, `global.json`, `.editorconfig`, `.gitattributes`, `.gitignore`, `icon.png`, `README.md`, `CHANGELOG.md`, `SECURITY.md`.
- The capture's `Cases.cs` uses `System.Runtime.Caching`, Autofac and Castle public API only; it must compile unchanged against the v2 dependency set (checked first in Phase 2).
- Workflows from IsImageUrlDotNet: `ci.yml` (build, format, test on Ubuntu and Windows, pack, consumers of the packed package on net48 and net10.0), `release.yml` (tag equals csproj version, commit on master, Windows net48 tests, pack, attest, environment-gated push without checkout, GitHub Release), `verify-published.yml` (both indexes, signature, fresh consumers on three OSes); `dependabot.yml` (nuget, github-actions, dotnet-sdk; Autofac, DynamicProxy and Castle.Core `ignore`d for version updates so no bot pull request raises the declared floors, security updates still on). The run also copies these three workflows into the skill as `templates/nuget/` templates.

## Phases

### Phase 0: survey and baseline (2026-09-27, no package code changed)
- [x] Cloned to D:/m4bwa/Claude/Projects/Ai/labs/CachingServiceWithAOPSupport, branch `v2`; survey in ai-docs/notes/2026-09-27-survey.txt
- [x] Old build as it is: MSB3644; old tests unchanged against the published 1.0.1 on net48: 13 of 13
- [x] Golden capture from the published 1.0.1 committed under tests/Golden (commit 348da5d: the recordings, the capture and API-listing programs stay as they are from here on)
- [x] everlast registered (mode repo, sync push); AGENTS.md, CLAUDE.md (the AGENTS.md import line), Copilot pointer
- [x] Environment `nuget` created (reviewer m4bwav, tag rule `v*`, secret NUGET_USER)
### Phase 1: plan
- [x] This plan and [../decisions/2026-09-27-modernize-v2-fixes-in-place.md](../decisions/2026-09-27-modernize-v2-fixes-in-place.md). **Stop**: the maintainer rules on the table, answers the GitHub question, adds the Trusted Publishing policy.
### Phase 2: rewrite on branch v2
- [x] Remove the D12 files; add the templates; generated icon
- [x] Golden replay first (first build: 2 differences, a harness fix and TimeSpan's shape on .NET, see the log), then green apart from E1 to E4; canary after committing (L-074 `commit-before-canary`): a planted line in src/ turns it red, reverted, green (both runs logged); `git diff --exit-code 348da5d -- tests/Golden` empty
- [x] src/, the rest of the tests, README, CHANGELOG, SECURITY.md, AGENTS.md
- [x] D5 floor test (Autofac 9.3.4 with DynamicProxy 7.1.0) green: the 7.1.0 floor stands; verified on net10.0 and net48 and from a fresh clone (log)
- [x] Workflows and Dependabot, actions pinned to SHAs, actionlint, zizmor and check-workflow-shell.py clean
- [ ] Pushed; pull request with a "For review" list
### Phase 3: review
- [ ] Independent read-only review (prompts/review-subagent.md); findings fixed or answered; summary on the pull request
- [ ] Rulesets and security settings of D16 applied (L-077). **Stop** for the pull-request review.
### Phase 4: CI, settings, merge, cleanup
- [ ] CI green (run id); merge after the review (read the SHA and method back); tag ruleset; nothing else to clean up (no bot pull requests, issues, webhooks)
### Phase 5: release rehearsal
- [ ] `2.0.0-beta.1` tagged after master is green; **stop** for the approval; verify-published green (run id)
### Phase 6: release
- [ ] Changelog dated; `2.0.0` tagged; **stop** for the approval; verified (verify-published, GitHub Release, symbols); PackageValidationBaselineVersion set to 2.0.0
- [ ] The maintainer deprecates 1.0.0 and 1.0.1 on nuget.org (fields in the appendix)
### Phase 7: wrap-up
- [ ] HANDOFF.md around standing work; inventory row; lessons into the skill; the kickoff's "What the run found wrong"

## Test strategy: every artifact, every runtime, and the behaviour itself

| Layer | What it proves | How | Runs where |
|---|---|---|---|
| Golden replay | Every 1.0.1 answer kept, apart from E1 to E4 | The capture's Cases.cs compiled against v2, compared case by case with the net48 recording | net48 (Windows), net10.0 (Ubuntu, Windows) |
| Public API | Every 1.0.1 name and parameter name | Reflection over v2 compared with PublicApi-1.0.1.txt | both |
| Ported old tests | The 13 original scenarios | NUnit, unique keys | both |
| Fixes and concurrency | Items 2 to 10: null and void, faulted tasks, ValueTask, cycles, one execution per key under parallel callers, the lock table empties | NUnit | both |
| Package | Contents, dependencies, README, icon, symbols | pack-list test and package validation at pack time | Ubuntu |
| Consumers | The packed nupkg restores and works | C# console apps on net48 and net10.0 against the packed package, with the Autofac floor, current and mixed combinations | Windows (net48), Ubuntu |
| Registry | The published package | verify-published: indexes, signature, fresh consumers | three OSes |

| Artifact | Runtime lines | Other OSes | Other runtimes | Bare engine |
|---|---|---|---|---|
| lib/netstandard2.0 | net48 (tests), net462+ by contract | Windows only for net48 | .NET 8 via netstandard2.0 in the floor consumer | n/a |
| lib/net10.0 | net10.0 | Ubuntu, Windows, macOS (verify) | n/a | n/a |

## Pull requests, issues and forks: disposition

| Item | What it is | Disposition | Comment to post |
|---|---|---|---|
| (none) | No issues or pull requests ever | nothing | |
| WingStudio/CachingServiceWithAOPSupport | Fork, 0 ahead, 0 behind | nothing | |

## Security

No leaked tokens, webhooks, secrets or apps found. The published 1.0.0 carries the 2015 source files, PDBs and obj/ build caches, whose FileListAbsolute.txt names a local path (`C:\Users\Mark\Documents\Visual Studio 2012\Projects\...`): low risk, and nuget.org cannot delete a version; unlisting 1.0.0 after 2.0.0 (with the D0 deprecation) hides it from search, and is the maintainer's call. Settings for D16: secret scanning and push protection on, private vulnerability reporting on, Dependabot security updates on, default workflow permissions read, rulesets on master and on tags. Workflows: `permissions: contents: read` by default, `id-token` set to write only in the push job (no checkout, first-party actions), actions pinned to SHAs, `persist-credentials: false`, three-day cooldown on new dependency versions. The library makes no network or file access and evaluates nothing; it stores whatever the intercepted methods return in the process-wide MemoryCache (README says so, and that keys include argument values). SECURITY.md with private reporting; supported: 2.x.

## Badges and images: disposition

| Image or badge | What it shows now | Decision | New URL or reason |
|---|---|---|---|
| (none in the old README) | | | |
| NuGet version (new) | | add | img.shields.io/nuget/v/CachingServiceWithAOPSupport |
| CI (new) | | add | github.com/m4bwav/CachingServiceWithAOPSupport/actions/workflows/ci.yml/badge.svg |
| Downloads (new) | | add | img.shields.io/nuget/dt/CachingServiceWithAOPSupport |
| Package icon (new) | nuget.org shows the default icon | add | icon.png packed as PackageIcon |

## Verification checklist (what "done" means)

The NuGet checklist of references/nuget.md, plus: the golden replay green on net48 and net10.0 with exactly E1 to E4; the D5 mixed-combination consumer green; `git diff --exit-code 348da5d -- tests/Golden` empty before every tag.

## Risks and open points

- D5: if Autofac 9 with DynamicProxy 7.1.0 fails, the floors rise (already decided, no new stop).
- Castle.Core 5 may name proxy types or wrap exceptions differently (`resolved ISvc is a proxy` records namespace `Castle.Proxies`); a difference there becomes a named exception proposed in the pull request, never an edited recording.
- `ctor(TimeSpan 100 ms) immediate, then after 600 ms` and the 50 ms cases are timing-based with wide margins; a slow CI runner could flake them. The capture ran them twice on each runtime identically.

## GitHub: the one question (D16)

Apply with gh, before the pull-request stop: (1) ruleset on `master`: deletion and non-fast-forward blocked, required status check `ci`, admin bypass; (2) tag ruleset `v*`: only admins create, update or delete; (3) secret scanning, push protection, private vulnerability reporting and Dependabot security updates on; (4) default workflow permissions read, Actions may not approve pull requests; (5) `delete_branch_on_merge` on, wiki and projects off, homepage the nuget.org page, description "Method result caching for Autofac through a [Cache] attribute and Castle DynamicProxy interceptors", topics autofac, caching, aop, castle-dynamicproxy, nuget. **Deletions on GitHub: none** (the old nupkgs and build files leave through the pull request; the `v2` branch goes when the pull request merges, by the setting in (5)).

## Appendix A: nuget.org deprecation fields

After 2.0.0 is live (D0), in the maintainer's browser (Manage package, Deprecation):

- 1.0.0: reason **Critical bugs**; alternate package `CachingServiceWithAOPSupport`, version "Latest"; message: "1.0.0 installs no assembly (its DLL is packed under bin/Debug). Use 2.0.0 or later."
- 1.0.1: reason **Legacy**; alternate package `CachingServiceWithAOPSupport`, version "Latest"; message: "1.0.1 needs Autofac 3 and Castle.Core 3 and fails on .NET Core and .NET 5+. 2.0.0 keeps the same API on .NET Framework 4.6.2+, .NET 8 and .NET 10."

If the ruling is "deprecate instead" (no 2.0.0): both versions, reason **Legacy**, alternate package `Metalama.Patterns.Caching.Aspects` (any version), message: "Unmaintained since 2015 and fails on .NET Core and .NET 5+ (Castle.Core 3). For attribute-based caching use Metalama.Patterns.Caching.Aspects, or Autofac.Extras.DynamicProxy with your own interceptor over HybridCache." Then the repository is archived (a question for the maintainer then).

## Next single action

The maintainer rules on D0 to D16, answers the D16 question, and adds the Trusted Publishing policy on nuget.org (D13). Then Phase 2 starts with the golden replay project.
