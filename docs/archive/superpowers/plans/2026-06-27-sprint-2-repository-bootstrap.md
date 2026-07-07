> **Historical note:** This document was written when the product was named AISpace, which was renamed to Ferret during Sprint 5.

# Sprint 2 — Repository Bootstrap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the production-ready solution structure: all source projects, test projects, a sample plugin, build infrastructure, and CI integration — with zero business logic implemented.

**Architecture:** Eight source library projects (plus one executable), nine test projects, and one sample plugin project, all governed by `Directory.Build.props` (already exists), `Directory.Packages.props` (new), `Directory.Build.targets` (new), and `stylecop.json` (new). The dependency graph from ARCH-001 §8 is enforced as MSBuild build-time errors.

**Tech Stack:** .NET 9, C# (latest), xUnit 2.9.2, StyleCop.Analyzers 1.2.0-beta.556, Central Package Management, GitHub Actions (workflow already exists at `.github/workflows/ci.yml`).

## Global Constraints

- `net9.0` target; C# `latest`; `Nullable=enable`; `ImplicitUsings=enable`
- `TreatWarningsAsErrors=true`; `AnalysisLevel=latest`; `EnforceCodeStyleInBuild=true` — already set in `Directory.Build.props`
- `ManagePackageVersionsCentrally=true` — all NuGet versions in `Directory.Packages.props`; individual `.csproj` files omit `Version` attributes (STD-005 §11.2)
- `Ferret.Core` must have zero `<ProjectReference>` elements — enforced by `Directory.Build.targets` target `EnforceCoreDependencyRule`
- All source files use file-scoped namespace declarations (`namespace Foo;`)
- Test class naming: `<Subject>Tests.cs`; test method naming: `MethodName_StateUnderTest_ExpectedBehaviour`
- No business logic, services, engines, parsers, or storage implemented in this sprint
- Every source project contains exactly one placeholder type in one `.cs` file
- Every test project contains exactly one placeholder `[Fact]` that asserts `true`
- ARCH-001 §8 dependency rules: Core→none; Runtime→Core; Sdk→Core; Configuration→Core; Telemetry→Core; Plugins→Core; Mcp→Runtime+Core; Cli→Runtime+Core
- Plugin reference rule: plugins reference only `Ferret.Core` / `Ferret.Sdk` (ARCH-001 §8.3 — enforced in Task 1)
- STD-005 §1 layout: production code in `src/`, tests in `tests/`, samples in `samples/`
- Commit style: conventional commits with work item reference

---

## File Map

**Create:**
- `Directory.Packages.props` — central NuGet version catalogue
- `Directory.Build.targets` — ARCH-001 §8 dependency enforcement targets
- `stylecop.json` — StyleCop Analyser configuration
- `src/Ferret.Core/Ferret.Core.csproj`
- `src/Ferret.Core/CoreModule.cs`
- `src/Ferret.Runtime/Ferret.Runtime.csproj`
- `src/Ferret.Runtime/RuntimeModule.cs`
- `src/Ferret.Sdk/Ferret.Sdk.csproj`
- `src/Ferret.Sdk/SdkModule.cs`
- `src/Ferret.Configuration/Ferret.Configuration.csproj`
- `src/Ferret.Configuration/ConfigurationModule.cs`
- `src/Ferret.Telemetry/Ferret.Telemetry.csproj`
- `src/Ferret.Telemetry/TelemetryModule.cs`
- `src/Ferret.Plugins/Ferret.Plugins.csproj`
- `src/Ferret.Plugins/PluginsModule.cs`
- `src/Ferret.Mcp/Ferret.Mcp.csproj`
- `src/Ferret.Mcp/McpModule.cs`
- `src/Ferret.Cli/Ferret.Cli.csproj`
- `src/Ferret.Cli/Program.cs`
- `tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj`
- `tests/Ferret.Core.Tests/CoreModuleTests.cs`
- `tests/Ferret.Runtime.Tests/Ferret.Runtime.Tests.csproj`
- `tests/Ferret.Runtime.Tests/RuntimeModuleTests.cs`
- `tests/Ferret.Sdk.Tests/Ferret.Sdk.Tests.csproj`
- `tests/Ferret.Sdk.Tests/SdkModuleTests.cs`
- `tests/Ferret.Configuration.Tests/Ferret.Configuration.Tests.csproj`
- `tests/Ferret.Configuration.Tests/ConfigurationModuleTests.cs`
- `tests/Ferret.Telemetry.Tests/Ferret.Telemetry.Tests.csproj`
- `tests/Ferret.Telemetry.Tests/TelemetryModuleTests.cs`
- `tests/Ferret.Plugins.Tests/Ferret.Plugins.Tests.csproj`
- `tests/Ferret.Plugins.Tests/PluginsModuleTests.cs`
- `tests/Ferret.Mcp.Tests/Ferret.Mcp.Tests.csproj`
- `tests/Ferret.Mcp.Tests/McpModuleTests.cs`
- `tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj`
- `tests/Ferret.Cli.Tests/CliModuleTests.cs`
- `tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj`
- `tests/Ferret.Integration.Tests/PlaceholderIntegrationTests.cs`
- `samples/plugins/Ferret.Plugins.Sample/Ferret.Plugins.Sample.csproj`
- `samples/plugins/Ferret.Plugins.Sample/SamplePlugin.cs`
- `samples/plugins/Ferret.Plugins.Sample/plugin.json`
- `samples/plugins/Ferret.Plugins.Sample/README.md`
- `build/Build.ps1`
- `build/Build.sh`

**Modify:**
- `Directory.Build.props` — add StyleCop `PackageReference` `ItemGroup`
- `.editorconfig` — add StyleCop rule suppressions and file-scoped namespace style

**Do not create:** `.github/workflows/ci.yml` (already exists and is production-ready)

---

## Task 1: Central Package Management, Analyser Configuration, and Dependency Enforcement

**Files:**
- Create: `Directory.Packages.props`
- Create: `Directory.Build.targets`
- Create: `stylecop.json`
- Modify: `Directory.Build.props` (add StyleCop `ItemGroup`)
- Modify: `.editorconfig` (add StyleCop suppressions + namespace style)

