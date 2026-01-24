# UTMO.Text.FileGenerator - Copilot Coding Instructions

## Repository Overview

This is a **C# .NET 9.0 file generation framework** that uses DotLiquid templates to generate text files from configurable template resources. The project is designed as a modular, plugin-based system with a host-based architecture using Microsoft.Extensions.Hosting.

**Repository Structure**: Small-to-medium .NET solution with 7 projects organized under `src/v2/`:
- **Core Projects**: Abstract, FileGenerator, DefaultFileWriter, Validators
- **Plugin Projects**: EnvironmentInit, ResourceManifestGeneration
- **Test Project**: TestFileGenerator.Core.Tests (currently empty - no test files present)

**Key Technologies**:
- Target Framework: .NET 9.0 (SDK version 9.0.306 specified in `global.json`)
- Template Engine: DotLiquid 2.3.197
- Logging: Serilog with console sink
- CLI Parsing: CommandLineParser 2.9.1
- Feature Management: Microsoft.FeatureManagement 4.3.0
- DI/Hosting: Microsoft.Extensions.Hosting 10.0.0
- Testing: NUnit 4.4.0 (no tests currently implemented)

## Build & Validation Process

### Prerequisites
- .NET SDK 9.0.306 or higher (rollForward: patch, no prerelease)
- NuGet package source configured: `https://pkgs.dev.azure.com/utmo-public/_packaging/ConfigGen/nuget/v3/index.json`

### Important Build Rules

**ALWAYS specify the solution file explicitly** - The `src/` directory contains multiple solution files (`.sln` and `.slnx`). Running `dotnet` commands without specifying `UTMO.Text.FileGenerator.sln` will fail with "MSB1011: Specify which project or solution file to use".

**Working directory**: All commands must run from `src/` directory: `S:\Repos\UTMO-Public\LiquidConfigGen\UTMO.Text.FileGenerator\src`

### Build Commands (Verified Working)

```powershell
# Navigate to src directory first
cd S:\Repos\UTMO-Public\LiquidConfigGen\UTMO.Text.FileGenerator\src

# Restore packages (ALWAYS run before building)
dotnet restore UTMO.Text.FileGenerator.sln

# Build Debug configuration (default)
dotnet build UTMO.Text.FileGenerator.sln

# Build Release configuration
dotnet build UTMO.Text.FileGenerator.sln --configuration Release

# Clean build artifacts
dotnet clean UTMO.Text.FileGenerator.sln

# Pack NuGet packages (after building)
dotnet pack UTMO.Text.FileGenerator.sln --configuration Release --no-build

# Run tests (currently no tests exist - command completes successfully but runs nothing)
dotnet test UTMO.Text.FileGenerator.sln
```

### Build Timing
- **Restore**: ~1.1s (first run may be slower for package downloads)
- **Clean Build (Debug)**: ~6s for all 7 projects
- **Release Build**: ~2.6s
- **Clean**: ~0.6s

### Build Order (Automatic via project references)
1. UTMO.Text.FileGenerator.Abstract
2. UTMO.Text.FileGenerator.Validators
3. UTMO.Text.FileGenerator.EnvironmentInit
4. UTMO.Text.FileGenerator.DefaultFileWriter
5. UTMO.Text.FileGenerator.ResourceManifestGeneration
6. UTMO.Text.FileGenerator
7. TestFileGenerator.Core.Tests (test project - no tests)

### Package Generation
- **Enabled**: `GeneratePackageOnBuild` is `true` in `Directory.Build.props`
- All projects automatically generate NuGet packages on successful build
- Packages use centralized version management via `Directory.Packages.props`

## Project Architecture

### Core Architecture Pattern
This is a **hosted service application** using `IHostedService` pattern with a plugin pipeline architecture.

**Execution Flow**:
1. `FileGenerator.Create()` initializes the host with Serilog logging
2. Registers services: `FileGeneratorHost`, `ITemplateRenderer`, `IGeneralFileWriter`, plugins
3. Loads feature flags from embedded `FeatureFlights.manifest.json`
4. `FileGeneratorHost.StartAsync()` executes the generation pipeline:
   - Initialize environments via `EnvironmentInitPlugin`
   - Validate all environments
   - Run "Before" pipeline plugins
   - Render templates (parallel or sequential based on feature flag `EnableParallelResourceRendering`)
   - Run "After" pipeline plugins
5. Exit codes: 0=success, 1=unhandled exception, 3=errors, 5=cancelled, 45=validation failure, -315=tracked exceptions

### Project Responsibilities

