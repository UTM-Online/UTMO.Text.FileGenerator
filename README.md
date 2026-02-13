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
   dotnet restore UTMO.Text.FileGenerator.sln
   ```

### Building the Solution

```bash
cd src/
dotnet build UTMO.Text.FileGenerator.sln
```

For release builds:
```bash
dotnet build UTMO.Text.FileGenerator.sln --configuration Release
```

### Running Tests

```bash
dotnet test UTMO.Text.FileGenerator.sln
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