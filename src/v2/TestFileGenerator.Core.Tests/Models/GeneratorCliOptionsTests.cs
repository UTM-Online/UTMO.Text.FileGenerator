using CommandLine;
using FluentAssertions;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests.Models;

[TestFixture]
public class GeneratorCliOptionsTests
{
    [Test]
    public void CommandLineParser_WithForceFlag_ShouldSetAllowOverwrite()
    {
        // Arrange
        var args = new[] { "-o", "/output", "-t", "/templates", "--force" };

        // Act
        var result = Parser.Default.ParseArguments<GeneratorCliOptions>(args);

        // Assert
        result.Value.AllowOverwrite.Should().BeTrue();
    }

    [Test]
    public void CommandLineParser_WithShortForceFlag_ShouldSetAllowOverwrite()
    {
        // Arrange
        var args = new[] { "-o", "/output", "-t", "/templates", "-f" };

        // Act
        var result = Parser.Default.ParseArguments<GeneratorCliOptions>(args);

        // Assert
        result.Value.AllowOverwrite.Should().BeTrue();
    }

    [Test]
    public void CommandLineParser_WithoutForceFlag_ShouldNotSetAllowOverwrite()
    {
        // Arrange
        var args = new[] { "-o", "/output", "-t", "/templates" };

        // Act
        var result = Parser.Default.ParseArguments<GeneratorCliOptions>(args);

        // Assert
        result.Value.AllowOverwrite.Should().BeFalse();
    }

    [Test]
    public void AllowOverwrite_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var options = new GeneratorCliOptions();

        // Assert
        options.AllowOverwrite.Should().BeFalse();
    }
}
