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
- `ParallelTemplateRendering` - Enable parallel template rendering
- `ParallelPropertyRendering` - Enable parallel rendering of collection properties within a template resource
- `LegacyNonPublicTemplateProperties` - **Migration only (deprecated, security risk)**: Re-enables the legacy behavior of exposing all properties to templates — both non-public properties and public properties that are not decorated with `[TemplateProperty]` — emitting a deprecation warning per-property. Non-public properties marked with `[TemplateProperty]` are never exposed regardless of this flag. Defaults to `false`. Enable only temporarily during migration to identify which properties your templates rely on, then annotate all intended public properties with `[TemplateProperty]`, and disable the flag.
- `SuppressNonPublicPropertyWarnings` - Suppresses the per-property warning messages that are emitted when a non-public property is encountered and `LegacyNonPublicTemplateProperties` is disabled. Enable this flag when non-public properties in your template resource classes are intentional and the migration guidance warnings are no longer needed. Defaults to `false`.
- `ManifestReferenceResolution` - Enables cross-resource manifest reference resolution. When enabled, template resources that declare manifest references (via `AddManifestReference`) will have those references resolved from the in-memory manifest index before each template render. Defaults to `false`. See [Manifest Reference Resolution](#manifest-reference-resolution) for details.

## Manifest Reference Resolution

The manifest reference feature allows a template resource to declaratively reference a property
from another resource's manifest output.  This is most useful for expressing cross-configuration
dependencies — for example, in PowerShell DSC scenarios where one configuration must list another
configuration as a dependency.

### How it works

1. **Index building** – Before any template is rendered, `ManifestIndexBuildingPlugin` walks
   all resources in the environment, calls `ToManifest()` on every `IManifestProducer` with
   `GenerateManifest = true`, and stores the results in an in-memory index keyed by
   `(ResourceTypeName, ResourceName)`.
2. **Resolution** – `ManifestReferenceResolverPlugin` runs before each template render.  For
   every manifest reference declared on the resource it looks up the value from the index and
   injects it into the template context via `AddAdditionalProperty`.
3. **Feature flag gate** – The entire pipeline is gated behind the
   `ManifestReferenceResolution` feature flag (default **off**).  Set it to `true` in
   `FeatureFlights.manifest.json` or your own feature management configuration to enable it.

### Declaring a manifest reference

Call `AddManifestReference` inside your resource's constructor or `Initialize` override:

```csharp
public class DscNodeConfigurationResource : TemplateResourceBase
{
    public DscNodeConfigurationResource(string nodeName, string dependsOnConfig)
    {
        NodeName = nodeName;

        // Declare a reference to the "DependsOn" property of another resource's manifest.
        // The resolved value is injected into the template context under the key "DependsOn".
        AddManifestReference("DependsOn", new ManifestReference
        {
            ResourceTypeName = "NodeConfiguration",
            ResourceName     = dependsOnConfig,
            PropertyPath     = "DependsOn",
            // DefaultValue = null means "required" – generation fails if not resolved.
            // Provide a non-null string to make it optional with a fallback.
            DefaultValue     = null
        });
    }

    [TemplateProperty]
    public string NodeName { get; }

    public override string ResourceTypeName => "NodeConfiguration";
    public override string TemplatePath     => "Dsc/NodeConfiguration";
    public override string OutputExtension  => "ps1";
    public override string ResourceName     => NodeName;
}
```

The referenced resource must have `GenerateManifest = true` and return the relevant data from
its `ToManifest()` implementation:

```csharp
public class BaseConfigResource : TemplateResourceBase, IManifestProducer
{
    public override bool GenerateManifest => true;

    public override Task<object?> ToManifest() =>
        Task.FromResult<object?>(new { DependsOn = $"[{ResourceTypeName}]{ResourceName}" });

    // ... other members
}
```

### Property path syntax

The `PropertyPath` field uses a dot-separated syntax to navigate nested objects:

| Path | Description |
|------|-------------|
| `"DependsOn"` | Top-level property named `DependsOn` |
| `"Network.SubnetId"` | Property `SubnetId` on the nested `Network` object |
| `"A.B.C"` | Three-level nesting |
| `""` (empty) | The entire manifest root object |

Path navigation works on:
- Plain CLR objects (via reflection)
- `Dictionary<string, object>` / `Dictionary<string, object?>` instances

### Required vs optional references

| `DefaultValue` | Behaviour when unresolved |
|---|---|
| `null` | **Required** – logs an error and returns `false` from `HandleTemplate`, causing `IsSuccessfulRun = false`. |
| `""` or any string | **Optional** – the default value is injected and a warning is logged. |

### Enabling the feature flag

In `FeatureFlights.manifest.json`:

```json
{
  "FeatureManagement": {
    "ManifestReferenceResolution": true
  }
}
```

### Cycle protection

The index-building traversal tracks visited resources (by `ResourceTypeName/ResourceName`) and
skips resources that have already been visited in the current traversal, preventing infinite
recursion when resources contain mutual references in their property graph.

## Security

### Template Property Exposure

By default, **no properties** are exposed to DotLiquid templates. Developers must explicitly opt in using the `[TemplateProperty]` attribute on public properties they want to make available to templates. This prevents accidental exposure of sensitive data (credentials, tokens, internal state).

#### Rules
1. A property is exposed if and only if it is **public** AND decorated with `[TemplateProperty]` (when the `LegacyNonPublicTemplateProperties` feature flag is disabled, which is the secure default).
2. `[IgnoreMember]` always takes precedence and will exclude a property from the template context.
3. Non-public properties decorated with `[TemplateProperty]` are **never** exposed — the attribute is only valid on public properties. Non-public properties *without* `[TemplateProperty]` are only exposed when the `LegacyNonPublicTemplateProperties` migration flag is enabled (deprecated behavior, emits a security warning per-property).
4. Properties added via `AddAdditionalProperty<T>()` are always exposed regardless of attributes.

#### Migration from older versions
If upgrading from a version that exposed all public properties by default:
1. Enable the `LegacyNonPublicTemplateProperties` feature flag temporarily if you need to preserve legacy behavior during migration. This restores the deprecated exposure of non-public properties (with security warnings) and also restores exposure of public properties without `[TemplateProperty]`.
2. Add `[TemplateProperty]` to every public property that your templates need so you can disable the feature flag and keep only explicit, intended template exposure.
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