# AGENTS.md

Rules for any AI agent (Claude Code, Copilot, Cursor, Codex) working in this repository. `CLAUDE.md` and `.github/copilot-instructions.md` only point here.

## What this is

The NuGet package `CachingServiceWithAOPSupport` (assembly and namespace `CachingServiceWithAOP`): caches method results through a `[Cache]` attribute, Castle DynamicProxy interface interceptors wired by Autofac, and `System.Runtime.Caching.MemoryCache`. 1.0.1 (2015-04-18, net45, Autofac 3, Castle.Core 3) is the published version. It is being modernized with the package-modernize skill on branch `v2`; the plan is in `ai-docs/plans/`, and `ai-docs/HANDOFF.md` says where the run stands. Start there.

## Rules

- **The recording is the contract.** `tests/Golden/1.0.1.net48-windows.json` holds what the published 1.0.1 answered on .NET Framework 4.8 (158 cases), `tests/Golden/1.0.1.net10.0-windows.json` what it answered on .NET 10 (there every proxy fails: Castle.Core 3.2.2 needs `System.Security.Permissions`), and `tests/Golden/PublicApi-1.0.1.txt` its public API with parameter names. They were captured from the published package by the programs in `tests/Golden/Capture` and `tests/Golden/ApiList`. Never edit or regenerate them, and never change those programs: when a golden test fails, the fix goes in the library, or the difference becomes a named exception the maintainer ruled on in the plan.
- **Nothing reaches nuget.org without the maintainer.** No API key is stored anywhere; releases go through Trusted Publishing from a job gated by the `nuget` environment, which the maintainer approves. Never push a package from a machine.
- **Research beats recall.** SDK, package and action versions change; re-verify any version older than three months and keep a three-day cooldown on newly published versions.
- **Document for handoff.** Anything learned, decided or built goes into `ai-docs/` before you finish; rewrite `ai-docs/HANDOFF.md` when work is left unfinished.
- **No AI attribution anywhere.** Commits are the maintainer's (m4bwav).
- **Line endings.** New files are LF; count byte 13 with node on Windows before committing.

## Commands (Phase 0 state: the 2015 projects do not build on the .NET 10 SDK)

```
dotnet run --project tests/Golden/Capture/Capture.csproj -c Release -f net48 -- <scratch>/out.json   # re-check only; never overwrite the committed recording
dotnet run --project tests/Golden/ApiList/ApiList.csproj -- <scratch>/api.txt
```

## Layout and traps

- `CachingServiceWithAOP/`: the 2015 library (non-SDK csproj, `packages.config`, a `.nuspec`, and both old nupkgs committed). `CachingServiceWithAOP.Tests/`: the 2015 MSTest project; it passes 13 of 13 against the published 1.0.1 on net48 when compiled in an SDK-style project (see `ai-docs/log.md`).
- The capture and API listing projects have empty `Directory.Build.*` and `Directory.Packages.props` beside them so later repository props cannot change what they record.
- Cache keys in the recording start with the fixture types' full names (`GoldenCapture.Fixtures.*`); the fixture namespace and type names are part of the recording.
- An XML comment in a project file must not contain two hyphens in a row (MSB4025).

## everlast (session knowledge, load on demand)

- `ai-docs/INDEX.md` lists what past sessions learned here (solutions with verified commands, decisions with reasons, plans). At the start of a task, scan it and open only the entries whose title or tags match; no line matches: `everlast.py search "<key terms>"` before concluding nothing was recorded. Read `ai-docs/HANDOFF.md` when continuing unfinished work (everlast-resume skill).
- Before acting on an entry marked `(recheck due)`, run `everlast.py recheck <entry>`, re-run its Verified-by command only when that is read-only or safe (a build, a test, a version query), then record `everlast.py verify <entry>` or `verify <entry> --failed "what broke"`; a fix that changed is superseded, never reused blindly.
- Before finishing a task that hit a dead end, verified a non-obvious command, made a design choice, or taught you something about the user, record it (everlast-capture skill, or `everlast.py note` / `handoff`); rewrite `HANDOFF.md` when work is left unfinished. Say "nothing to record" when that is true.
- Anything naming a person, an internal host or name, a credential, or an opinion about people goes to the private sidecar (`--private`), never here. Lessons about the user or this machine go to the user tier (`--user`).
- Rules go in this file, system layout in CODEMAP.md; the doc set holds only what could not be re-derived from the code in a minute.
- Link documents together with relative markdown links: every markdown folder is reachable from an index whose lines say when to read each file (`ai-docs/INDEX.md` is generated from frontmatter; give entries a one-line `summary`), and an entry links the entries it relates to on a typed `Related:` line (`supersedes`, `contradicts`, `builds on`, `see also`). The set then reads as a graph for people in Obsidian and for agents alike. No wikilinks in the repo.
