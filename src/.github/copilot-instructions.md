# Copilot Coding Agent Instructions for UTMO.Text.FileGenerator

## Repository Overview

**UTMO.Text.FileGenerator** is a .NET-based template-driven file generation framework that leverages DotLiquid templates to dynamically generate configuration and code files. The repository produces NuGet packages used as build-time or run-time dependencies in other projects.

### Key Technologies
- **Language**: C# (.NET 9.0)
- **Repository Type**: Multi-project solution with core libraries, plugins, and test framework
- **Template Engine**: DotLiquid (Liquid template syntax)
- **Logging**: Serilog
- **Configuration**: Microsoft.Extensions.Configuration & Microsoft.FeatureManagement
- **Package Management**: Centralized dependency versioning via `Directory.Packages.props`
- **CI/CD**: Azure Pipelines (located in `.pipelines/` folder, outside src directory)

### Repository Size & Structure
- ~8 C# projects organized into Core, Plug-ins, Validators, and Tests
- Single solution file: `UTMO.Text.FileGenerator.sln`
- All source code located in `v2/` subdirectory
- All configuration is centralized in `v2/Directory.Build.props` and `v2/Directory.Packages.props`

---

## Build & Environment Setup

### Prerequisites
- **.NET SDK 9.0.310** (or later patch version) - verified working version
- PowerShell 5.1+ (Windows)
- The `.packages/` folder is local and uses a custom NuGet source (UTMO packages from Azure Artifacts)

### Package Restoration
Always restore packages before building any project:
```powershell
cd "src"
dotnet restore UTMO.Text.FileGenerator.sln
```

**Important**: The `nuget.config` specifies a centralized package folder at `.packages/`. All NuGet dependencies are sourced from:
- **ConfigGen Feed**: `https://pkgs.dev.azure.com/utmo-public/_packaging/ConfigGen/nuget/v3/index.json`

This feed contains custom UTMO packages, which are required for builds to succeed.

### Building the Solution

**Full clean build** (recommended when making structural changes):
```powershell
cd "src"
dotnet clean UTMO.Text.FileGenerator.sln
dotnet restore UTMO.Text.FileGenerator.sln
dotnet build UTMO.Text.FileGenerator.sln --configuration Debug
```

**Incremental build** (for quick iterations):
```powershell
cd "src"
dotnet build UTMO.Text.FileGenerator.sln --configuration Debug --no-restore
```

**Build-time Notes**:
- Initial build may show workload update warnings (safe to ignore)
- Successful build produces assemblies in `v2/*/bin/Debug/net9.0/`
- Build enforces `TreatWarningsAsErrors=True` in `Directory.Build.props` - all warnings must be resolved

### Running Tests

The test project is `v2/TestFileGenerator.Core.Tests/TestFileGenerator.Core.Tests.csproj` (NUnit framework):
```powershell
cd "src"
dotnet test UTMO.Text.FileGenerator.sln --configuration Debug
```

**Note**: Currently, the test project exists but contains no test cases. The command succeeds without running any tests.

---

## Project Structure & Architecture

### Solution Organization (from `UTMO.Text.FileGenerator.sln`)

**Core Projects** (`v2/Core/`):
- **UTMO.Text.FileGenerator.Abstract** - Contracts and interfaces defining the plugin/template architecture
- **UTMO.Text.FileGenerator** - Main engine for file generation, hosts plugins, renders templates via DotLiquid
- **UTMO.Text.FileGenerator.DefaultFileWriter** - Default implementation of file writing with Serilog logging

**Plugins** (`v2/Plug-ins/`):
- **UTMO.Text.FileGenerator.EnvironmentInit** - Plugin for environment initialization
- **UTMO.Text.FileGenerator.ResourceManifestGeneration** - Plugin for manifest generation (includes ReX resource files)

**Modules** (`v2/Modules/`):
- **UTMO.Text.FileGenerator.Validators** - Validation utilities and validators

