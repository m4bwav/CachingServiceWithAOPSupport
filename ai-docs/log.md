# Log

Append-only. One line per operation: `## [YYYY-MM-DD] op | title` where op is one of add, update, supersede, verify, verify-failed, prune, handoff, index. Newest at the bottom. Never edited, only appended; this is the history the entries themselves do not carry.

## [2026-09-27] init | scaffolded

## [2026-09-27] add | Phase 0: survey, baseline, golden capture (Windows 11, SDK 9.0.317 and 10.0.401, gh)
- Cloned m4bwav/CachingServiceWithAOPSupport into D:/m4bwa/Claude/Projects/Ai/labs; branch `v2` from master a34ea94. package-modernize at 35565a9 (C-20260927-12).
- `scripts/survey-nuget.sh CachingServiceWithAOPSupport m4bwav/CachingServiceWithAOPSupport` > ai-docs/notes/2026-09-27-survey.txt: versions 1.0.0 and 1.0.1 (both 2015-04-18, listed), 4 008 downloads, owner rogersm0; GitHub: no issues, pull requests, workflows, webhooks, secrets, tags; 0 alerts; every security feature off; workflow permissions write; fork WingStudio (0 ahead, 0 behind).
- Reading and findings: ai-docs/notes/2026-09-27-phase-0-findings.md. Research (versions, alternatives, dependents, by a research subagent): ai-docs/notes/2026-09-27-aop-ecosystem-research.md. nuget.org lists no dependents.
- Published 1.0.0 puts its DLLs under bin/Debug and obj/Debug (installs nothing); its CachingServiceWithAOP.dll equals 1.0.1's. Committed nupkgs equal the published ones.
- Baseline as it is: `dotnet restore CachingServiceWithAOP.sln` exit 0 (packages.config ignored); `dotnet build CachingServiceWithAOP.sln` exit 1, MSB3644 (no .NETFramework v4.5 reference assemblies; no Visual Studio here).
- Old tests unchanged (linked) in a scratch SDK net48 project with MSTest 4.4.1 against the published 1.0.1: 13 passed, 0 failed.
- Golden capture, tests/Golden/Capture (C#, net48 and net10.0, hand-written JSON writer, 158 cases): `dotnet build -c Release`, then `dotnet run -c Release -f <fw> --no-build -- <out>` twice per runtime: byte-identical on both (net48: .NET Framework 4.8.9345.0; net10.0: .NET 10.0.12). Rerun from the repository copy: byte-identical to the scratch runs. assemblySha256 2f956a8bc3d97a8aae8a4ab1a5794da528655c7401068a313b0d8022cec53f3d. Loaded: Autofac 3.3.0.0, DynamicProxy2 3.0.0.0, Castle.Core 3.2.0.0.
- On net10.0 every proxy fails (Castle.Core 3.2.2 needs System.Security.Permissions). No macOS run: net48 exists only on Windows, and the net10.0 answers depend on no OS API (MemoryCache, reflection, a load failure), so macOS could not differ in a way v2 must honour.
- tests/Golden/PublicApi-1.0.1.txt from tests/Golden/ApiList (net48, reflection over the published DLL): 46 lines.
- check-readme-images.mjs README.md --registry nuget: 0 images, exit 0. History grep for credentials: only assembly public-key tokens.
- Versions checked for the capture (registration index): System.Runtime.Caching 10.0.12 (2026-09-08), Microsoft.NETFramework.ReferenceAssemblies 1.0.3 (2022-08-19), MSTest 4.4.1 (2026-09-16). NUnit 5.0.0 was published 2026-09-27: inside the cooldown.
- everlast registered (mode repo, sync push); AGENTS.md, CLAUDE.md (@AGENTS.md import), .github/copilot-instructions.md.
- Trap: `--` inside an XML comment of Capture.csproj stopped the build (MSB4025); reworded.
## [2026-09-27] index | rebuilt (2 entries)

## [2026-09-27] add | Phase 1: plan and proposed decision
- Phase 0 committed as 348da5d and pushed (`git push -u origin v2`).
- Environment `nuget` created with gh: `gh api -X PUT repos/m4bwav/CachingServiceWithAOPSupport/environments/nuget` (reviewer user id 156112, custom_branch_policies true), `POST .../deployment-branch-policies name=v* type=tag`, `gh secret set NUGET_USER --env nuget --body rogersm0`; read back: required_reviewers and branch_policy rules, policy `v* tag`, secret NUGET_USER.
- Versions for the plan (registration index, listed, published): Autofac 9.3.4 (2026-09-18), Autofac.Extras.DynamicProxy 8.1.0 (2026-08-14; 7.1.0 of 2023-06-29 needs Autofac >= 6.5.0 and Castle.Core >= 5.1.1; 8.x needs Autofac >= 9.3.x and Castle.Core >= 5.2.1), Castle.Core 5.2.1 (2025-03-09), System.Runtime.Caching 10.0.12, System.Text.Json 10.0.12, MSTest 4.4.1, NUnit 4.6.1 (5.0.0 published 2026-09-27: cooldown), NUnit3TestAdapter 6.3.0, Microsoft.NET.Test.Sdk 18.10.1.
- ai-docs/plans/2026-09-27-modernization-and-v2-release.md (D0-D16, E1-E4); ai-docs/decisions/2026-09-27-modernize-v2-fixes-in-place.md (proposed).
- Stop for the maintainer's plan review.
## [2026-09-27] index | rebuilt (4 entries)

## [2026-09-27] update | Plan rulings
- Mark: "do the decisions as you recommend, just modernize, it can at least function as a code example". D0-D16 stand, E1-E4 accepted, D16 settings approved (apply before the pull-request stop). Trusted Publishing policy not yet confirmed: ask again at the pull-request stop.

## [2026-09-27] add | Phase 2: rewrite on v2
- Versions (registration index via the skill's nuget-latest.py, all older than three days): Autofac.Extras.DynamicProxy 7.1.0 (2023-06-29, floor), Autofac 6.5.0 (floor), Castle.Core 5.1.1 (floor), System.Runtime.Caching 10.0.12 (2026-09-08), NUnit 4.6.1 (5.0.0 of 2026-09-27 inside the cooldown), NUnit3TestAdapter 6.3.0, Microsoft.NET.Test.Sdk 18.10.1, System.Text.Json 10.0.12 (net48 golden test only), Microsoft.NETFramework.ReferenceAssemblies 1.0.3; consumers' current set Autofac 9.3.4 (2026-09-18) and DynamicProxy 8.1.0 (2026-08-14). Action SHAs as in IsImageUrlDotNet (checked there 2026-09-27).
- D12 files removed; 1.x sources moved with `git mv` into src/CachingServiceWithAOP (history kept).
- Golden replay first (tests/CachingServiceWithAOP.GoldenTests, Cases.cs, Fixtures.cs and Json.cs linked unchanged). First build: 2 differences per runtime apart from E1-E4. (1) the E2 case's recorded value is a list holding a throw, so the harness now compares throws at any depth for E2 cases; (2) on net10.0 only, `DefaultCacheKeyService | WithTimeSpan(90 s)`: TimeSpan has four more public properties since .NET 7 and .NET computes TotalHours exactly (0.025 against the Framework's 0.024999999999999998), so ScriptJson writes TimeSpan in the Framework's shape and arithmetic. Then green on net10.0 and net48 (3 of 3 each).
- Commit c1a8a16, then the canary (L-074): planted `if (result is int i) { invocation.ReturnValue = i + 1; }` in MemoryCacheService.TryAnswer: golden test red on net10.0 and net48; `git checkout -- src/CachingServiceWithAOP/CachingServices/MemoryCacheService.cs`; green on both.
- `git diff --exit-code 348da5d -- tests/Golden`: empty.
- Unit tests: first run hung on both runtimes (`--blame-hang-timeout` named FixTests.A_task_that_faults_later_is_evicted): the test passed a TaskCompletionSource as an argument, and building its key reads Task.Result, which blocks while the task runs (1.0.1's JavaScriptSerializer did the same). Fixture changed; README and AGENTS.md warn. Then 37 of 37 on net10.0 and net48.
- `dotnet restore --locked-mode`, `dotnet format --verify-no-changes`, `dotnet build -c Release` (0 warnings), `dotnet restore -p:AuditPipeline=true --force` (clean), `dotnet pack`: nupkg holds nuspec, README.md, icon.png, lib/net10.0 and lib/netstandard2.0 dll and xml; both groups depend on Autofac 6.5.0, Autofac.Extras.DynamicProxy 7.1.0, Castle.Core 5.1.1, System.Runtime.Caching 10.0.12.
- D5 condition: tests/consumers/run.sh 2.0.0 artifacts: floor (Autofac 6.5.0.0, DynamicProxy 7.1.0.0), current (9.3.4.0, 8.1.0.0) and mixed (9.3.4.0, 7.1.0.0) all answer on net10.0 and net48. The 7.1.0 floor stands.
- actionlint 1.7.12 (release zip, checksum verified): clean. zizmor 1.30.1 --offline: no findings (4 suppressed). check-workflow-shell.py: exit 0. check-readme-images.mjs: the two shields.io badges ok, the ci badge 404 until ci.yml is on master.
- Fresh clone of v2 (f0ab1df): locked restore, build, tests 4 of 4 runs green.
- Trap: the Bash tool's quoted heredoc turned a Python `'%s\n'` into a real newline inside run.sh; repaired with chr(92) (the memory note of 2026-09-25 again).

## [2026-09-27] add | Pull request, CI, settings (L-077), templates
- Pull request #1 (v2 into master) opened with a "For review" list; CI run 36363389674: build and test (ubuntu-24.04) success, build and test (windows-latest) success, ci success (tests on net10.0 and net48, package content and dependency check, consumers in three dependency sets).
- D16 settings applied with gh before the pull-request stop (L-077) and read back: branch ruleset 24089709 `master` (copied from IsImageUrlDotNet's: deletion, non_fast_forward, required check `ci`, admin bypass), tag ruleset 24089711 "Tags only by admins" (templates/rulesets/tags-admins-only.json); secret scanning and push protection enabled; vulnerability alerts, Dependabot security updates and private vulnerability reporting on; default workflow permissions read, Actions may not approve pull requests; delete_branch_on_merge true, wiki and projects off, homepage the nuget.org page, description and topics (aop, autofac, caching, castle-dynamicproxy, nuget).
- The skill's NuGet templates now come from this run's CI-proven workflows: package-modernize pull request #5 (782def4, C-20260927-14, L-083 to L-085). Earlier: #4 (01283e5, C-20260927-13, L-078 to L-082).
## [2026-09-27] index | rebuilt (5 entries)

## [2026-09-27] add | Phase 3: independent review and fixes
- Review subagent (prompts/review-subagent.md, read-only, about 9 minutes): 12 findings from a differential fuzz of about 50 000 inputs against the real JavaScriptSerializer on net48, probes against the published 1.0.1 and the packed 2.0.0, and run.sh (6 of 6). Dispositions: ai-docs/notes/2026-09-27-phase-3-review-findings.md (9 fixed with a test each, 3 answered or documented).
- Fixes: null marker never leaks (Get<object>, value types miss); IntPtr keys; .NET Framework uses its own "R" (double.MaxValue no longer overflows); DateTimeOffset as its instant; raw Uris; `__type` first; long and ulong enums refused as in 1.0.1 and run uncached; only ScriptJson's own refusals (Exception.Data marker) run uncached; ContinueWith always for tasks; TimeSpan counts toward the depth limit.
- New tests: ReviewTests R1-R12 (R12, net48 only, compares ScriptJson with System.Web.Extensions' JavaScriptSerializer over edge cases and 3 000 seeded random strings, doubles and floats: equal), PublicApiTests.No_public_member_was_added and CacheAttribute_keeps_1_0_1_usage.
- `dotnet test` (solution): golden 3 of 3 on net10.0 and net48; unit 48 on net10.0, 49 on net48. `dotnet format --verify-no-changes` clean. `git diff --exit-code 348da5d -- tests/Golden` empty.
- Review summary posted on pull request #1. CI run 36364171385 on cc4cdd4: ubuntu success, windows success, ci success. **Stop for Mark's pull-request review.**