**UTMO.Text.FileGenerator.Abstract** (`v2/UTMO.Text.FileGenerator.Abstract/`):
- Defines all contracts/interfaces (16 interface files in `Contracts/`)
- Exception types (9 custom exceptions in `Exceptions/`)
- Enums: `PluginPosition`, `ValidationFailureType`, `EmbeddedResourceType`
- No external dependencies (standalone contract library)

**UTMO.Text.FileGenerator** (`v2/UTMO.Text.FileGenerator/`):
- Main orchestration logic
- Key files:
  - `FileGenerator.cs`: Entry point, host builder configuration
  - `FileGeneratorHost.cs`: IHostedService implementation, pipeline execution (307 lines)
  - `TemplateRenderer.cs`: DotLiquid template rendering
  - `GenerationEnvironmentBase.cs`: Base class for environments
- Features:
  - CLI option parsing via `Models/GeneratorCliOptions.cs`
  - Feature flags via embedded `FeatureFlights.manifest.json`
  - Localized log messages via `.resx` files in `Logging/`
  - Extensions for feature management and dictionary operations

**UTMO.Text.FileGenerator.DefaultFileWriter** (`v2/UTMO.Text.FileGenerator.DefaultFileWriter/`):
- Implements `IGeneralFileWriter` for file I/O operations
- Custom exceptions: `InvalidTemplateDirectoryException`, `InvalidOutputDirectoryException`

**UTMO.Text.FileGenerator.Validators** (`v2/UTMO.Text.FileGenerator.Validators/`):
- Contains `BasicValidators.cs` with validation logic

**Plugin Projects**:
- **EnvironmentInit**: Initialization plugin, runs at startup
- **ResourceManifestGeneration**: Manifest generation plugin with helper utilities

### Configuration Files

**`src/global.json`**: Pins .NET SDK to 9.0.306 with patch rollforward
**`src/nuget.config`**: 
- Custom package source (Azure DevOps feed)
- Global packages folder: `.packages/` (relative to solution)
- Package restore: enabled, NOT automatic

**`v2/Directory.Build.props`**: Applies to all projects:
- `ImplicitUsings`: enabled
- `Nullable`: enabled
- `GeneratePackageOnBuild`: true (auto-creates NuGet packages)
- `TreatWarningsAsErrors`: True (build fails on ANY warning)
- Author: Josh Irwin

**`v2/Directory.Packages.props`**: Central Package Management (CPM):
- `ManagePackageVersionsCentrally`: true
- All package versions defined centrally
- Projects reference packages WITHOUT version attributes

### Code Style & Standards

**ReSharper Settings**:
- Solution-level: `.sln.DotSettings` - disables "ConvertToPrimaryConstructor" inspection
- Project-level: `Abstract.csproj.DotSettings` - skips namespace folders for enums

**Warning Treatment**: `TreatWarningsAsErrors=True` means:
- ANY compiler warning will FAIL the build
- Code must be warning-free to compile
- Use `#pragma warning disable` sparingly and with justification

**Nullable Reference Types**: Enabled across all projects
- Use `!` null-forgiving operator only when null-safety is guaranteed
- Prefer nullable annotations (`?`) over suppressions

**Implicit Usings**: Enabled - common namespaces auto-imported

## Making Code Changes

### Before Making Changes
1. **Check for tests**: Currently NO tests exist (TestFileGenerator.Core.Tests is empty)
2. **Review interfaces**: Start in `Abstract/Contracts/` to understand contracts
3. **Check for validation**: The framework has extensive validation in `FileGeneratorHost.Validate()`

### After Making Changes
1. **Always run restore + build**: `dotnet restore UTMO.Text.FileGenerator.sln && dotnet build UTMO.Text.FileGenerator.sln`
2. **Check for warnings**: Build fails on warnings - address ALL warnings before considering the change complete
3. **Test both Debug and Release**: Release may have different behavior (see commented-out `<Choose>` block in FileGenerator.csproj)
4. **Verify package generation**: Packages auto-generate - check `bin/Debug|Release/` for `.nupkg` files

### Common Pitfalls

**Plugin Architecture**: 
- Plugins execute in defined positions: `PluginPosition.Before` or `PluginPosition.After`
- Both `IPipelinePlugin` (environment-level) and `IRenderingPipelinePlugin` (resource-level) exist
- Registration: Use DI registration in `FileGenerator.Create()`

**Environment.Exit() Usage**: 
- `FileGeneratorHost` uses `Environment.Exit()` with specific codes
- Modify exit behavior carefully - it terminates the entire process

**Async/Await Patterns**:
- Heavy use of `.WaitAsync(cancellationToken)` for cancellation support
- Parallel rendering available via feature flag - check both code paths