**Tests** (`v2/Tests/`):
- **TestFileGenerator.Core.Tests** - NUnit-based test project (currently empty)

### Key Source Files

**Main Entry Point**:
- `v2/UTMO.Text.FileGenerator/FileGenerator.cs` - Static factory `FileGenerator.Create(string[] args, LogEventLevel logLevel)` initializes the generator with CLI parsing via CommandLineParser library

**Core Abstractions**:
- `v2/UTMO.Text.FileGenerator.Abstract/Contracts/ITemplateRenderer.cs` - Template rendering interface
- `v2/UTMO.Text.FileGenerator.Abstract/Contracts/IGeneratorCliOptions.cs` - CLI options contract
- `v2/UTMO.Text.FileGenerator.Abstract/Contracts/ITemplateGenerationEnvironment.cs` - Generation environment interface

**Template Rendering**:
- `v2/UTMO.Text.FileGenerator/TemplateRenderer.cs` - Implements DotLiquid template parsing and rendering with error handling

**Generation Environment**:
- `v2/UTMO.Text.FileGenerator/GenerationEnvironmentBase.cs` - Base class for template generation environments with resource validation and initialization

**CLI Models**:
- `v2/UTMO.Text.FileGenerator/Models/GeneratorCliOptions.cs` - CLI argument definitions using CommandLineParser attributes

### Configuration & Build Metadata

**Root Solution Configuration**:
- `v2/Directory.Build.props` - Global project properties (nullable enabled, implicit usings, treat warnings as errors, package generation on build)
- `v2/Directory.Packages.props` - Centralized NuGet package versions (31 packages defined, e.g., DotLiquid 2.3.197, Serilog 4.3.0, NUnit 4.4.0)
- `UTMO.Text.FileGenerator.sln.DotSettings` - JetBrains Rider IDE settings (suppresses primary constructor inspection)

**Root Configuration Files**:
- `nuget.config` - NuGet configuration with custom packages folder (`.packages/`) and Azure Artifacts feed
- `global.json` - Enforces .NET SDK version 9.0.306+ with patch rollforward

### Feature Management

- `v2/UTMO.Text.FileGenerator/FeatureFlights.manifest.json` - Feature flight configuration (embedded resource)
- Uses Microsoft.FeatureManagement for feature toggling

### Logging

All projects use **Serilog** for structured logging:
- Configured in `FileGenerator.cs` with console sink and exception enrichment
- Log message resources in `Logging/LogMessages.resx` (auto-generated Designer.cs files)
- Plugin projects embed their own `.resx` resources for localized messages

---

## Project Dependency Graph

```
UTMO.Text.FileGenerator (main)
  ├── UTMO.Text.FileGenerator.Abstract (contracts)
  ├── UTMO.Text.FileGenerator.DefaultFileWriter
  │   └── UTMO.Text.FileGenerator.Abstract
  ├── UTMO.Text.FileGenerator.EnvironmentInit (plugin)
  │   └── UTMO.Text.FileGenerator.Abstract
  └── UTMO.Text.FileGenerator.ResourceManifestGeneration (plugin)
      └── UTMO.Text.FileGenerator.Abstract

UTMO.Text.FileGenerator.Validators
  └── UTMO.Text.FileGenerator.Abstract
```

All NuGet package versions are managed centrally in `v2/Directory.Packages.props`. Do not add direct package references; instead, update the central file.

---

## Validation & CI/CD

### Azure Pipelines

CI/CD pipelines are located in `.pipelines/` (outside the `src/` directory):
- **UTMO.Text.FileGenerator-Unified.yml** - Main pipeline (triggered on `src/v2/*` changes)
- Configurable parameters: Configuration (Release/Debug), MajorVersion, MinorVersion
- Builds the solution and packages projects for NuGet publishing
- Projects with `GeneratePackageOnBuild=true` automatically create NuGet packages on build