**Interfaces:**
- Consumes: `Directory.Build.props` (existing properties), `.editorconfig` (existing)
- Produces: Central Package Management active; StyleCop analyser applied to all projects; ARCH-001 §8 dependency enforcement active

- [ ] **Step 1: Create `Directory.Packages.props`**

Create at the repository root (`<repo-root>\Directory.Packages.props`):

```xml
<Project>

  <!--
    Central Package Management — STD-005 §11.2.
    All NuGet versions are declared here. Individual .csproj files declare
    <PackageReference Include="..." /> with no Version attribute.
  -->

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup Label="Analysers — applied to all projects via Directory.Build.props">
    <PackageVersion Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
  </ItemGroup>

  <ItemGroup Label="Test framework">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add StyleCop analyser reference to `Directory.Build.props`**

Append the following `ItemGroup` before the final `</Project>` tag in `Directory.Build.props`:

```xml
  <ItemGroup Label="Analysers">
    <PackageReference Include="StyleCop.Analyzers">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>

```

- [ ] **Step 3: Create `stylecop.json`**

Create at the repository root (`<repo-root>\stylecop.json`):

```json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
  "settings": {
    "documentationRules": {
      "companyName": "Ferret Contributors",
      "documentInterfaces": true,
      "documentExposedElements": true,
      "documentInternalElements": false,
      "documentPrivateElements": false,
      "documentPrivateFields": false
    },
    "orderingRules": {
      "usingDirectivesPlacement": "outsideNamespace"
    }
  }
}
```

- [ ] **Step 4: Add StyleCop suppressions and namespace style to `.editorconfig`**

Append the following block at the end of `.editorconfig` (after the last existing section):

```ini
# ---- StyleCop Analyzers --------------------------------------------------------
# SA1633: File must have file header — not used in this project
dotnet_diagnostic.SA1633.severity = none
# SA1101: Prefix local calls with this — modern C# style omits this
dotnet_diagnostic.SA1101.severity = none
# SA1309: Field names must not begin with underscore — project uses _field convention
dotnet_diagnostic.SA1309.severity = none

# Test and sample files — relax documentation requirement
[{tests,samples}/**/*.cs]
dotnet_diagnostic.SA1600.severity = none

# ---- C# namespace declaration style --------------------------------------------
[*.cs]
csharp_style_namespace_declarations = file_scoped : warning
```

- [ ] **Step 5: Create `Directory.Build.targets`**

Create at the repository root (`<repo-root>\Directory.Build.targets`):

```xml
<Project>

  <!--
    Enforce ARCH-001 §8 dependency rules as build-time errors.
    These targets run before project references are resolved, ensuring
    violations fail the build immediately with a descriptive error code.
  -->

  <!-- ARCH001: Ferret.Core must have zero ProjectReference elements -->
  <Target Name="EnforceCoreDependencyRule"
          BeforeTargets="ResolveProjectReferences"
          Condition="'$(MSBuildProjectName)' == 'Ferret.Core'">
    <Error Condition="@(ProjectReference->Count()) &gt; 0"
           Code="ARCH001"
           Text="ARCH-001 §8: Ferret.Core must have zero project references. Found: @(ProjectReference->'%(Filename)')" />
  </Target>

  <!-- ARCH002: Ferret.Runtime must not reference Ferret.Cli or Ferret.Mcp -->
  <Target Name="EnforceRuntimeForbiddenReferences"
          BeforeTargets="ResolveProjectReferences"
          Condition="'$(MSBuildProjectName)' == 'Ferret.Runtime'">
    <ItemGroup>
      <_RuntimeForbiddenRef Include="@(ProjectReference)"
                            Condition="'%(Filename)' == 'Ferret.Cli' Or '%(Filename)' == 'Ferret.Mcp'" />
    </ItemGroup>
    <Error Condition="@(_RuntimeForbiddenRef->Count()) &gt; 0"
           Code="ARCH002"
           Text="ARCH-001 §8: Ferret.Runtime must not reference Ferret.Cli or Ferret.Mcp. Found: @(_RuntimeForbiddenRef->'%(Filename)')" />
  </Target>

</Project>
```

- [ ] **Step 6: Verify restore succeeds**

Run: `dotnet restore src/Ferret.sln`

Expected: `Restore complete.` with no errors. (The solution currently has no projects; restore completes trivially — that's the expected baseline before projects are added.)

- [ ] **Step 7: Commit Task 1**

```bash
git add Directory.Packages.props Directory.Build.props Directory.Build.targets stylecop.json .editorconfig
git commit -m "build(sprint-2): add central package management, StyleCop analyser, ARCH dependency enforcement targets"
```

---

## Task 2: Ferret.Core — Foundation Project

**Files:**
- Create: `src/Ferret.Core/Ferret.Core.csproj`
- Create: `src/Ferret.Core/CoreModule.cs`
- Modify: `src/Ferret.sln` (via `dotnet sln add`)

**Interfaces:**
- Consumes: `Directory.Build.props` (all properties inherited), `Directory.Build.targets` (`EnforceCoreDependencyRule` target)
- Produces: `Ferret.Core` assembly — the dependency-free foundation that all other projects reference

- [ ] **Step 1: Confirm the project does not yet exist**

Run: `dotnet build src/Ferret.Core/Ferret.Core.csproj`

Expected: Error — `MSBUILD : error MSB1009: Project file does not exist.`

This is the red state.

- [ ] **Step 2: Create the project directory**

```bash
mkdir -p src/Ferret.Core
```

- [ ] **Step 3: Create `src/Ferret.Core/Ferret.Core.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Core</AssemblyName>
    <RootNamespace>Ferret.Core</RootNamespace>
  </PropertyGroup>

  <!-- Zero project references — enforced at build time by Directory.Build.targets ARCH001 target -->

</Project>
```

- [ ] **Step 4: Create `src/Ferret.Core/CoreModule.cs`**

```csharp
namespace Ferret.Core;