**Resource Files**:
- `.resx` files auto-generate `Designer.cs` classes
- Modify `.resx` directly, not generated code

### Directory Structure Quick Reference

```
src/
├── global.json                           # SDK version pinning
├── nuget.config                          # NuGet feed configuration  
├── UTMO.Text.FileGenerator.sln          # MAIN SOLUTION FILE (use this)
├── UTMO.Text.FileGenerator.slnx         # Secondary solution file
├── .gitignore                           # Standard .NET gitignore
├── v2/                                  # All v2 projects
│   ├── Directory.Build.props            # Global project properties
│   ├── Directory.Packages.props         # Central package versions
│   ├── UTMO.Text.FileGenerator.Abstract/
│   │   ├── Contracts/                   # 16 interface definitions
│   │   ├── Exceptions/                  # 9 custom exceptions
│   │   ├── Enums/                       # 3 enum types
│   │   └── Attributes/                  # Custom attributes
│   ├── UTMO.Text.FileGenerator/         # MAIN PROJECT
│   │   ├── FileGenerator.cs             # Entry point & host setup
│   │   ├── FileGeneratorHost.cs         # Pipeline orchestration (307 lines)
│   │   ├── TemplateRenderer.cs          # DotLiquid rendering
│   │   ├── GenerationEnvironmentBase.cs # Base environment class
│   │   ├── FeatureFlights.manifest.json # Feature flags (embedded resource)
│   │   ├── Constants/FeatureFlags.cs    # Feature flag string constants
│   │   ├── Extensions/                  # Helper extensions
│   │   ├── Logging/                     # Log message resources (.resx)
│   │   ├── Models/                      # DTOs and models
│   │   └── Utils/                       # Utility classes
│   ├── UTMO.Text.FileGenerator.DefaultFileWriter/
│   │   ├── DefaultFileWriter.cs         # File I/O implementation
│   │   └── Exceptions/                  # File writer exceptions
│   ├── UTMO.Text.FileGenerator.Validators/
│   │   └── BasicValidators.cs           # Validation logic
│   ├── Plug-ins/
│   │   ├── UTMO.Text.FileGenerator.EnvironmentInit/
│   │   └── UTMO.Text.FileGenerator.ResourceManifestGeneration/
│   └── TestFileGenerator.Core.Tests/    # Empty test project (no tests)
```

## CI/CD & Validation

**Pipeline Files**: Referenced in solution but located outside workspace at `../.pipelines/`:
- `UTMO.Text.FileGenerator-Unified.yml`
- `UTMO.Text.FileGenerator.PipelinePlugin.ScriptRunner-Official.yml`
- `UTMO.Text.FileGenerator.PipelinePlugin.ScriptRunner-Release.yml`
- `UTMO.Text.FileGenerator.RendererPlugin.ScriptRunner-Official.yml`
- `UTMO.Text.FileGenerator.RendererPlugin.ScriptRunner-Release.yml`

**Note**: Pipeline files are outside the `src/` workspace and may not be directly accessible.

**Validation Steps to Replicate CI**:
1. Clean build: `dotnet clean UTMO.Text.FileGenerator.sln`
2. Restore: `dotnet restore UTMO.Text.FileGenerator.sln`
3. Build Release: `dotnet build UTMO.Text.FileGenerator.sln --configuration Release`
4. Run tests: `dotnet test UTMO.Text.FileGenerator.sln` (currently passes with 0 tests)
5. Pack: `dotnet pack UTMO.Text.FileGenerator.sln --configuration Release --no-build`

## Known Issues & Workarounds

**Issue**: Running `dotnet` commands without solution file specified fails
**Workaround**: ALWAYS specify `UTMO.Text.FileGenerator.sln` explicitly

**Issue**: Workload updates available warning appears
**Behavior**: This is informational only - does not affect builds
**Action**: Can be safely ignored unless mobile workloads are needed

**Issue**: Test project exists but contains no test files
**Behavior**: `dotnet test` succeeds but runs nothing
**Action**: This is expected - tests not yet implemented

**Known TODO**: In `FileGeneratorHost.cs` line 55 - "TODO: Evaluate if this is needed" regarding `IGeneralFileWriter FileWriter` property (unused but kept)

## Trust These Instructions

These instructions were created through **comprehensive repository analysis** including:
- Reading all solution and project files
- Executing and validating all build commands
- Analyzing project dependencies and architecture
- Reviewing key source files and interfaces
- Testing build, clean, restore, pack operations

**Only search/explore further if**:
- These instructions are incomplete for your specific task
- You encounter errors that contradict these instructions
- You need details about specific implementation files not covered here

When in doubt, start with the build commands above - they are verified to work correctly.
