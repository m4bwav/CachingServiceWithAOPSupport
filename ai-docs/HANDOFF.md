# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state
- Modernization with the package-modernize skill; plan [plans/2026-09-27-modernization-and-v2-release.md](plans/2026-09-27-modernization-and-v2-release.md). Phases 0-4 done: pull request #1 merged (40327ba), settings and rulesets on, Trusted Publishing policy created by Mark.
- Phase 5: master at e29385b has version 2.0.0-beta.1; tag v2.0.0-beta.1 pushed; release run 36365583388 waits at the `nuget` environment for Mark's approval.

## In progress
After the approval: `gh run watch 36365583388`, then `gh workflow run verify-published.yml -f version=2.0.0-beta.1`, check `gh release view v2.0.0-beta.1`. Then Phase 6: version 2.0.0, date the CHANGELOG heading (drop or keep the beta section), pull request, ci green, tag v2.0.0, approval, verify-published 2.0.0, PackageValidationBaselineVersion 2.0.0, Mark deprecates 1.0.0 and 1.0.1 (fields in the plan's Appendix A).

## Decisions made this session
- [decisions/2026-09-27-modernize-v2-fixes-in-place.md](decisions/2026-09-27-modernize-v2-fixes-in-place.md) (accepted). D5: the DynamicProxy 7.1.0 floor stands (the mixed Autofac 9 consumer passed).

## Dead ends hit
- The 2015 projects cannot build on the .NET 10 SDK; the old tests ran unchanged against the published package in a scratch net48 project.
- Passing a pending Task or TaskCompletionSource as an intercepted argument hangs: key building reads Task.Result (as 1.0.1 did). Use `dotnet test --blame-hang-timeout 60s --blame-hang-dump-type none` to name a hanging test.
- Bash heredocs turn `\\` into `\`, even quoted: write generator scripts with the editor tools.

## Also owed after this run
- Evergreen upkeep the SessionStart hooks asked for (claims due on acestep-music, comfyui-gen, dandy, evergreen; package-modernize is due 2026-10-05); the inventory row at the end of the release.

## Next single action
Mark approves release run 36365583388 in the browser; then verify-published 2.0.0-beta.1.