### Pre-Check Validation
When making code changes, always verify:
1. **Build succeeds**: `dotnet build UTMO.Text.FileGenerator.sln --configuration Debug`
2. **No new warnings**: TreatWarningsAsErrors=True enforces this
3. **Tests pass**: `dotnet test UTMO.Text.FileGenerator.sln` (even though currently empty)
4. **Check for compiler errors**: `dotnet build --no-restore` should complete without errors

### Package Generation
Plugins and core modules automatically generate NuGet packages during build (GeneratePackageOnBuild=true). Verify `.nupkg` files exist in respective `bin/Debug/` directories.

---

## Important Build & Design Constraints

### Code Standards
1. **Implicit Usings**: Enabled globally (`ImplicitUsings=enable`)
2. **Nullable Reference Types**: Enabled globally (`Nullable=enable`)
3. **Warning Severity**: All warnings treated as errors (`TreatWarningsAsErrors=True`)
4. **Language Version**: Default for most projects; latest for test project

### Plugin Architecture
- All plugins must implement interfaces from `UTMO.Text.FileGenerator.Abstract`
- Plugins are discovered and loaded at runtime by FileGenerator
- Must be packaged as NuGet packages (GeneratePackageOnBuild=true)

### Template Rendering Notes
- Templates use **DotLiquid syntax** (Liquid template language)
- Template file extension: `.liquid` (automatically appended if not provided)
- Templates are loaded from the path specified in CLI option `--template-path`
- Global context can be merged with per-template context in TemplateRenderer

### Logging & Error Handling
- Serilog is the standardized logger across all projects
- Use `ILogger<T>` from Microsoft.Extensions.Logging abstraction
- Log exceptions with enrichment via Serilog.Exceptions
- LogMessages.resx files contain localized log messages (auto-generated Designer classes)

---

## Troubleshooting & Known Behaviors

### Build Warnings About Workloads
Output messages like "Workload updates are available" are informational and safe to ignore. The build succeeds despite these warnings.

### Package Restore Failures
If you see "Package not found" errors, ensure:
- You have access to the Azure Artifacts feed (`ConfigGen`)
- Your NuGet credentials are configured (may require Azure DevOps authentication)
- The `.packages/` folder exists in the repo root

### Test Project Is Empty
The test project `TestFileGenerator.Core.Tests` currently has no test cases. When you add tests, place them as `.cs` files in the test project directory. The test framework is NUnit 4.4.0 with NUnit3TestAdapter for discovery.

### Clean Build vs. Incremental
Use clean builds when:
- Modifying project structure (adding/removing files)
- Changing Directory.Build.props or Directory.Packages.props
- Experiencing unexplained build failures

Use incremental builds for quick iteration on source code changes.

---

## File Locations Reference

| Item | Path |
|------|------|
| Solution File | `src/UTMO.Text.FileGenerator.sln` |
| Global Build Props | `src/v2/Directory.Build.props` |
| Package Versions | `src/v2/Directory.Packages.props` |
| Main Project | `src/v2/UTMO.Text.FileGenerator/` |
| Abstract Interfaces | `src/v2/UTMO.Text.FileGenerator.Abstract/Contracts/` |
| Tests | `src/v2/TestFileGenerator.Core.Tests/` |
| Plugins | `src/v2/Plug-ins/` |
| Pipelines | `.pipelines/` (parent directory) |
| NuGet Config | `src/nuget.config` |
| SDK Version | `src/global.json` |

---

## Trusting These Instructions

**Follow these instructions first** before searching the codebase. The information provided covers:
- ✅ All required build and test commands
- ✅ Complete project structure and dependencies
- ✅ Standard versions and runtime requirements
- ✅ Known issues and workarounds
- ✅ Architecture and design patterns

**Only perform additional searches if**:
- You need implementation details for a specific class or method
- You're adding a new feature not covered in these instructions
- You encounter an error not mentioned in the Troubleshooting section
- Instructions appear incomplete or incorrect (please report this)

