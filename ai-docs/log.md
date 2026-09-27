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
