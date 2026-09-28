---
title: Phase 3 review findings and their dispositions
kind: note
date: 2026-09-27
verified: 2026-09-27
stale_after: never
tags: [review, phase-3, v2, keys, concurrency]
summary: "the independent read-only review of v2 (12 findings from a 50 000-input differential fuzz against the real JavaScriptSerializer and 1.0.1), and what was fixed, documented or answered for each"
---

# Phase 3 review findings (2026-09-27)

## Summary

A read-only review subagent (the skill's prompts/review-subagent.md) fuzzed ScriptJson against the real JavaScriptSerializer on net48 (about 50 000 inputs), ran probes against the published 1.0.1 and the packed 2.0.0, and ran the consumers. Twelve findings; nine fixed with a test each (tests/CachingServiceWithAOP.Tests/ReviewTests.cs, R1 to R12), three answered. Nothing found in security beyond what 1.0.1 already did, nor in the package or workflows.

## Findings and dispositions

| # | Severity | Finding | Disposition |
|---|---|---|---|
| 1 | bug | The internal null marker reached `Get<object>` callers and readers of MemoryCache.Default through `Get<T>` | Fixed: the marker becomes `default`; a cached null answers only types that can hold null, otherwise a miss (R1). Readers of `MemoryCache.Default` itself still see an internal object for a cached null, since ObjectCache cannot hold null (README) |
| 2 | bug | IntPtr and UIntPtr arguments threw InvalidCastException | Fixed: non-IConvertible primitives use ToString (R2) |
| 3 | bug | `double.MaxValue` threw OverflowException on .NET Framework (Parse of the 15-digit form) | Fixed: .NET Framework uses its own `R`; the emulation on .NET uses TryParse (R3) |
| 4 | bug | DateTimeOffset was written as an object (time-zone dependent) instead of its instant | Fixed: written as `\/Date(utc ms)\/` (R4) |
| 5 | bug | Uris were JavaScript-escaped; the Framework wrote them raw | Fixed (R5) |
| 6 | bug | A `__type` dictionary entry is written first by the Framework | Fixed (R6) |
| 7 | risk | Long and ulong enums were accepted; 1.0.1 threw | Fixed: thrown as in 1.0.1, and the call runs uncached (R7, CHANGELOG) |
| 8 | risk | On .NET, about 0.2 % of doubles and 0.5 % of floats differ from 1.0.1 in the last digit | Documented (README, CHANGELOG, AGENTS.md): keys stay one per value, and keys are per process; on .NET Framework the output is byte for byte (R12) |
| 9 | risk | The catch filter also swallowed exceptions thrown by the arguments themselves | Fixed: ScriptJson's own refusals carry a marker in Exception.Data; only those run uncached (R9) |
| 10 | risk | A task faulting between the check and the continuation stayed cached; Get-then-Remove is not atomic; after a failed computation waiters and a new caller may run in parallel | Fixed the first (always ContinueWith). Answered the others: ObjectCache has no conditional remove, and the window only evicts a newer entry early; after a failure, parallel recomputation is acceptable (no stale value is served) |
| 11 | nit | TimeSpan members did not count toward the recursion limit | Fixed (R11) |
| 12 | test gap | No differential test against the real serializer; the API test did not see added members or AttributeUsage | Fixed: R12 on net48 (edge cases plus 3 000 seeded random values), PublicApiTests.No_public_member_was_added and CacheAttribute_keeps_1_0_1_usage |

Also noted by the reviewer and answered: release.yml pushes the nupkg built on Linux without running tests/consumers/run.sh on that exact file; ci.yml runs the consumers on the same commit, and the tag must be on master after ci is green.

Related: builds on [../plans/2026-09-27-modernization-and-v2-release.md](../plans/2026-09-27-modernization-and-v2-release.md)