internal static class CoreModule
{
}
```

- [ ] **Step 5: Add to solution**

```bash
dotnet sln src/Ferret.sln add src/Ferret.Core/Ferret.Core.csproj
```

Expected output: `Project 'src/Ferret.Core/Ferret.Core.csproj' added to the solution.`

- [ ] **Step 6: Build and verify green**

Run: `dotnet build src/Ferret.Core/Ferret.Core.csproj --configuration Release`

Expected: `Build succeeded.` with `0 Warning(s)` and `0 Error(s)`.

- [ ] **Step 7: Verify dependency enforcement fires on a deliberate violation**

Temporarily add a reference to `src/Ferret.Core/Ferret.Core.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Ferret.Runtime\Ferret.Runtime.csproj" />
</ItemGroup>
```

Run: `dotnet build src/Ferret.Core/Ferret.Core.csproj`

Expected: Error `ARCH001` — "Ferret.Core must have zero project references."

Remove the reference. Build again to confirm it passes.

- [ ] **Step 8: Commit Task 2**

```bash
git add src/Ferret.Core/ src/Ferret.sln
git commit -m "feat(sprint-2): scaffold Ferret.Core — dependency-free foundation project"
```

---

## Task 3: Core-Dependent Library Projects

**Files:**
- Create: `src/Ferret.Runtime/Ferret.Runtime.csproj` + `RuntimeModule.cs`
- Create: `src/Ferret.Sdk/Ferret.Sdk.csproj` + `SdkModule.cs`
- Create: `src/Ferret.Configuration/Ferret.Configuration.csproj` + `ConfigurationModule.cs`
- Create: `src/Ferret.Telemetry/Ferret.Telemetry.csproj` + `TelemetryModule.cs`
- Create: `src/Ferret.Plugins/Ferret.Plugins.csproj` + `PluginsModule.cs`
- Modify: `src/Ferret.sln` (five `dotnet sln add` calls)

**Interfaces:**
- Consumes: `Ferret.Core` project reference (all five)
- Produces: Five compiled assemblies; each references `Ferret.Core` only — satisfies ARCH-001 §8 layer rules

- [ ] **Step 1: Create `src/Ferret.Runtime/`**

```bash
mkdir -p src/Ferret.Runtime src/Ferret.Sdk src/Ferret.Configuration src/Ferret.Telemetry src/Ferret.Plugins
```

- [ ] **Step 2: Create `src/Ferret.Runtime/Ferret.Runtime.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Runtime</AssemblyName>
    <RootNamespace>Ferret.Runtime</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create `src/Ferret.Runtime/RuntimeModule.cs`**

```csharp
namespace Ferret.Runtime;

internal static class RuntimeModule
{
}
```

- [ ] **Step 4: Create `src/Ferret.Sdk/Ferret.Sdk.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Sdk</AssemblyName>
    <RootNamespace>Ferret.Sdk</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Create `src/Ferret.Sdk/SdkModule.cs`**

```csharp
namespace Ferret.Sdk;

internal static class SdkModule
{
}
```

- [ ] **Step 6: Create `src/Ferret.Configuration/Ferret.Configuration.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Configuration</AssemblyName>
    <RootNamespace>Ferret.Configuration</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 7: Create `src/Ferret.Configuration/ConfigurationModule.cs`**

```csharp
namespace Ferret.Configuration;

internal static class ConfigurationModule
{
}
```

- [ ] **Step 8: Create `src/Ferret.Telemetry/Ferret.Telemetry.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Telemetry</AssemblyName>
    <RootNamespace>Ferret.Telemetry</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 9: Create `src/Ferret.Telemetry/TelemetryModule.cs`**

```csharp
namespace Ferret.Telemetry;

internal static class TelemetryModule
{
}
```

- [ ] **Step 10: Create `src/Ferret.Plugins/Ferret.Plugins.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Plugins</AssemblyName>
    <RootNamespace>Ferret.Plugins</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 11: Create `src/Ferret.Plugins/PluginsModule.cs`**

```csharp
namespace Ferret.Plugins;

internal static class PluginsModule
{
}
```

- [ ] **Step 12: Add all five projects to solution**

```bash
dotnet sln src/Ferret.sln add src/Ferret.Runtime/Ferret.Runtime.csproj
dotnet sln src/Ferret.sln add src/Ferret.Sdk/Ferret.Sdk.csproj
dotnet sln src/Ferret.sln add src/Ferret.Configuration/Ferret.Configuration.csproj
dotnet sln src/Ferret.sln add src/Ferret.Telemetry/Ferret.Telemetry.csproj
dotnet sln src/Ferret.sln add src/Ferret.Plugins/Ferret.Plugins.csproj
```

Expected: Five lines each reading `Project '...' added to the solution.`

- [ ] **Step 13: Build all five projects via solution**

Run: `dotnet build src/Ferret.sln --configuration Release`

Expected: `Build succeeded.` — `0 Warning(s)`, `0 Error(s)`, six projects built (Core + five new).

- [ ] **Step 14: Commit Task 3**

```bash
git add src/Ferret.Runtime/ src/Ferret.Sdk/ src/Ferret.Configuration/ src/Ferret.Telemetry/ src/Ferret.Plugins/ src/Ferret.sln
git commit -m "feat(sprint-2): scaffold infrastructure layer — Runtime, Sdk, Configuration, Telemetry, Plugins"
```

---

## Task 4: Presentation Projects — Mcp and Cli

**Files:**
- Create: `src/Ferret.Mcp/Ferret.Mcp.csproj` + `McpModule.cs`
- Create: `src/Ferret.Cli/Ferret.Cli.csproj` + `Program.cs`
- Modify: `src/Ferret.sln`

**Interfaces:**
- Consumes: `Ferret.Runtime` and `Ferret.Core` project references
- Produces: `Ferret.Mcp` (library) and `Ferret.Cli` (executable); eight total source projects in solution

- [ ] **Step 1: Create directories**

```bash
mkdir -p src/Ferret.Mcp src/Ferret.Cli
```

- [ ] **Step 2: Create `src/Ferret.Mcp/Ferret.Mcp.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Mcp</AssemblyName>
    <RootNamespace>Ferret.Mcp</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Runtime\Ferret.Runtime.csproj" />
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create `src/Ferret.Mcp/McpModule.cs`**

```csharp
namespace Ferret.Mcp;

internal static class McpModule
{
}
```

- [ ] **Step 4: Create `src/Ferret.Cli/Ferret.Cli.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Cli</AssemblyName>
    <RootNamespace>Ferret.Cli</RootNamespace>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Runtime\Ferret.Runtime.csproj" />
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Create `src/Ferret.Cli/Program.cs`**

