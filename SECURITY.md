# Security policy

## Reporting a vulnerability

Report it privately through GitHub: open the repository's **Security** tab and choose **Report a vulnerability**. Please do not open a public issue for a security problem.

A confirmed problem is fixed in a new release, and the advisory is published once the fix is on nuget.org. Affected versions are then marked deprecated on nuget.org with the fixed version as the alternate.

## Supported versions

Only the latest major version (2.x) gets security fixes.

## What this package is not

The package makes no network or file access and evaluates nothing. It keeps whatever intercepted methods return in the process-wide `MemoryCache.Default` (unless you pass your own `ObjectCache`), keyed by the target's `ToString()` and the argument values; building a key reads every public getter of every argument. In a process that serves several users, a cached result is served to any caller with the same arguments: do not put `[Cache]` on methods whose results depend on who is calling unless the caller is an argument. These are documented behaviours, not vulnerabilities.
