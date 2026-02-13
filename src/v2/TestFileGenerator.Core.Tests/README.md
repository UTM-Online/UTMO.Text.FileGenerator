# Test Suite for UTMO.Text.FileGenerator

This test project provides comprehensive test coverage for the UTMO.Text.FileGenerator library and its dependencies.

## Test Organization

Tests are organized by component in the following structure:

```
TestFileGenerator.Core.Tests/
├── Constants/              # Tests for constants classes
├── DefaultFileWriter/      # File I/O and security tests
├── Environment/            # Environment validation and resource management tests
├── Exceptions/             # Custom exception tests
├── Extensions/             # Extension method tests
├── Integration/            # End-to-end integration tests
├── Plugins/                # Plugin execution tests
├── TemplateRenderer/       # Template rendering tests
└── Validators/             # Validation API tests
```

## Test Coverage Summary

### Security Tests (11 tests)
- **DefaultFileWriterSecurityTests**: Path traversal protection, system directory blocking, overwrite behavior

### Core Functionality Tests (67 tests total)

#### DefaultFileWriter (11 tests)
- Path validation (traversal attacks, system paths)
- Directory creation
- File overwrite protection
- Null/empty path handling

#### Validators (11 tests)
- Fluent validation API
- Null checks, string validation, array validation
- Error accumulation and chaining

#### Exceptions (7 tests)
- Custom exception initialization
- Exception properties
- Inner exception wrapping

#### TemplateRenderer (8 tests)
- Template finding and loading
- Context merging (local + global)
- Error handling (missing templates, empty output)
- Complex template rendering

#### Constants (13 tests)
- Exit code values
- Generation constants
- Feature flag names

#### Environment (13 tests)
- Resource management
- Validation with retries
- Environment constants
- Error accumulation

#### Extensions (10 tests)
- Dictionary merging
- AddOrUpdate behavior
- Template context conversion
- Join operations

#### Plugins (10 tests)
- EnvironmentInitPlugin execution
- Sync/async initialization
- Error handling
- Logging verification

#### Integration (4 tests)
- End-to-end generation workflows
- Complex template scenarios
- Multi-resource validation

## Running Tests

### Run all tests
```bash
cd src
dotnet test UTMO.Text.FileGenerator.sln
```

### Run tests with detailed output
```bash
cd src
dotnet test UTMO.Text.FileGenerator.sln --logger "console;verbosity=detailed"
```

### Run tests for specific category
```bash
cd src
dotnet test UTMO.Text.FileGenerator.sln --filter "FullyQualifiedName~SecurityTests"
```

### Run tests with coverage
```bash
cd src
dotnet test UTMO.Text.FileGenerator.sln --collect:"XPlat Code Coverage"
```

## Test Frameworks and Libraries

- **NUnit 4.4.0**: Testing framework
- **Moq**: Mocking library for interfaces and dependencies
- **FluentAssertions**: Readable assertion syntax
- **coverlet**: Code coverage tool

## Writing New Tests

### Test Naming Convention
- Test class: `{ComponentName}Tests` (e.g., `DefaultFileWriterSecurityTests`)
- Test method: `{MethodName}_{Scenario}_{ExpectedResult}` (e.g., `WriteFile_WithPathTraversal_ShouldThrowException`)

### Example Test Structure
```csharp
[TestFixture]
public class MyComponentTests
{
    private MyComponent _component = null!;
    private Mock<IDependency> _mockDependency = null!;

    [SetUp]
    public void Setup()
    {
        _mockDependency = new Mock<IDependency>();
        _component = new MyComponent(_mockDependency.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Cleanup resources
    }

    [Test]
    public void MyMethod_WithValidInput_ShouldReturnExpectedResult()
    {
        // Arrange
        var input = "test";

        // Act
        var result = _component.MyMethod(input);

        // Assert
        result.Should().Be("expected");
    }
}
```

## Best Practices

1. **Arrange-Act-Assert**: Structure tests clearly with these three sections
2. **One Assertion Per Test**: Focus each test on a single behavior
3. **Descriptive Names**: Test names should describe the scenario and expected outcome
4. **Mock External Dependencies**: Use Moq to isolate units under test
5. **Clean Up Resources**: Use TearDown to clean up temporary files/directories
6. **FluentAssertions**: Use for readable, expressive assertions

## Key Test Scenarios Covered

### Security
✅ Path traversal attacks (../, ~/)  
✅ System directory access prevention  
✅ Input validation (null, empty, whitespace)

### Error Handling
✅ Exception throwing and propagation  
✅ Error message accuracy  
✅ Logging verification  
✅ Inner exception wrapping

### Business Logic
✅ Template rendering with DotLiquid  
✅ Context merging (global + local)  
✅ Resource validation with retries  
✅ Plugin execution order

### Integration
✅ End-to-end file generation  
✅ Multi-resource workflows  
✅ Complex template scenarios

## Continuous Integration

These tests are designed to run in CI/CD pipelines:
- Fast execution (most tests under 100ms)
- Isolated (no external dependencies)
- Deterministic (no flaky tests)
- Comprehensive coverage of critical paths

## Future Enhancements

Potential areas for additional test coverage:
- [ ] Parallel vs sequential rendering performance
- [ ] Feature flag toggling during execution
- [ ] Cancellation token propagation
- [ ] Resource manifest generation
- [ ] Custom plugin scenarios
- [ ] CLI argument parsing edge cases
