# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state
- **CachingServiceWithAOPSupport 2.0.0 is released and verified** (2026-09-28 UTC): nuget.org latest, GitHub Release v2.0.0, verify-published run 36368035840 green on three OSes, attestation from release.yml at the tag. 2.0.0-beta.1 is the verified rehearsal. Evidence: [log.md](log.md); plan: [plans/2026-09-27-modernization-and-v2-release.md](plans/2026-09-27-modernization-and-v2-release.md) (done).
- Settings: rulesets 24089709 (master, required `ci`) and 24089711 (tags, admins only); scanning, push protection, private reporting, Dependabot security updates; workflow permissions read; environment `nuget` (reviewer m4bwav); Trusted Publishing policy on nuget.org bound to release.yml and `nuget`.
- PackageValidationBaselineVersion is 2.0.0.

## Owed by Mark
- Deprecate 1.0.0 (Critical bugs) and 1.0.1 (Legacy) on nuget.org with the alternate CachingServiceWithAOPSupport, the messages in the plan's Appendix A. Optionally unlist 1.0.0 (it ships the 2015 build caches with a local path).

## Standing work
- Dependabot pull requests (weekly, Monday; three-day cooldown): merge when `ci` is green. Autofac, Autofac.Extras.DynamicProxy and Castle.Core are ignored on purpose (declared floors, plan D5); raise a floor only for an advisory or a needed API, and keep tests/consumers/run.sh's current set at the latest versions (it names Autofac 9.3.4 and DynamicProxy 8.1.0).
- The .NET 8 and 9 end of support (2026-11-10) does not touch the targets (netstandard2.0 and net10.0). .NET 11 (November 2026): add net11.0 to the test matrix when the SDK ships; global.json follows through Dependabot.
- A release: the ritual in AGENTS.md (CHANGELOG, `<Version>`, pull request, green `ci`, tag, approval, verify-published).

## Optional extras (not planned)
- An IMemoryCache or HybridCache-backed ICacheService; honouring [Cache] on interface methods; async-aware keys. Each needs a plan decision.

## Next single action
None for the package. For Mark's records: the inventory row and the kickoff's corrections are in m4bwav/package-modernization.
