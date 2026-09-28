# AGENTS.md

Rules for any AI agent (Claude Code, Copilot, Cursor, Codex) working in this repository. `CLAUDE.md` and `.github/copilot-instructions.md` only point here.

## What this is

The NuGet package `CachingServiceWithAOPSupport` (assembly and namespace `CachingServiceWithAOP`): method result caching for Autofac through a `[Cache]` attribute, Castle DynamicProxy interface interceptors and System.Runtime.Caching. Library in `src/CachingServiceWithAOP/`, tests in `tests/`. 1.0.1 (2015-04-18, net45, Autofac 3) is the published version until 2.0.0 ships. The plan is `ai-docs/plans/2026-09-27-modernization-and-v2-release.md`; start with `ai-docs/HANDOFF.md`.

## Rules

- **The recording is the contract.** `tests/Golden/1.0.1.net48-windows.json` holds what the published 1.0.1 answered on .NET Framework 4.8 (158 cases). `tests/CachingServiceWithAOP.GoldenTests` compiles the capture's `Cases.cs`, `Fixtures.cs` and `Json.cs` unchanged and compares every answer on every runtime; the only allowed differences are the named exceptions E1, E2 and E4 in `GoldenTests.cs`, each ruled on in the plan. `tests/Golden/1.0.1.net10.0-windows.json` is only the record of 1.0.1 failing on .NET 10 (E3). Never edit or regenerate anything under `tests/Golden`: when the golden test fails, the fix goes in `src/`. A fix that would change another 1.0.1 answer goes under a new name, with a decision entry and a changelog line.
- **Cache keys are 1.0.1's.** `ScriptJson` reproduces .NET Framework's JavaScriptSerializer (escaping, `\/Date(ms)\/` for DateTime and DateTimeOffset, raw Uris, `__type` first, enums as numbers and long or ulong enums refused, the Framework's `R` for doubles, fields then properties, TimeSpan in the Framework's shape). On .NET Framework the output is byte for byte (ReviewTests.R12 compares it with the real JavaScriptSerializer on net48); on .NET the last digit of a small share of doubles and floats differs. Its refusals carry a marker in `Exception.Data`, and MemoryCacheService catches only those.
- **The public API is 1.0.1's.** `tests/CachingServiceWithAOP.Tests/PublicApi-1.0.1.txt` (a copy of the frozen listing) must hold line for line, parameter names included; no public type may be added without a plan decision.
- **Dependency floors, not latest.** The library references Autofac.Extras.DynamicProxy 7.1.0, Autofac 6.5.0 and Castle.Core 5.1.1 on purpose (plan D5: Autofac 6.5 to 9 users). Dependabot ignores them; `tests/consumers/run.sh` proves the floor, the latest and the mixed set against the packed package in CI. Raise a floor only for a named reason (an advisory, an API the code needs).
- **Targets.** The library multi-targets `netstandard2.0` and `net10.0`: no net5+ APIs without an `#if` or a polyfill. It is not trim or AOT compatible (DynamicProxy emits code at run time).
- **Tests cover every artifact.** Golden (its own project, so its process holds no other test), unit and public-API tests on net10.0 and net48, package contents and dependencies at pack time in CI, fresh consumers of the packed package, `verify-published.yml` after a release. A behaviour change lands with its test. Tests never touch the network.
- **Nothing reaches nuget.org without the maintainer.** No API key is stored anywhere; `release.yml` publishes through Trusted Publishing from a job that waits at the `nuget` environment for the maintainer's approval. Never push a package from a machine.
- **Releases follow one ritual.** Update `CHANGELOG.md` (a release heading carries its date), set `<Version>` in `src/CachingServiceWithAOP/CachingServiceWithAOP.csproj`, merge, wait for `ci` to be green on `master`, then tag `v<version>` and push the tag. `release.yml` checks the tag against the version and master, tests on Linux and Windows, packs, attests, waits for the approval, pushes and creates the GitHub Release. Then run `verify-published` with the version. Tag only after green: tags are not force-pushed here.
- **Dependencies.** Lock files are committed (`RestorePackagesWithLockFile`); run plain `dotnet restore` after changing a PackageReference and commit the lock file; CI restores with `--locked-mode`. Keep a three-day cooldown on newly published versions. Actions are pinned to commit SHAs.
- **Research beats recall.** SDK, package and action versions change; re-verify any version older than three months.
- **Document for handoff.** Anything learned, decided or built goes into `ai-docs/` before you finish; rewrite `ai-docs/HANDOFF.md` when work is left unfinished.
- **No AI attribution anywhere.** Commits are the maintainer's (m4bwav).
- **Line endings.** Files are LF (`.gitattributes` and `.editorconfig`), so `dotnet format` agrees on every OS; count byte 13 with node on Windows before committing new files.

