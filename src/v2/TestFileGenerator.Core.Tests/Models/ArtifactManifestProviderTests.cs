using FluentAssertions;
using Newtonsoft.Json;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests.Models;

/// <summary>
/// Unit tests for <see cref="ArtifactManifestProvider"/> (Manifest v2 phase P3, gaps G4/G5):
/// reads a self-contained Manifest Package written by another project's generation run.
/// </summary>
[TestFixture]
public class ArtifactManifestProviderTests
{
    private string _tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ArtifactManifestProviderTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private void WritePackage(string environment, params (string Subject, string? ParentSubject, string ResourceTypeName, string ResourceName, object Manifest)[] entries)
    {
        var package = new
        {
            SchemaVersion = 1,
            ProviderKind = "local",
            Environment = environment,
            GeneratedAtUtc = DateTime.UtcNow,
            Entries = entries.Select(e => new
            {
                e.Subject,
                e.ParentSubject,
                e.ResourceTypeName,
                e.ResourceName,
                ManifestFile = $"{e.ResourceTypeName}.Manifest.json",
                e.Manifest,
            }),
        };

        File.WriteAllText(Path.Combine(_tempDirectory, "manifest-package.json"), JsonConvert.SerializeObject(package, Formatting.Indented));
    }

    [Test]
    public void Constructor_WhenPackageFileMissing_Throws()
    {
        var act = () => new ArtifactManifestProvider(_tempDirectory);

        act.Should().Throw<FileNotFoundException>();
    }

    [Test]
    public void ProviderKind_IsArtifact()
    {
        WritePackage("Production");

        new ArtifactManifestProvider(_tempDirectory).ProviderKind.Should().Be("artifact");
    }

    [Test]
    public void TryResolveBySubject_WithMatchingEnvironmentAndSubject_ReturnsManifest()
    {
        WritePackage("Production", ("BaseConfig", null, "TypeA", "ResourceA", new { DependsOn = "Network" }));
        var provider = new ArtifactManifestProvider(_tempDirectory);
        var scope = GenerationScope.ForEnvironment("Production");

        provider.TryResolveBySubject(scope, "BaseConfig", null, "DependsOn", out var value).Should().BeTrue();
        value!.ToString().Should().Be("Network");
    }

    [Test]
    public void TryResolveBySubject_WithMismatchedEnvironment_ReturnsFalse()
    {
        WritePackage("Production", ("BaseConfig", null, "TypeA", "ResourceA", new { DependsOn = "Network" }));
        var provider = new ArtifactManifestProvider(_tempDirectory);
        var scope = GenerationScope.ForEnvironment("Development");

        provider.TryResolveBySubject(scope, "BaseConfig", null, "DependsOn", out _).Should().BeFalse();
    }

    [Test]
    public void HasManifestBySubject_WithParentSubject_Distinguishes()
    {
        WritePackage(
            "Production",
            ("Child", "Parent", "TypeA", "ResourceA", new { Value = "scoped" }),
            ("Child", null, "TypeB", "ResourceB", new { Value = "unscoped" }));
        var provider = new ArtifactManifestProvider(_tempDirectory);
        var scope = GenerationScope.ForEnvironment("Production");

        provider.HasManifestBySubject(scope, "Child", "Parent").Should().BeTrue();
        provider.TryResolveBySubject(scope, "Child", "Parent", "Value", out var scopedValue).Should().BeTrue();
        scopedValue!.ToString().Should().Be("scoped");

        provider.TryResolveBySubject(scope, "Child", null, "Value", out var unscopedValue).Should().BeTrue();
        unscopedValue!.ToString().Should().Be("unscoped");
    }

    [Test]
    public void TryResolveProperty_LegacyLookup_ResolvesByResourceTypeAndName()
    {
        WritePackage("Production", ("BaseConfig", null, "TypeA", "ResourceA", new { Value = "legacy" }));
        var provider = new ArtifactManifestProvider(_tempDirectory);
        var scope = GenerationScope.ForEnvironment("Production");

        provider.TryResolveProperty(scope, "TypeA", "ResourceA", "Value", out var value).Should().BeTrue();
        value!.ToString().Should().Be("legacy");
    }

    [Test]
    public void StoreManifest_Throws_ProviderIsReadOnly()
    {
        WritePackage("Production");
        var provider = new ArtifactManifestProvider(_tempDirectory);
        var scope = GenerationScope.ForEnvironment("Production");

        var act = () => provider.StoreManifest(scope, "TypeA", "ResourceA", null);

        act.Should().Throw<NotSupportedException>();
    }

    [Test]
    public void StoreManifestBySubject_Throws_ProviderIsReadOnly()
    {
        WritePackage("Production");
        var provider = new ArtifactManifestProvider(_tempDirectory);
        var scope = GenerationScope.ForEnvironment("Production");

        var act = () => provider.StoreManifestBySubject(scope, "Subject", null, null);

        act.Should().Throw<NotSupportedException>();
    }
}
