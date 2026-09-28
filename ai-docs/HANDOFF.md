# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state
- Modernization with the package-modernize skill (C:\Users\m4bwa\.claude\skills\package-modernize, a junction into D:\m4bwa\Claude\Projects\Ai\package-modernize) on branch `v2` (pushed). Plan: [plans/2026-09-27-modernization-and-v2-release.md](plans/2026-09-27-modernization-and-v2-release.md).
- Phase 0 done 2026-09-27 (commit 348da5d): survey, baseline (old build fails MSB3644; old tests 13/13 against published 1.0.1 on net48), golden recordings for net48 and net10.0 (158 cases each, twice byte-identical), PublicApi-1.0.1.txt, everlast, AGENTS.md, CLAUDE.md.
- Environment `nuget` exists on GitHub (reviewer m4bwav, tag rule `v*`, secret NUGET_USER = rogersm0).
- Phase 1: plan and proposed decision written. **Stopped for the maintainer's rulings.**

## In progress
Waiting on: rulings on D0 to D16 (silence = recommendations stand), the D16 GitHub settings question, and the nuget.org Trusted Publishing policy (fields in D13).

## Decisions made this session
- Proposed: modernize as 2.0.0, fixes in the old names (E4), keys byte-identical: [decisions/2026-09-27-modernize-v2-fixes-in-place.md](decisions/2026-09-27-modernize-v2-fixes-in-place.md).

## Dead ends hit
- The 2015 projects cannot build on the .NET 10 SDK (no v4.5 reference assemblies, MSTest from Visual Studio); run the old tests through a scratch SDK net48 project instead (see log).
- 1.0.1 on .NET 10: every proxy fails (System.Security.Permissions); do not try to capture more there.

## Next single action
After the rulings: Phase 2, starting with the golden replay project that compiles tests/Golden/Capture/{Cases,Fixtures,Json}.cs by link against the new library (first check they compile against Autofac 9 / Castle 5 unchanged).