```csharp
// Ferret CLI entry point — full implementation in Sprint 9 (ARCH-009).
return 0;
```

- [ ] **Step 6: Add both projects to solution**

```bash
dotnet sln src/Ferret.sln add src/Ferret.Mcp/Ferret.Mcp.csproj
dotnet sln src/Ferret.sln add src/Ferret.Cli/Ferret.Cli.csproj
```

- [ ] **Step 7: Verify ARCH002 enforcement: Runtime must not reference Cli**

Temporarily add to `src/Ferret.Runtime/Ferret.Runtime.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Ferret.Cli\Ferret.Cli.csproj" />
</ItemGroup>
```

Run: `dotnet build src/Ferret.Runtime/Ferret.Runtime.csproj`

Expected: Error `ARCH002` — "Ferret.Runtime must not reference Ferret.Cli or Ferret.Mcp."

Remove the reference.

- [ ] **Step 8: Build complete solution**

Run: `dotnet build src/Ferret.sln --configuration Release`

Expected: `Build succeeded.` — `0 Warning(s)`, `0 Error(s)`, eight projects built.

- [ ] **Step 9: Commit Task 4**

```bash
git add src/Ferret.Mcp/ src/Ferret.Cli/ src/Ferret.sln
git commit -m "feat(sprint-2): scaffold presentation layer — Mcp library, Cli executable"
```

---

## Task 5: Unit Test Projects

**Files:**
- Create: `tests/Ferret.Core.Tests/` (csproj + CoreModuleTests.cs)
- Create: `tests/Ferret.Runtime.Tests/` (csproj + RuntimeModuleTests.cs)
- Create: `tests/Ferret.Sdk.Tests/` (csproj + SdkModuleTests.cs)
- Create: `tests/Ferret.Configuration.Tests/` (csproj + ConfigurationModuleTests.cs)
- Create: `tests/Ferret.Telemetry.Tests/` (csproj + TelemetryModuleTests.cs)
- Create: `tests/Ferret.Plugins.Tests/` (csproj + PluginsModuleTests.cs)
- Create: `tests/Ferret.Mcp.Tests/` (csproj + McpModuleTests.cs)
- Create: `tests/Ferret.Cli.Tests/` (csproj + CliModuleTests.cs)
- Modify: `src/Ferret.sln`

**Interfaces:**
- Consumes: Each unit test project references its paired source project
- Produces: Eight test assemblies; `dotnet test src/Ferret.sln` runs and passes all 8 placeholder tests

The test project template repeats for each source project. The pattern is shown in full for `Ferret.Core.Tests` and abbreviated for the remaining seven (which follow the same pattern with the project name substituted).

- [ ] **Step 1: Create test directories**

```bash
mkdir -p tests/Ferret.Core.Tests tests/Ferret.Runtime.Tests tests/Ferret.Sdk.Tests tests/Ferret.Configuration.Tests tests/Ferret.Telemetry.Tests tests/Ferret.Plugins.Tests tests/Ferret.Mcp.Tests tests/Ferret.Cli.Tests
```

- [ ] **Step 2: Write the failing test for Ferret.Core.Tests (red)**

Run: `dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj`

Expected: Error — project file does not exist. This is the red state.

- [ ] **Step 3: Create `tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Core.Tests</AssemblyName>
    <RootNamespace>Ferret.Core.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Create `tests/Ferret.Core.Tests/CoreModuleTests.cs`**

```csharp
namespace Ferret.Core.Tests;

public sealed class CoreModuleTests
{
    [Fact]
    public void Placeholder_ScaffoldVerification_Passes()
    {
        // Sprint 2: Verifies Core project compiles and tests execute.
        // Replace with meaningful assertions in Sprint 3.
        Assert.True(true);
    }
}
```

- [ ] **Step 5: Run CoreModuleTests (green)**

```bash
dotnet add tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj reference src/Ferret.Core/Ferret.Core.csproj  # already in csproj
dotnet test tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
```

Expected: `1 Test(s) Passed, 0 Failed, 0 Skipped.`

- [ ] **Step 6: Create `tests/Ferret.Runtime.Tests/Ferret.Runtime.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Runtime.Tests</AssemblyName>
    <RootNamespace>Ferret.Runtime.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Runtime\Ferret.Runtime.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 7: Create `tests/Ferret.Runtime.Tests/RuntimeModuleTests.cs`**

```csharp
namespace Ferret.Runtime.Tests;

public sealed class RuntimeModuleTests
{
    [Fact]
    public void Placeholder_ScaffoldVerification_Passes()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 8: Create `tests/Ferret.Sdk.Tests/Ferret.Sdk.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Sdk.Tests</AssemblyName>
    <RootNamespace>Ferret.Sdk.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Sdk\Ferret.Sdk.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 9: Create `tests/Ferret.Sdk.Tests/SdkModuleTests.cs`**

```csharp
namespace Ferret.Sdk.Tests;

public sealed class SdkModuleTests
{
    [Fact]
    public void Placeholder_ScaffoldVerification_Passes()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 10: Create `tests/Ferret.Configuration.Tests/Ferret.Configuration.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Configuration.Tests</AssemblyName>
    <RootNamespace>Ferret.Configuration.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Configuration\Ferret.Configuration.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 11: Create `tests/Ferret.Configuration.Tests/ConfigurationModuleTests.cs`**

```csharp
namespace Ferret.Configuration.Tests;

public sealed class ConfigurationModuleTests
{
    [Fact]
    public void Placeholder_ScaffoldVerification_Passes()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 12: Create `tests/Ferret.Telemetry.Tests/Ferret.Telemetry.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Telemetry.Tests</AssemblyName>
    <RootNamespace>Ferret.Telemetry.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Telemetry\Ferret.Telemetry.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 13: Create `tests/Ferret.Telemetry.Tests/TelemetryModuleTests.cs`**

