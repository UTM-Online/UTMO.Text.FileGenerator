# UTMO.Text.FileGenerator

A .NET 9.0 template-based file generation framework using DotLiquid templates with a modular, plugin-based architecture.

## Overview

UTMO.Text.FileGenerator is a flexible code generation engine that processes Liquid templates to produce text files. It features a host-based architecture with dependency injection, feature flags, and extensible plugin pipelines for custom processing.

## Features

- **DotLiquid Template Engine**: Uses the powerful Liquid templating language for file generation
- **Plugin Architecture**: Extensible pipeline with before/after hooks at both environment and resource levels
- **Feature Flags**: Runtime feature toggling using Microsoft.FeatureManagement (e.g., parallel rendering)
- **Dependency Injection**: Full Microsoft.Extensions.DependencyInjection integration
- **Structured Logging**: Serilog-based logging with exception enrichment
- **Async/Await**: Fully asynchronous API with cancellation token support
- **Validation**: Built-in validation framework with detailed error reporting
- **Cross-Platform**: Works on Windows and Linux with automatic path normalization

## Project Structure

```
src/v2/
├── UTMO.Text.FileGenerator          # Core generation engine
├── UTMO.Text.FileGenerator.Abstract # Contracts and exceptions
├── UTMO.Text.FileGenerator.DefaultFileWriter # File I/O implementation
├── UTMO.Text.FileGenerator.Validators # Validation utilities
└── Plug-ins/
    ├── EnvironmentInit             # Environment initialization plugin
    └── ResourceManifestGeneration  # Manifest generation plugin
```

## Getting Started

### Prerequisites

- .NET SDK 9.0.306 or higher
- Access to the UTMO NuGet feed (configured in `nuget.config`)

### Installation

1. Clone the repository
2. Navigate to the `src/` directory
3. Restore NuGet packages:
   ```bash
   dotnet restore UTMO.Text.FileGenerator.slnx
   ```

### Building the Solution

```bash
cd src/
dotnet build UTMO.Text.FileGenerator.slnx
```

For release builds:
```bash
dotnet build UTMO.Text.FileGenerator.slnx --configuration Release
```

### Running Tests

```bash
dotnet test UTMO.Text.FileGenerator.slnx
```

## Usage

### Basic Usage

```csharp
// Create a file generator with your custom environment
var generator = FileGenerator.Create(args)
    .UseEnvironment<MyGenerationEnvironment>()
    .RegisterPipelinePlugin<MyCustomPlugin>()
    .RegisterCustomCliOptions<MyCliOptions>();

// Run the generation process
generator.Run();
```

### Creating a Generation Environment

```csharp
public class MyEnvironment : GenerationEnvironmentBase
{
    public override string EnvironmentName => "MyEnvironment";
    
    public override void Initialize()
    {
        // Add your template resources
        AddResource(new MyTemplateResource());
    }
}
```

### Creating Template Resources (Secure by Default)

Template resources use an **opt-in security model**: only properties explicitly decorated with `[TemplateProperty]` are exposed to the DotLiquid template context. This prevents accidental exposure of sensitive data.

```csharp
using UTMO.Text.FileGenerator.Attributes;

public class MyTemplateResource : TemplateResourceBase
{
    // ✅ Explicitly exposed to templates
    [TemplateProperty]
    public string ServerName { get; set; } = "my-server";

    [TemplateProperty]
    public string Environment { get; set; } = "production";

    // ✅ NOT exposed to templates (no [TemplateProperty] attribute)
    public string ApiKey { get; set; } = "secret-key";

    // ✅ Explicitly excluded even if [TemplateProperty] is also present
    [IgnoreMember]
    public string InternalId { get; set; } = "internal";

    public override string ResourceTypeName => "MyResource";
    public override string TemplatePath => "Templates/MyTemplate";
    public override string OutputExtension => ".json";
    public override string ResourceName => "myresource";
}
```

Templates can then access the exposed properties:
```liquid
{
  "server": "{{ ServerName }}",
  "env": "{{ Environment }}"
}
```

#### Security Attributes

| Attribute | Namespace | Purpose |
|---|---|---|
| `[TemplateProperty]` | `UTMO.Text.FileGenerator.Attributes` | Opt-in: marks a **public** property as safe to expose to templates |
| `[IgnoreMember]` | `UTMO.Text.FileGenerator.Attributes` | Opt-out: explicitly excludes a property from the template context (takes precedence over `[TemplateProperty]`) |
| `[MemberName("alias")]` | `UTMO.Text.FileGenerator.Attributes` | Renames the property key used in the template context |

> **Important**: Only **public** properties decorated with `[TemplateProperty]` are exposed (when the `LegacyNonPublicTemplateProperties` feature flag is disabled, which is the secure default). Private and protected properties are never exposed by default.

### Exit Codes

The application uses standardized exit codes:
- `0` - Success
- `1` - Unhandled exception
- `3` - Generation completed with errors
- `5` - Operation cancelled
- `45` - Validation failure
- `-315` - Exceptions were tracked during execution
- `-3828` - Path normalization error

## Architecture

### Plugin System

The framework supports two types of plugins:

1. **IPipelinePlugin**: Environment-level processing (runs before/after entire environment)
2. **IRenderingPipelinePlugin**: Resource-level processing (runs before/after each template)

Both plugins can be positioned to run before or after their target operation using `PluginPosition`.

### Feature Flags

Feature flags are configured via `FeatureFlights.manifest.json`. Available flags:
- `ParallelResourceRendering` - Enable parallel template rendering
- `ParallelPropertyRendering` - Enable parallel rendering of collection properties within a template resource
- `LegacyNonPublicTemplateProperties` - **Migration only**: Temporarily restores the previous (insecure) behavior of exposing all public properties and non-public properties to templates, regardless of whether they carry `[TemplateProperty]`. Defaults to `false`. Enable only during migration from older versions to identify which properties your templates rely on, then add `[TemplateProperty]` to those public properties and disable the flag.

## Security

### Template Property Exposure

By default, **no properties** are exposed to DotLiquid templates. Developers must explicitly opt in using the `[TemplateProperty]` attribute on public properties they want to make available to templates. This prevents accidental exposure of sensitive data (credentials, tokens, internal state).

#### Rules
1. A property is exposed if and only if it is **public** AND decorated with `[TemplateProperty]` (when the `LegacyNonPublicTemplateProperties` feature flag is disabled, which is the secure default).
2. `[IgnoreMember]` always takes precedence and will exclude a property from the template context.
3. Private and protected properties are **never** exposed by default, even with `[TemplateProperty]`. They are only exposed when the `LegacyNonPublicTemplateProperties` migration flag is enabled.
4. Properties added via `AddAdditionalProperty<T>()` are always exposed regardless of attributes.

#### Migration from older versions
If upgrading from a version that exposed all public properties by default:
1. Enable the `LegacyNonPublicTemplateProperties` feature flag temporarily to restore previous behavior during migration.
2. Add `[TemplateProperty]` to every public property that your templates need.
3. Disable the feature flag once migration is complete.

## Contributing

Contributions are welcome! Please ensure:
1. Code follows existing patterns and style
2. All builds pass without warnings (TreatWarningsAsErrors is enabled)
3. New features include appropriate tests
4. XML documentation comments are provided for public APIs

## License

Copyright (c) Microsoft Corporation. All rights reserved.

## Support

For issues and questions, please use the repository issue tracker.