## Commands

```
dotnet restore --locked-mode
dotnet format --verify-no-changes
dotnet build -c Release
dotnet test -c Release                              # net10.0 and net48 (net48 executes only on Windows)
dotnet restore -p:AuditPipeline=true --force        # fails on any NuGetAudit finding, as CI does
dotnet pack src/CachingServiceWithAOP -c Release -o artifacts
tests/consumers/run.sh 2.0.0 artifacts              # fresh consumers of the packed package (Git Bash on Windows)
```

## Layout and traps

- `src/CachingServiceWithAOP/`: the library. `CachingServices/MemoryCacheService.cs` (store, per-key locks, Task eviction), `CachingServices/IKeyService.cs` (keys), `CachingServices/ScriptJson.cs` (the JavaScriptSerializer-compatible writer), `AOPCachingInterceptor.cs`, `AutofacCachingModule.cs`, `Extensions/`.
- `tests/Golden/`: frozen since commit 348da5d (the recordings, `Capture/` and `ApiList/`, which ran against the published 1.0.1). They have empty `Directory.Build.*` files beside them so repository props cannot change them. `.editorconfig` marks them generated code.
- Building a cache key reads every public getter of every argument, as 1.0.1 did: a test that passes a `TaskCompletionSource` or a pending `Task` as an argument hangs on `Task.Result`.
- A locked-mode lock file for a multi-OS matrix must not depend on anything the SDK infers per OS: `Microsoft.NETFramework.ReferenceAssemblies` is referenced explicitly with `PrivateAssets="all"`, and the net48 test projects pin `RuntimeIdentifier win-x86` with `SelfContained false`.
- The publish job has no checkout, so it pins `dotnet-version` instead of reading `global.json`.
- An XML comment in a project file must not contain two hyphens in a row (MSB4025).
- After 2.0.0 is released, set `PackageValidationBaselineVersion` to 2.0.0 in the csproj.

## everlast (session knowledge, load on demand)

- `ai-docs/INDEX.md` lists what past sessions learned here (solutions with verified commands, decisions with reasons, plans). At the start of a task, scan it and open only the entries whose title or tags match; no line matches: `everlast.py search "<key terms>"` before concluding nothing was recorded. Read `ai-docs/HANDOFF.md` when continuing unfinished work (everlast-resume skill).
- Before acting on an entry marked `(recheck due)`, run `everlast.py recheck <entry>`, re-run its Verified-by command only when that is read-only or safe (a build, a test, a version query), then record `everlast.py verify <entry>` or `verify <entry> --failed "what broke"`; a fix that changed is superseded, never reused blindly.
- Before finishing a task that hit a dead end, verified a non-obvious command, made a design choice, or taught you something about the user, record it (everlast-capture skill, or `everlast.py note` / `handoff`); rewrite `HANDOFF.md` when work is left unfinished. Say "nothing to record" when that is true.
- Anything naming a person, an internal host or name, a credential, or an opinion about people goes to the private sidecar (`--private`), never here. Lessons about the user or this machine go to the user tier (`--user`).
- Rules go in this file, system layout in CODEMAP.md; the doc set holds only what could not be re-derived from the code in a minute.
- Link documents together with relative markdown links: every markdown folder is reachable from an index whose lines say when to read each file (`ai-docs/INDEX.md` is generated from frontmatter; give entries a one-line `summary`), and an entry links the entries it relates to on a typed `Related:` line (`supersedes`, `contradicts`, `builds on`, `see also`). The set then reads as a graph for people in Obsidian and for agents alike. No wikilinks in the repo.