```csharp
namespace Ferret.Telemetry.Tests;

public sealed class TelemetryModuleTests
{
    [Fact]
    public void Placeholder_ScaffoldVerification_Passes()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 14: Create `tests/Ferret.Plugins.Tests/Ferret.Plugins.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Plugins.Tests</AssemblyName>
    <RootNamespace>Ferret.Plugins.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Plugins\Ferret.Plugins.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 15: Create `tests/Ferret.Plugins.Tests/PluginsModuleTests.cs`**

```csharp
namespace Ferret.Plugins.Tests;

public sealed class PluginsModuleTests
{
    [Fact]
    public void Placeholder_ScaffoldVerification_Passes()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 16: Create `tests/Ferret.Mcp.Tests/Ferret.Mcp.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Mcp.Tests</AssemblyName>
    <RootNamespace>Ferret.Mcp.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Mcp\Ferret.Mcp.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 17: Create `tests/Ferret.Mcp.Tests/McpModuleTests.cs`**

```csharp
namespace Ferret.Mcp.Tests;

public sealed class McpModuleTests
{
    [Fact]
    public void Placeholder_ScaffoldVerification_Passes()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 18: Create `tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Cli.Tests</AssemblyName>
    <RootNamespace>Ferret.Cli.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Cli\Ferret.Cli.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 19: Create `tests/Ferret.Cli.Tests/CliModuleTests.cs`**

```csharp
namespace Ferret.Cli.Tests;

public sealed class CliModuleTests
{
    [Fact]
    public void Placeholder_ScaffoldVerification_Passes()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 20: Add all eight unit test projects to solution**

```bash
dotnet sln src/Ferret.sln add tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Runtime.Tests/Ferret.Runtime.Tests.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Sdk.Tests/Ferret.Sdk.Tests.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Configuration.Tests/Ferret.Configuration.Tests.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Telemetry.Tests/Ferret.Telemetry.Tests.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Plugins.Tests/Ferret.Plugins.Tests.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Mcp.Tests/Ferret.Mcp.Tests.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj
```

- [ ] **Step 21: Run all unit tests via solution (green)**

Run: `dotnet test src/Ferret.sln --configuration Release --verbosity normal`

Expected: `8 Test(s) Passed, 0 Failed, 0 Skipped` across 8 test projects.

- [ ] **Step 22: Commit Task 5**

```bash
git add tests/ src/Ferret.sln
git commit -m "test(sprint-2): scaffold eight unit test projects with placeholder [Fact] tests"
```

---

## Task 6: Integration Test Project

**Files:**
- Create: `tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj`
- Create: `tests/Ferret.Integration.Tests/PlaceholderIntegrationTests.cs`
- Modify: `src/Ferret.sln`

**Interfaces:**
- Consumes: `Ferret.Runtime` and `Ferret.Core` project references
- Produces: Integration test assembly registered in the solution; `dotnet test` reports 1 passing integration test

- [ ] **Step 1: Create directory**

```bash
mkdir -p tests/Ferret.Integration.Tests
```

- [ ] **Step 2: Create `tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Integration.Tests</AssemblyName>
    <RootNamespace>Ferret.Integration.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Runtime\Ferret.Runtime.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create `tests/Ferret.Integration.Tests/PlaceholderIntegrationTests.cs`**

```csharp
namespace Ferret.Integration.Tests;

public sealed class PlaceholderIntegrationTests
{
    [Fact]
    public void Placeholder_ScaffoldVerification_Passes()
    {
        // Sprint 2: Verifies integration test project compiles and executes.
        // Replace with real integration scenarios in Sprint 3+.
        Assert.True(true);
    }
}
```

- [ ] **Step 4: Add to solution**

```bash
dotnet sln src/Ferret.sln add tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj
```

- [ ] **Step 5: Run integration tests**

Run: `dotnet test tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj`

Expected: `1 Test(s) Passed, 0 Failed, 0 Skipped.`

- [ ] **Step 6: Commit Task 6**

```bash
git add tests/Ferret.Integration.Tests/ src/Ferret.sln
git commit -m "test(sprint-2): scaffold Ferret.Integration.Tests — cross-module test project"
```

---

## Task 7: Sample Plugin Project

**Files:**
- Create: `samples/plugins/Ferret.Plugins.Sample/Ferret.Plugins.Sample.csproj`
- Create: `samples/plugins/Ferret.Plugins.Sample/SamplePlugin.cs`
- Create: `samples/plugins/Ferret.Plugins.Sample/plugin.json`
- Create: `samples/plugins/Ferret.Plugins.Sample/README.md`

**Interfaces:**
- Consumes: `Ferret.Sdk` project reference (demonstrates the plugin authoring model)
- Produces: A compilable sample plugin project demonstrating manifest structure and plugin layout (STD-005 §8)
- Note: This project is NOT added to `src/Ferret.sln` (samples are excluded per STD-005 §8 — they build standalone or via a separate samples CI job)

- [ ] **Step 1: Create directory**

```bash
mkdir -p samples/plugins/Ferret.Plugins.Sample
```

- [ ] **Step 2: Create `samples/plugins/Ferret.Plugins.Sample/Ferret.Plugins.Sample.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Plugins.Sample</AssemblyName>
    <RootNamespace>Ferret.Plugins.Sample</RootNamespace>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\src\Ferret.Sdk\Ferret.Sdk.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create `samples/plugins/Ferret.Plugins.Sample/SamplePlugin.cs`**

```csharp
namespace Ferret.Plugins.Sample;

// Demonstrates the plugin entry point pattern.
// In Sprint 6+, this class will implement IPlugin from Ferret.Sdk
// once the Core interfaces are defined (Sprint 3, ARCH-007).
internal sealed class SamplePlugin
{
}
```

- [ ] **Step 4: Create `samples/plugins/Ferret.Plugins.Sample/plugin.json`**

```json
{
  "$schema": "https://Ferret.dev/schemas/plugin/1.0.json",
  "id": "Ferret.sample.minimal",
  "version": "1.0.0",
  "compatibility": {
    "min": "1.0",
    "max": "1.*"
  },
  "entryPoint": "Ferret.Plugins.Sample.SamplePlugin, Ferret.Plugins.Sample",
  "permissions": [],
  "interfaces": [],
  "dependencies": []
}
```

- [ ] **Step 5: Create `samples/plugins/Ferret.Plugins.Sample/README.md`**

```markdown
# Ferret.Plugins.Sample

