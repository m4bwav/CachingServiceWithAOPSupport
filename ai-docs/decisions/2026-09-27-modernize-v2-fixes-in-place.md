---
title: Modernize as 2.0.0, with the fixes in the old names
kind: decision
status: proposed
date: 2026-09-27
verified: 2026-09-27
stale_after: never
tags: [v2, decision, compatibility, golden, nuget]
summary: "proposed: modernize rather than deprecate; the net48 recording is the contract on every runtime; fixes for cases where 1.0.1 threw after the work ran go into the old names (E4) instead of new names; cache keys stay byte-identical"
---

# Modernize as 2.0.0, with the fixes in the old names

## Context

CachingServiceWithAOPSupport 1.0.1 (2015) fails on every .NET Core and .NET 5+ runtime (Castle.Core 3.2.2 needs System.Security.Permissions), has no known dependents, and its 1.0.0 installs nothing. Every building block has a current, netstandard2.0 version with the same API. The golden capture shows bugs where 1.0.1 runs the intercepted method and then throws ArgumentNullException (null returns, void methods), caches nothing (the ObjectCache constructor), or throws after running on an overflowing lifetime. Evidence: [../notes/2026-09-27-phase-0-findings.md](../notes/2026-09-27-phase-0-findings.md).

## Decision (proposed; the maintainer rules at the plan review)

Modernize as 2.0.0. The net48 recording is the contract on every runtime. The fixes for cases where 1.0.1 threw after the work ran, or cached nothing, go into the existing names and are listed case by case as exception E4; nothing else may differ apart from runtime message suffixes (E1), Autofac's own errors (E2) and the .NET 10 failure record (E3). Cache keys stay byte-identical through a built-in JavaScriptSerializer-compatible writer.

## Reasons

The E4 cases are ones where 1.0.1 threw after the intercepted work had run, or cached nothing; no caller can depend on them except by catching ArgumentNullException. Keeping keys exact costs a small writer and avoids a dependency.

## Options considered

- Deprecate instead: cheapest, but leaves Autofac users without a working `[Cache]` and gives up a package that costs one run to revive.
- The skill's default rule (fixes under new names, old names exact for a major): keeps `[Cache]` breaking null-returning and void methods in 2.0.0, with twice the API, to protect a behaviour no caller can use.
- System.Text.Json keys: a new key format over 50 recorded cases and one more dependency, for no caller benefit.

## Consequences

The CHANGELOG's first paragraph states the promise and lists E4. The golden replay enforces it. A later fix that changes a non-throwing 1.0.1 answer follows the default rule.

Related: builds on [../plans/2026-09-27-modernization-and-v2-release.md](../plans/2026-09-27-modernization-and-v2-release.md)
