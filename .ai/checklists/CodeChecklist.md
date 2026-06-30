# Code Checklist

Use for every PR before requesting review. Mark each item: ✓ Pass | ✗ Fail | N/A (with reason).

## Build
- [ ] `dotnet build` succeeds with `TreatWarningsAsErrors=true` — zero warnings
- [ ] All unit tests pass (`dotnet test`)
- [ ] No new compiler suppressions (`#pragma warning disable`, `[SuppressMessage]`) added without a comment citing a specific reason

## TDD
- [ ] Every new behaviour is covered by at least one unit test
- [ ] Tests were written before the implementation (or at minimum: tests fail without the implementation)
- [ ] No test passes trivially (test verifies the acceptance criterion, not just that no exception is thrown)

## Architecture Compliance
- [ ] No new lateral engine-to-engine method calls (ARCH-001 §8)
- [ ] No `<ProjectReference>` from `Ferret.Core` to any other module
- [ ] No plugin references `Ferret.Runtime` directly (plugins → Core only, per STD-005 §11)
- [ ] No domain event raised via direct method call — uses `IEventBus` (ARCH-013)

## Cross-Cutting Concerns (ARCH-012)
- [ ] `IClock` injected and used — no `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`
- [ ] `CancellationToken` accepted as last parameter on every new public async method
- [ ] `CancellationToken` propagated through all async calls within the method
- [ ] New engine types implement `IAsyncDisposable` if they hold managed resources
- [ ] No `Task.Result` or `.GetAwaiter().GetResult()` (synchronous blocking on async)

## STD-005 Compliance
- [ ] One type per file; file name matches type name exactly
- [ ] Namespace matches folder structure
- [ ] Test method names follow `MethodName_StateUnderTest_ExpectedBehaviour` pattern
- [ ] No real `DateTimeOffset.UtcNow` in any test

## NuGet
- [ ] Any new package added to `Directory.Packages.props` only (no `Version=` in `.csproj`)
- [ ] Analyser and test-only packages have `PrivateAssets="all"`

## Documentation
- [ ] All new `public` types in `Ferret.Core` and `Ferret.Sdk` have XML doc comments
- [ ] All new `public` interface members in `Ferret.Core` and `Ferret.Sdk` have XML doc comments
- [ ] No explanatory comment blocks — inline comments cite a ticket or non-obvious constraint only

## Scope
- [ ] Only files declared in the WI scope are modified
- [ ] No unrelated formatting or cleanup in the PR diff