Minimal sample plugin demonstrating the Ferret plugin project structure.

## What this shows

- Plugin project layout per STD-005 §5.2
- `plugin.json` manifest structure (ARCH-001 §11.6)
- Plugin entry point convention (Sprint 6: ARCH-007 implementation)
- Reference to `Ferret.Sdk` as the only platform package

## Status

Sprint 2 scaffold. The `SamplePlugin` class will implement `IPlugin` from `Ferret.Sdk`
once the Core interfaces are defined in Sprint 3.

## Building

```bash
dotnet build samples/plugins/Ferret.Plugins.Sample/Ferret.Plugins.Sample.csproj
```
```

- [ ] **Step 6: Build the sample plugin standalone**

Run: `dotnet build samples/plugins/Ferret.Plugins.Sample/Ferret.Plugins.Sample.csproj --configuration Release`

Expected: `Build succeeded.` — `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 7: Commit Task 7**

```bash
git add samples/
git commit -m "feat(sprint-2): add sample plugin project — demonstrates plugin layout and plugin.json manifest"
```

---

## Task 8: Build Scripts

**Files:**
- Create: `build/Build.ps1`
- Create: `build/Build.sh`

Note: The CI workflow is already production-ready at `.github/workflows/ci.yml`. These scripts are the developer-local equivalents.

**Interfaces:**
- Consumes: `src/Ferret.sln`
- Produces: Cross-platform build scripts for local developer use

- [ ] **Step 1: Create `build/` directory**

```bash
mkdir -p build
```

- [ ] **Step 2: Create `build/Build.ps1`**

```powershell
#!/usr/bin/env pwsh
[CmdletBinding()]
param (
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$Test,
    [switch]$Clean,
    [switch]$Format
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot
$sln = Join-Path $repoRoot 'src' 'Ferret.sln'

if ($Clean) {
    Write-Host 'Cleaning...' -ForegroundColor Cyan
    & dotnet clean $sln --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "Building Ferret ($Configuration)..." -ForegroundColor Cyan
& dotnet build $sln --configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($Format) {
    Write-Host 'Checking format...' -ForegroundColor Cyan
    & dotnet format $sln --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($Test) {
    Write-Host 'Running tests...' -ForegroundColor Cyan
    & dotnet test $sln --no-build --configuration $Configuration --verbosity normal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host 'Done.' -ForegroundColor Green
```

- [ ] **Step 3: Create `build/Build.sh`**

```bash
#!/usr/bin/env bash
# build/Build.sh — local build and test script for Linux/macOS.
# Usage: ./build/Build.sh [Debug|Release] [--test] [--clean] [--format]

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SLN="$REPO_ROOT/src/Ferret.sln"
CONFIGURATION="${1:-Debug}"
shift || true

RUN_TEST=false
RUN_CLEAN=false
RUN_FORMAT=false

for arg in "$@"; do
    case "$arg" in
        --test)   RUN_TEST=true ;;
        --clean)  RUN_CLEAN=true ;;
        --format) RUN_FORMAT=true ;;
    esac
done

if $RUN_CLEAN; then
    echo "Cleaning ($CONFIGURATION)..."
    dotnet clean "$SLN" --configuration "$CONFIGURATION"
fi

echo "Building Ferret ($CONFIGURATION)..."
dotnet build "$SLN" --configuration "$CONFIGURATION"

if $RUN_FORMAT; then
    echo "Checking format..."
    dotnet format "$SLN" --verify-no-changes --no-restore
fi

if $RUN_TEST; then
    echo "Running tests..."
    dotnet test "$SLN" --no-build --configuration "$CONFIGURATION" --verbosity normal
fi

echo "Done."
```

- [ ] **Step 4: Make Build.sh executable (Linux/macOS)**

```bash
chmod +x build/Build.sh
```

(Skip on Windows — the `+x` bit is irrelevant there.)

- [ ] **Step 5: Verify Build.ps1 runs**

Run: `pwsh build/Build.ps1 -Configuration Release -Test`

Expected: Build succeeds, 9 tests pass.

- [ ] **Step 6: Commit Task 8**

```bash
git add build/
git commit -m "build(sprint-2): add Build.ps1 and Build.sh developer build scripts"
```

---

## Task 9: Full Solution Validation

**Files:** No new files — validation only.

**Interfaces:**
- Validates: All 17 projects in solution, zero warnings, 9 passing tests, format compliance, dependency rules

- [ ] **Step 1: Full solution build — Release**

Run: `dotnet build src/Ferret.sln --configuration Release`

Expected:
- 17 projects built (8 source + 9 test)
- `0 Warning(s)`, `0 Error(s)`
- `Build succeeded.`

If any warnings appear, fix them before continuing. Do not suppress warnings; fix the root cause.

- [ ] **Step 2: Full test run**

Run: `dotnet test src/Ferret.sln --configuration Release --verbosity normal`

Expected:
- 9 Test(s) Passed (one per test project)
- `0 Failed, 0 Skipped`
- All test assemblies: `Ferret.Core.Tests`, `Ferret.Runtime.Tests`, `Ferret.Sdk.Tests`, `Ferret.Configuration.Tests`, `Ferret.Telemetry.Tests`, `Ferret.Plugins.Tests`, `Ferret.Mcp.Tests`, `Ferret.Cli.Tests`, `Ferret.Integration.Tests`

- [ ] **Step 3: Format check**

Run: `dotnet format src/Ferret.sln --verify-no-changes --no-restore`

Expected: `Format complete — no files changed.`

If files are changed, the step will fail. Review the diff; apply the format changes; re-run the build and tests.

- [ ] **Step 4: Validate dependency rules enforcement**

Confirm the `ARCH001` target fires: temporarily add to `src/Ferret.Core/Ferret.Core.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Ferret.Runtime\Ferret.Runtime.csproj" />
</ItemGroup>
```

Run: `dotnet build src/Ferret.Core/Ferret.Core.csproj`

Expected: Error `ARCH001`.

Revert the change. Confirm build passes.

Confirm the `ARCH002` target fires: temporarily add to `src/Ferret.Runtime/Ferret.Runtime.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Ferret.Cli\Ferret.Cli.csproj" />
</ItemGroup>
```

