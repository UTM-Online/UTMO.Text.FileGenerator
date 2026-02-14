using FluentAssertions;
using UTMO.Text.FileGenerator.DefaultFileWriter;
using UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;

namespace TestFileGenerator.Core.Tests.DefaultFileWriter;

/// <summary>
/// Tests for DefaultFileWriter path validation and security.
/// </summary>
[TestFixture]
public class DefaultFileWriterSecurityTests
{
    private UTMO.Text.FileGenerator.DefaultFileWriter.DefaultFileWriter _fileWriter = null!;
    private string _testOutputDir = null!;

    [SetUp]
    public void Setup()
    {
        _fileWriter = new UTMO.Text.FileGenerator.DefaultFileWriter.DefaultFileWriter();
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"FileGeneratorTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testOutputDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testOutputDir))
        {
            Directory.Delete(_testOutputDir, recursive: true);
        }
    }

    [Test]
    public void WriteFile_WithPathTraversalDoubleDot_ShouldThrowInvalidOutputDirectoryException()
    {
        // Arrange
        var maliciousPath = Path.Combine(_testOutputDir, "..", "malicious.txt");
        var content = "malicious content";

        // Act & Assert
        var act = async () => await _fileWriter.WriteFile(maliciousPath, content);
        act.Should().ThrowAsync<InvalidOutputDirectoryException>();
    }

    [Test]
    public void WriteFile_WithPathTraversalTilde_ShouldThrowInvalidOutputDirectoryException()
    {
        // Arrange
        var maliciousPath = "~/malicious.txt";
        var content = "malicious content";

        // Act & Assert
        var act = async () => await _fileWriter.WriteFile(maliciousPath, content);
        act.Should().ThrowAsync<InvalidOutputDirectoryException>();
    }

    [Test]
    [TestCase("/etc/passwd")]
    [TestCase("/sys/kernel/notes")]
    [TestCase("/proc/self/environ")]
    [TestCase("/root/.ssh/id_rsa")]
    [TestCase("/var/log/syslog")]
    public void WriteFile_WithLinuxSystemPath_ShouldThrowInvalidOutputDirectoryException(string systemPath)
    {
        // Arrange
        var content = "malicious content";

        // Act & Assert
        var act = async () => await _fileWriter.WriteFile(systemPath, content);
        act.Should().ThrowAsync<InvalidOutputDirectoryException>();
    }

    [Test]
    [TestCase("c:/windows/system32/config.sys")]
    [TestCase("C:/Windows/System32/drivers/etc/hosts")]
    [TestCase("c:/program files/test.txt")]
    [TestCase("C:/Program Files (x86)/test.txt")]
    [TestCase("c:/users/administrator/desktop/test.txt")]
    public void WriteFile_WithWindowsSystemPath_ShouldThrowInvalidOutputDirectoryException(string systemPath)
    {
        // Arrange
        var content = "malicious content";

        // Act & Assert
        var act = async () => await _fileWriter.WriteFile(systemPath, content);
        act.Should().ThrowAsync<InvalidOutputDirectoryException>();
    }

    [Test]
    public async Task WriteFile_WithValidPath_ShouldCreateFile()
    {
        // Arrange
        var validPath = Path.Combine(_testOutputDir, "valid.txt");
        var content = "valid content";

        // Act
        await _fileWriter.WriteFile(validPath, content);

        // Assert
        File.Exists(validPath).Should().BeTrue();
        var actualContent = await File.ReadAllTextAsync(validPath);
        actualContent.Should().Be(content);
    }

    [Test]
    public async Task WriteFile_WithNestedDirectory_ShouldCreateDirectoryAndFile()
    {
        // Arrange
        var nestedPath = Path.Combine(_testOutputDir, "nested", "dir", "file.txt");
        var content = "nested content";

        // Act
        await _fileWriter.WriteFile(nestedPath, content);

        // Assert
        File.Exists(nestedPath).Should().BeTrue();
        var actualContent = await File.ReadAllTextAsync(nestedPath);
        actualContent.Should().Be(content);
    }

    [Test]
    public async Task WriteFile_WhenFileExists_ShouldThrowException()
    {
        // Arrange
        var filePath = Path.Combine(_testOutputDir, "existing.txt");
        await File.WriteAllTextAsync(filePath, "original");

        // Act & Assert
        var act = async () => await _fileWriter.WriteFile(filePath, "new content");
        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage($"*\"{filePath}\"*already exists*");
    }

    [Test]
    public async Task WriteFile_WithOverwriteFlag_ShouldOverwriteExistingFile()
    {
        // Arrange
        var filePath = Path.Combine(_testOutputDir, "overwrite.txt");
        await File.WriteAllTextAsync(filePath, "original");
        var newContent = "overwritten content";

        // Act
        await _fileWriter.WriteFile(filePath, newContent, overwrite: true);

        // Assert
        var actualContent = await File.ReadAllTextAsync(filePath);
        actualContent.Should().Be(newContent);
    }

    [Test]
    public void WriteFile_WithNullOrEmptyPath_ShouldThrowInvalidOutputDirectoryException()
    {
        // Act & Assert
        var actNull = async () => await _fileWriter.WriteFile(null!, "content");
        actNull.Should().ThrowAsync<InvalidOutputDirectoryException>();

        var actEmpty = async () => await _fileWriter.WriteFile("", "content");
        actEmpty.Should().ThrowAsync<InvalidOutputDirectoryException>();

        var actWhitespace = async () => await _fileWriter.WriteFile("   ", "content");
        actWhitespace.Should().ThrowAsync<InvalidOutputDirectoryException>();
    }
}
