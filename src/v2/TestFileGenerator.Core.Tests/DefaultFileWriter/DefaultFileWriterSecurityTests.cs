using FluentAssertions;
using UTMO.Text.FileGenerator.Abstract;
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
    public async Task WriteFile_WithPathTraversalDoubleDot_ShouldThrowInvalidOutputDirectoryException()
    {
        // Arrange
        var maliciousPath = Path.Combine(_testOutputDir, "..", "malicious.txt");
        var content = "malicious content";

        // Act & Assert
        var act = async () => await _fileWriter.WriteFile(maliciousPath, content);
        await act.Should().ThrowAsync<InvalidOutputDirectoryException>();
    }

    [Test]
    public async Task WriteFile_WithPathTraversalTilde_ShouldThrowInvalidOutputDirectoryException()
    {
        // Arrange
        var maliciousPath = "~/malicious.txt";
        var content = "malicious content";

        // Act & Assert
        var act = async () => await _fileWriter.WriteFile(maliciousPath, content);
        await act.Should().ThrowAsync<InvalidOutputDirectoryException>();
    }

    [Test]
    public async Task WriteFile_WithTildePrefixedFilenameNotFollowedBySeparator_ShouldCreateFile()
    {
        // A tilde at the start of a plain filename (e.g. "~backup.txt") is a legitimate
        // Windows/Linux naming convention and must NOT be rejected.
        // Arrange
        var validPath = Path.Join(_testOutputDir, "~backup.txt");
        var content = "backup content";

        // Act
        await _fileWriter.WriteFile(validPath, content);

        // Assert
        File.Exists(validPath).Should().BeTrue();
        (await File.ReadAllTextAsync(validPath)).Should().Be(content);
    }

    [Test]
    [TestCase("/etc/passwd")]
    [TestCase("/sys/kernel/notes")]
    [TestCase("/proc/self/environ")]
    [TestCase("/root/.ssh/id_rsa")]
    [TestCase("/var/log/syslog")]
    [TestCase("/Etc/passwd")]
    [TestCase("/ETC/passwd")]
    [TestCase("/etc/../etc/passwd")]
    public async Task WriteFile_WithLinuxSystemPath_ShouldThrowInvalidOutputDirectoryException(string systemPath)
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Linux system path validation test.");
        }

        // Arrange
        var content = "malicious content";

        // Act & Assert
        var act = async () => await _fileWriter.WriteFile(systemPath, content);
        await act.Should().ThrowAsync<InvalidOutputDirectoryException>();
    }

    [Test]
    [TestCase("c:/windows/system32/config.sys")]
    [TestCase("C:/Windows/System32/drivers/etc/hosts")]
    [TestCase("c:/program files/test.txt")]
    [TestCase("C:/Program Files (x86)/test.txt")]
    [TestCase("c:/users/administrator/desktop/test.txt")]
    [TestCase(@"\\?\c:\windows\system32\config.sys")]
    public async Task WriteFile_WithWindowsSystemPath_ShouldThrowInvalidOutputDirectoryException(string systemPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Windows system path validation test.");
        }

        // Arrange
        var content = "malicious content";

        // Act & Assert
        var act = async () => await _fileWriter.WriteFile(systemPath, content);
        await act.Should().ThrowAsync<InvalidOutputDirectoryException>();
    }

    [Test]
    public async Task WriteFile_WithLegitimatePathContainingBlockedSubstring_ShouldCreateFile()
    {
        // Arrange
        var validPath = Path.Join(_testOutputDir, "project_etc", "proc_data", "file.txt");
        var content = "valid content";

        // Act
        await _fileWriter.WriteFile(validPath, content);

        // Assert
        File.Exists(validPath).Should().BeTrue();
        var actualContent = await File.ReadAllTextAsync(validPath);
        actualContent.Should().Be(content);
    }

    [Test]
    [TestCase("etc")]
    [TestCase("proc")]
    [TestCase("sys")]
    public async Task WriteFile_WithNonRootDirectoryMatchingBlockedName_ShouldCreateFile(string directoryName)
    {
        // Arrange
        var validPath = Path.Join(_testOutputDir, directoryName.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "file.txt");
        var content = "valid content";

        // Act
        await _fileWriter.WriteFile(validPath, content);

        // Assert
        File.Exists(validPath).Should().BeTrue();
        var actualContent = await File.ReadAllTextAsync(validPath);
        actualContent.Should().Be(content);
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
    public async Task WriteFile_WithNullOrEmptyPath_ShouldThrowInvalidOutputDirectoryException()
    {
        // Act & Assert
        var actNull = async () => await _fileWriter.WriteFile(null!, "content");
        await actNull.Should().ThrowAsync<InvalidOutputDirectoryException>();

        var actEmpty = async () => await _fileWriter.WriteFile("", "content");
        await actEmpty.Should().ThrowAsync<InvalidOutputDirectoryException>();

        var actWhitespace = async () => await _fileWriter.WriteFile("   ", "content");
        await actWhitespace.Should().ThrowAsync<InvalidOutputDirectoryException>();
    }

    [Test]
    public void BuildWindowsSystemPathPrefixesFromCandidates_WithMixedValidAndInvalidCandidates_ShouldSkipInvalidCandidates()
    {
        // Arrange
        var validPath = Path.Join(Path.GetTempPath(), $"PrefixCandidate_{Guid.NewGuid():N}");
        var invalidPath = $"invalid{Path.DirectorySeparatorChar}\0candidate";
        var expectedPrefix = Path.GetFullPath(validPath)
            .Replace('\\', '/')
            .TrimEnd('/', '\\') + "/";

        string[] prefixes = Array.Empty<string>();

        // Act
        var act = () =>
            prefixes = UTMO.Text.FileGenerator.DefaultFileWriter.DefaultFileWriter.BuildWindowsSystemPathPrefixesFromCandidates(
                new string?[] { validPath, invalidPath, null, string.Empty, "   " });

        // Assert
        act.Should().NotThrow();
        prefixes.Should().ContainSingle(p => p.Equals(expectedPrefix, StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void BuildWindowsSystemPathPrefixesFromCandidates_WithOnlyInvalidCandidates_ShouldReturnEmptySet()
    {
        // Arrange
        var invalidPath = "bad\0path";

        // Act
        var prefixes = UTMO.Text.FileGenerator.DefaultFileWriter.DefaultFileWriter.BuildWindowsSystemPathPrefixesFromCandidates(
            new string?[] { invalidPath, null, string.Empty, "   " });

        // Assert
        prefixes.Should().BeEmpty();
    }

    [Test]
    public async Task WriteFile_WhenFileCreatedAtomically_ShouldNotOverwriteWhenOverwriteIsFalse()
    {
        // Arrange -- pre-create the file to simulate a race where the file already exists
        var filePath = Path.Join(_testOutputDir, "atomic_test.txt");
        await File.WriteAllTextAsync(filePath, "pre-existing content");

        // Act & Assert -- atomic FileMode.CreateNew must detect the existing file
        var act = async () => await _fileWriter.WriteFile(filePath, "new content", overwrite: false);
        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage($"*\"{filePath}\"*already exists*");

        // Original content must be untouched
        var actualContent = await File.ReadAllTextAsync(filePath);
        actualContent.Should().Be("pre-existing content");
    }

    [Test]
    public async Task WriteFile_ConcurrentAttempts_OnlyFirstShouldSucceed()
    {
        // Arrange -- use a Barrier so all tasks attempt the create at the same instant
        var filePath = Path.Join(_testOutputDir, "concurrent.txt");
        const int concurrency = 8;
        var successes = 0;
        var failures = 0;
        using var barrier = new Barrier(concurrency);

        // Act -- fire concurrent writes; the Barrier aligns all starts to maximise the race window
        var tasks = Enumerable.Range(0, concurrency).Select(i => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            try
            {
                await _fileWriter.WriteFile(filePath, $"content from task {i}", overwrite: false);
                Interlocked.Increment(ref successes);
            }
            catch (ApplicationException ex) when (ex.Message.Contains("already exists"))
            {
                Interlocked.Increment(ref failures);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert -- exactly one writer must have won the race; all others must have failed
        successes.Should().Be(1);
        failures.Should().Be(concurrency - 1);
        File.Exists(filePath).Should().BeTrue();
    }

    [Test]
    public async Task WriteFile_OverwriteTrue_ShouldAlwaysSucceedEvenWhenFileExists()
    {
        // Arrange
        var filePath = Path.Join(_testOutputDir, "overwrite_atomic.txt");
        await File.WriteAllTextAsync(filePath, "original");

        // Act
        await _fileWriter.WriteFile(filePath, "updated", overwrite: true);

        // Assert
        var actualContent = await File.ReadAllTextAsync(filePath);
        actualContent.Should().Be("updated");
    }

    [Test]
    public async Task WriteEmbeddedResource_WhenOutputFileAlreadyExists_ShouldThrowApplicationException()
    {
        // Arrange -- pre-create the output file so WriteEmbeddedResource finds it occupied
        var outputPath = Path.Join(_testOutputDir, "embedded_existing.txt");
        await File.WriteAllTextAsync(outputPath, "pre-existing content");

        // Act & Assert -- atomic FileMode.CreateNew must reject the existing file
        var act = async () => await _fileWriter.WriteEmbeddedResource(
            "test-embedded-resource.txt",
            outputPath,
            EmbeddedResourceType.PowerShell,
            typeof(DefaultFileWriterSecurityTests));

        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage($"*\"{outputPath}\"*already exists*");

        // Original content must be untouched
        var actualContent = await File.ReadAllTextAsync(outputPath);
        actualContent.Should().Be("pre-existing content");
    }

    [Test]
    public async Task WriteEmbeddedResource_WhenOutputFileDoesNotExist_ShouldWriteResourceContent()
    {
        // Arrange
        var outputPath = Path.Join(_testOutputDir, "embedded_new.txt");

        // Act
        await _fileWriter.WriteEmbeddedResource(
            "test-embedded-resource.txt",
            outputPath,
            EmbeddedResourceType.PowerShell,
            typeof(DefaultFileWriterSecurityTests));

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var actualContent = await File.ReadAllTextAsync(outputPath);
        actualContent.Should().Contain("test embedded resource");
    }
}