Run: `dotnet build src/Ferret.Runtime/Ferret.Runtime.csproj`

Expected: Error `ARCH002`.

Revert the change. Confirm build passes.

- [ ] **Step 5: Verify project count in solution**

Run: `dotnet sln src/Ferret.sln list`

Expected output: 17 lines, listing:
```
src/Ferret.Core/Ferret.Core.csproj
src/Ferret.Runtime/Ferret.Runtime.csproj
src/Ferret.Sdk/Ferret.Sdk.csproj
src/Ferret.Configuration/Ferret.Configuration.csproj
src/Ferret.Telemetry/Ferret.Telemetry.csproj
src/Ferret.Plugins/Ferret.Plugins.csproj
src/Ferret.Mcp/Ferret.Mcp.csproj
src/Ferret.Cli/Ferret.Cli.csproj
tests/Ferret.Core.Tests/Ferret.Core.Tests.csproj
tests/Ferret.Runtime.Tests/Ferret.Runtime.Tests.csproj
tests/Ferret.Sdk.Tests/Ferret.Sdk.Tests.csproj
tests/Ferret.Configuration.Tests/Ferret.Configuration.Tests.csproj
tests/Ferret.Telemetry.Tests/Ferret.Telemetry.Tests.csproj
tests/Ferret.Plugins.Tests/Ferret.Plugins.Tests.csproj
tests/Ferret.Mcp.Tests/Ferret.Mcp.Tests.csproj
tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj
tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj
```

- [ ] **Step 6: Verify naming conventions**

Run:

```bash
# Verify project names follow STD-005 §2.1: Ferret.<Module>
dotnet sln src/Ferret.sln list | grep -v "^Ferret\." | grep -v "^tests/" | grep -v "^src/"
```

Expected: No output (all src/ projects begin with `Ferret.`).

```bash
# Verify test projects follow STD-005 §2.1: <SourceProject>.Tests
dotnet sln src/Ferret.sln list | grep "tests/" | grep -v "\.Tests\.csproj$"
```

Expected: No output (all test projects end with `.Tests.csproj`).

- [ ] **Step 7: Verify sample plugin builds independently**

Run: `dotnet build samples/plugins/Ferret.Plugins.Sample/Ferret.Plugins.Sample.csproj --configuration Release`

Expected: `Build succeeded.` — `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 8: Final commit**

```bash
git add src/Ferret.sln
git commit -m "build(sprint-2): validation complete — 17 projects, 9 tests passing, zero warnings"
```

---

## Task 10: Update Workspace State and Generate Completion Report

**Files:**
- Modify: `.ai/session.md`
- Modify: `.ai/current-context.json`

**Interfaces:**
- Consumes: Completed Tasks 1–9
- Produces: Updated session state; completion report for the sprint

- [ ] **Step 1: Update `.ai/session.md`**

Replace the content of `.ai/session.md` with:

```markdown
# AI Session Context

> This file tracks the current working session. Updated at the start and end of each AI-assisted session.
> Keep under 2 KB. Overwrite — do not append.

---

## Current Session

**Date:** 2026-06-27
**Sprint:** 2
**Task:** Sprint 2 — Repository Bootstrap (complete)
**Status:** Complete

## Active Work

_None — Sprint 2 delivered and committed._

## Recently Completed

- Sprint 2 — Repository Bootstrap: 8 source projects, 9 test projects, 1 sample plugin, build infrastructure

## Decisions Made This Session

- Central Package Management activated (`Directory.Packages.props`)
- StyleCop.Analyzers 1.2.0-beta.556 applied to all projects via `Directory.Build.props`
- ARCH-001 §8 dependency rules enforced as MSBuild build-time errors in `Directory.Build.targets`
- All source files use C# file-scoped namespace declarations
- Sample plugin at `samples/plugins/` is NOT in `src/Ferret.sln` (STD-005 §8)
- `src/Ferret.Cli` uses `OutputType=Exe` with a minimal top-level `return 0;` entry point

## Next Steps

- Sprint 3: Define `Ferret.Core` interfaces, value objects, and domain events (ARCH-002)
- Sprint 3: Write first meaningful unit tests against Core contracts

## Blockers

_None_
```

- [ ] **Step 2: Update `.ai/current-context.json`**

Replace with:

```json
{
  "$schema": "https://Ferret.dev/schemas/context/1.0.0",
  "updated": "2026-06-27T00:00:00Z",
  "sprint": 2,
  "task": "Sprint 2 — Repository Bootstrap (complete)",
  "activeFiles": [
    "src/Ferret.sln",
    "Directory.Build.props",
    "Directory.Packages.props",
    "Directory.Build.targets",
    "stylecop.json",
    ".editorconfig"
  ],
  "recentDecisions": [
    {
      "id": "d-003",
      "date": "2026-06-27",
      "summary": "Central Package Management activated; all NuGet versions in Directory.Packages.props",
      "adr": null
    },
    {
      "id": "d-004",
      "date": "2026-06-27",
      "summary": "ARCH-001 §8 dependency rules enforced as MSBuild targets in Directory.Build.targets",
      "adr": null
    },
    {
      "id": "d-005",
      "date": "2026-06-27",
      "summary": "Sample plugin excluded from src/Ferret.sln per STD-005 §8 — builds standalone",
      "adr": null
    }
  ],
  "openQuestions": [],
  "pendingADRs": [],
  "workingSet": {
    "maxEntries": 100,
    "entries": [
      "src/Ferret.Core/Ferret.Core.csproj",
      "src/Ferret.Runtime/Ferret.Runtime.csproj",
      "src/Ferret.Sdk/Ferret.Sdk.csproj",
      "src/Ferret.Configuration/Ferret.Configuration.csproj",
      "src/Ferret.Telemetry/Ferret.Telemetry.csproj",
      "src/Ferret.Plugins/Ferret.Plugins.csproj",
      "src/Ferret.Mcp/Ferret.Mcp.csproj",
      "src/Ferret.Cli/Ferret.Cli.csproj"
    ]
  },
  "knowledgeRefs": []
}
```

- [ ] **Step 3: Commit workspace update**

```bash
git add .ai/session.md .ai/current-context.json
git commit -m "chore(sprint-2): update workspace state — sprint complete"
```

- [ ] **Step 4: Generate completion report**

Write the following to the terminal as the sprint completion report:

```
=== Sprint 2 — Repository Bootstrap: Completion Report ===

Deliverables:
  [x] Ferret.sln — 17 projects (8 source + 9 test)
  [x] Ferret.Core          — foundation project, zero dependencies, ARCH001 enforced
  [x] Ferret.Runtime       — domain layer, references Core
  [x] Ferret.Sdk           — plugin SDK, references Core
  [x] Ferret.Configuration — infrastructure, references Core
  [x] Ferret.Telemetry     — infrastructure, references Core
  [x] Ferret.Plugins       — plugin host, references Core
  [x] Ferret.Mcp           — MCP server/client, references Runtime + Core
  [x] Ferret.Cli           — entry point executable, references Runtime + Core
  [x] 8 unit test projects  — one [Fact] each, all passing
  [x] Ferret.Integration.Tests — placeholder integration test, passing
  [x] Sample plugin          — samples/plugins/Ferret.Plugins.Sample/, builds standalone
  [x] Directory.Build.props  — updated (StyleCop analyser added)
  [x] Directory.Packages.props — central package management active
  [x] Directory.Build.targets — ARCH001 + ARCH002 dependency enforcement
  [x] .editorconfig          — StyleCop suppressions + file-scoped namespace style
  [x] stylecop.json          — StyleCop documentation and ordering rules
  [x] build/Build.ps1        — Windows developer build script
  [x] build/Build.sh         — Linux/macOS developer build script

Validation:
  [x] dotnet build src/Ferret.sln --configuration Release  → 0 warnings, 0 errors
  [x] dotnet test src/Ferret.sln                           → 9 passed, 0 failed
  [x] dotnet format --verify-no-changes                     → no changes needed
  [x] ARCH001 enforcement verified (Core with ref → error)
  [x] ARCH002 enforcement verified (Runtime → Cli → error)
  [x] Naming conventions verified (STD-005 §2.1)
  [x] Sample plugin builds standalone

Not implemented (deferred to Sprint 3+):
  - Ferret.Core interfaces, value objects, domain events (Sprint 3, ARCH-002)
  - Ferret.Runtime engine implementations (Sprint 4–8)
  - Plugin Host loading and isolation (Sprint 6, ARCH-007)
  - CLI command handling (Sprint 9, ARCH-009)
  - MCP transport and tool handlers (Sprint 11, ARCH-010)

Reviewer workflow: execute .ai/workflows/CodeReview.md on this sprint's output.
```

---

## Task 11: Reviewer Workflow

- [ ] **Step 1: Execute the Reviewer workflow**

Per `.ai/workflows/CodeReview.md`, execute a code review of the Sprint 2 output. The review scope is all files created or modified in Tasks 1–10.

The Reviewer should verify:
1. All projects compile with zero warnings (`dotnet build`)
2. All 9 tests pass (`dotnet test`)
3. `Directory.Build.targets` enforcement targets fire correctly on deliberate violations
4. Central Package Management is correctly configured (no `Version` attributes on `<PackageReference>` outside `Directory.Packages.props`)
5. No business logic, services, engines, parsers, or storage is implemented
6. Naming conventions match STD-005 §2.1 for all projects
7. All source files use file-scoped namespace declarations
8. `Ferret.Core` has zero project references in its `.csproj`
9. `Ferret.Cli` uses `OutputType=Exe`
10. Sample plugin is excluded from `src/Ferret.sln`

- [ ] **Step 2: Address any Reviewer findings**

Critical or High severity findings must be resolved before marking the sprint complete. Medium, Low, and Observation findings are recorded in `docs/Reviews/` and may be deferred.

- [ ] **Step 3: Mark sprint complete**

```bash
git add docs/Reviews/
git commit -m "review(sprint-2): code review findings recorded — sprint 2 complete"
```

---

## Self-Review

### Spec Coverage

| Deliverable | Task |
|---|---|
| Ferret.sln with all source projects | Tasks 2, 3, 4, 9 |
| All test projects | Tasks 5, 6 |
| Sample plugin project | Task 7 |
| Directory.Build.props (updated) | Task 1 |
| Directory.Packages.props (Central Package Management) | Task 1 |
| .editorconfig (updated) | Task 1 |
| Global using configuration | N/A — implicit usings already enabled in Directory.Build.props |
| GitHub Actions build workflow | Pre-existing `.github/workflows/ci.yml` — no action required |
| Build scripts | Task 8 |
| Nullable reference types | Pre-existing Directory.Build.props `<Nullable>enable</Nullable>` |
| Implicit usings | Pre-existing Directory.Build.props `<ImplicitUsings>enable</ImplicitUsings>` |
| Central Package Management | Task 1 (Directory.Packages.props) |
| Static analyzers | Task 1 (StyleCop.Analyzers via Directory.Build.props + stylecop.json) |
| Warning-as-error policy | Pre-existing Directory.Build.props `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` |
| Every project must compile | Tasks 2–6, validated in Task 9 |
| Every test project must execute | Tasks 5–6, validated in Task 9 |
| Dependency rules validated | Task 9 Steps 4–5 |
| Naming conventions validated | Task 9 Step 6 |
| Reviewer workflow | Task 11 |

### Placeholder Scan

No "TBD", "TODO", or "fill in" text appears in this plan. All file contents are complete and concrete. Package versions are pinned to specific releases. All commands include expected output.

### Type Consistency

All class names (`CoreModule`, `RuntimeModule`, etc.) match their file names (`CoreModule.cs`, `RuntimeModule.cs`). All namespace declarations match STD-005 §3.1 root namespace conventions. All test class names end in `Tests` and their test method names follow `MethodName_StateUnderTest_ExpectedBehaviour`.

### Pre-existing Files Confirmed Correct

- `Directory.Build.props` — already has `TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`, `AnalysisLevel=latest`; Task 1 appends `StyleCop.Analyzers` ItemGroup only
- `.editorconfig` — already has formatting and style rules; Task 1 appends StyleCop suppressions and namespace style only
- `.github/workflows/ci.yml` — already production-ready; not modified in this sprint
- `src/Ferret.sln` — already exists; expanded in Tasks 2–6
