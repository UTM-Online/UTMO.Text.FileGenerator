using FluentAssertions;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Logging;
using Moq;
using UTMO.Text.FileGenerator.Attributes;
using UTMO.Text.FileGenerator.Constants;
using UTMO.Text.FileGenerator.Models;
#pragma warning disable CS0618 // Intentional deprecated alias for backward-compatibility coverage
using LegacyTemplatePropertyAttribute = UTMO.Text.FileGenerator.Abstract.Attributes.TemplatePropertyAttribute;
#pragma warning restore CS0618

namespace TestFileGenerator.Core.Tests.Models;

/// <summary>
/// Security tests for TemplateResourceBase to ensure private properties are not exposed to templates.
/// Tests the fix for issue #5: Information Disclosure vulnerability.
/// </summary>
[TestFixture]
public class TemplateResourceBaseSecurityTests
{
    /// <summary>
    /// Helper method to set the Logger property on TemplateResourceBase using reflection
    /// since it's protected internal and not accessible from test assembly.
    /// </summary>
    private static void SetLogger(TemplateResourceBase resource, ILogger logger)
    {
        var loggerProperty = typeof(TemplateResourceBase).GetProperty("Logger", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        loggerProperty?.SetValue(resource, logger);
    }

    /// <summary>
    /// Helper method to set the FeatureManager property on TemplateResourceBase using reflection
    /// since it's protected internal and not accessible from test assembly.
    /// </summary>
    private static void SetFeatureManager(TemplateResourceBase resource, IFeatureManager featureManager)
    {
        var featureManagerProperty = typeof(TemplateResourceBase).GetProperty("FeatureManager",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        featureManagerProperty?.SetValue(resource, featureManager);
    }

    #region Test Resource Classes

    /// <summary>
    /// Test resource with secure opt-in properties using [TemplateProperty] attribute
    /// </summary>
    private class SecureResource : TemplateResourceBase
    {
        [TemplateProperty]
        public string SafePublicProperty { get; set; } = "safe_value";

        public string UnsafePublicProperty { get; set; } = "should_not_expose";

#pragma warning disable CS0414 // Field is assigned but its value is never used - intentional for security testing
        private string _privateProperty = "private_secret";
#pragma warning restore CS0414

        protected string ProtectedProperty { get; set; } = "protected_secret";

        [IgnoreMember]
        [TemplateProperty]
        public string IgnoredProperty { get; set; } = "ignored_value";

        public override string ResourceTypeName => "SecureResource";
        public override string TemplatePath => "/templates/test.liquid";
        public override string OutputExtension => ".txt";
        public override string ResourceName => "test";
    }

    /// <summary>
    /// Test resource attempting to expose non-public property with [TemplateProperty]
    /// </summary>
    private class InvalidResource : TemplateResourceBase
    {
        [TemplateProperty]
        private string PrivateWithAttribute { get; set; } = "invalid";

        public override string ResourceTypeName => "InvalidResource";
        public override string TemplatePath => "/templates/test.liquid";
        public override string OutputExtension => ".txt";
        public override string ResourceName => "test";
    }

    /// <summary>
    /// Test resource with sensitive data that should not be exposed
    /// </summary>
    private class SensitiveDataResource : TemplateResourceBase
    {
        [TemplateProperty]
        public string ServerName { get; set; } = "prod-server";

        // Sensitive properties without [TemplateProperty] - should NOT be exposed
        public string ApiKey { get; set; } = "secret_api_key_12345";
        public string ConnectionString { get; set; } = "Server=prod;Password=secret123";
#pragma warning disable CS0414 // Field is assigned but its value is never used - intentional for security testing
        private string _privatePassword = "private_password_123";
#pragma warning restore CS0414
        protected string ProtectedToken { get; set; } = "protected_token_xyz";

        public override string ResourceTypeName => "SensitiveResource";
        public override string TemplatePath => "/templates/test.liquid";
        public override string OutputExtension => ".txt";
        public override string ResourceName => "test";
    }

    /// <summary>
    /// Legacy resource without any [TemplateProperty] attributes (deprecated behavior)
    /// </summary>
    private class LegacyResource : TemplateResourceBase
    {
        public string PublicProperty { get; set; } = "public";
#pragma warning disable CS0414 // Field is assigned but its value is never used - intentional for security testing
        private string _privateProperty = "private";
#pragma warning restore CS0414

        public override string ResourceTypeName => "LegacyResource";
        public override string TemplatePath => "/templates/test.liquid";
        public override string OutputExtension => ".txt";
        public override string ResourceName => "test";
    }

    /// <summary>
    /// Test resource with nested resources
    /// </summary>
    private class ParentResource : TemplateResourceBase
    {
        [TemplateProperty]
        public string ParentName { get; set; } = "parent";

        [TemplateProperty]
        public SecureResource? Child { get; set; }

        public override string ResourceTypeName => "ParentResource";
        public override string TemplatePath => "/templates/test.liquid";
        public override string OutputExtension => ".txt";
        public override string ResourceName => "test";
    }

    /// <summary>
    /// Test resource that still uses the legacy TemplateProperty attribute namespace.
    /// </summary>
    private class LegacyNamespaceTemplatePropertyResource : TemplateResourceBase
    {
#pragma warning disable CS0618 // Legacy namespace is tested intentionally for backward compatibility
        [LegacyTemplateProperty]
#pragma warning restore CS0618
        public string LegacySafeProperty { get; set; } = "legacy_safe_value";

        public string LegacyUnsafeProperty { get; set; } = "legacy_should_not_expose";

        public override string ResourceTypeName => "LegacyNamespaceTemplatePropertyResource";
        public override string TemplatePath => "/templates/test.liquid";
        public override string OutputExtension => ".txt";
        public override string ResourceName => "test";
    }

    #endregion

    [SetUp]
    public void ResetMigrationLogState()
    {
        var resetMethod = typeof(TemplateResourceBase).GetMethod(
            "ResetMissingTemplatePropertyLogsForTesting",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        resetMethod?.Invoke(null, null);
    }

    [Test]
    public async Task ToTemplateContext_WithTemplatePropertyAttribute_ShouldExposeMarkedPublicProperty()
    {
        // Arrange
        var resource = new SecureResource();

        // Act
        var context = await resource.ToTemplateContext();

        // Assert
        context.Should().ContainKey("SafePublicProperty");
        context["SafePublicProperty"].Should().Be("safe_value");
    }

    [Test]
    public async Task ToTemplateContext_WithLegacyTemplatePropertyNamespace_ShouldStillExposeMarkedPublicProperty()
    {
        // Arrange
        var resource = new LegacyNamespaceTemplatePropertyResource();

        // Act
        var context = await resource.ToTemplateContext();

        // Assert
        context.Should().ContainKey("LegacySafeProperty");
        context["LegacySafeProperty"].Should().Be("legacy_safe_value");
        context.Should().NotContainKey("LegacyUnsafeProperty");
    }

    [Test]
    public async Task ToTemplateContext_WithoutTemplatePropertyAttribute_ShouldNotExposePublicProperty()
    {
        // Arrange
        var resource = new SecureResource();

        // Act
        var context = await resource.ToTemplateContext();

        // Assert
        context.Should().NotContainKey("UnsafePublicProperty");
    }

    [Test]
    public async Task ToTemplateContext_WithPrivateProperty_ShouldStillExposeForBackwardCompatibility()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var resource = new SecureResource();
        SetLogger(resource, mockLogger.Object);

        // Act
        var context = await resource.ToTemplateContext();

        // Assert - private property should still be exposed for backward compatibility (deprecated)
        // Note: With the fix, private fields without explicit get/set are not exposed as they're not properties
        // Only private *properties* would be exposed (which shouldn't exist in practice)
        context.Should().NotContainKey("_privateProperty");
        
        // Since _privateProperty is a field, not a property, no deprecation warning should be logged for it
        // But we should verify no errors occurred
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "No errors should be logged during property enumeration");
    }

    [Test]
    public async Task ToTemplateContext_WithProtectedProperty_ShouldNotExposeByDefault()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var resource = new SecureResource();
        SetLogger(resource, mockLogger.Object);

        // Act
        var context = await resource.ToTemplateContext();

        // Assert - secure default behavior does not expose non-public properties.
        context.Should().NotContainKey("ProtectedProperty");
        
        // Verify migration warning was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("ProtectedProperty") && 
                    v.ToString()!.Contains("will not be exposed") &&
                    v.ToString()!.Contains(FeatureFlags.EnableLegacyNonPublicTemplateProperties)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected migration warning for non-public property without [TemplateProperty]");
    }

    [Test]
    public async Task ToTemplateContext_WithProtectedPropertyAndLegacyFeatureEnabled_ShouldExposeForMigration()
    {
        // Arrange
        var mockFeatureManager = new Mock<IFeatureManager>();
        mockFeatureManager
            .Setup(x => x.IsEnabledAsync(FeatureFlags.EnableLegacyNonPublicTemplateProperties))
            .ReturnsAsync(true);

        var resource = new SecureResource();
        SetFeatureManager(resource, mockFeatureManager.Object);

        // Act
        var context = await resource.ToTemplateContext();

        // Assert
        context.Should().ContainKey("ProtectedProperty");
    }

    [Test]
    public async Task ToTemplateContext_WithIgnoreMemberAttribute_ShouldExcludeProperty()
    {
        // Arrange
        var resource = new SecureResource();

        // Act
        var context = await resource.ToTemplateContext();

        // Assert
        context.Should().NotContainKey("IgnoredProperty");
    }

    [Test]
    public async Task ToTemplateContext_WithNonPublicPropertyAndTemplateAttribute_ShouldNotExpose()
    {
        // Arrange
        var resource = new InvalidResource();

        // Act
        var context = await resource.ToTemplateContext();

        // Assert
        context.Should().NotContainKey("PrivateWithAttribute");
    }

    [Test]
    public async Task ToTemplateContext_SensitiveDataResource_ShouldNotExposeCredentials()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var resource = new SensitiveDataResource();
        SetLogger(resource, mockLogger.Object);

        // Act
        var context = await resource.ToTemplateContext();

        // Assert - Only ServerName should be exposed (has [TemplateProperty])
        context.Should().ContainKey("ServerName");
        context["ServerName"].Should().Be("prod-server");

        // These should NOT be exposed (security risk) - no [TemplateProperty] attribute
        context.Should().NotContainKey("ApiKey");
        context.Should().NotContainKey("ConnectionString");
        context.Should().NotContainKey("_privatePassword");
        
        // Protected property is non-public and should be excluded by default.
        context.Should().NotContainKey("ProtectedToken");
        
        // Verify migration warning was logged for the protected property
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("ProtectedToken") && 
                    v.ToString()!.Contains("will not be exposed") &&
                    v.ToString()!.Contains(FeatureFlags.EnableLegacyNonPublicTemplateProperties)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected migration warning for ProtectedToken property");
        
        // Verify debug migration messages are logged for public properties without [TemplateProperty]
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("ApiKey") && 
                    v.ToString()!.Contains("not marked with [TemplateProperty]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected debug migration message for ApiKey property without [TemplateProperty]");

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("ConnectionString") && 
                    v.ToString()!.Contains("not marked with [TemplateProperty]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected debug migration message for ConnectionString property without [TemplateProperty]");
    }

    [Test]
    public async Task ToTemplateContext_WithNestedSecureResource_ShouldOnlyExposeMarkedProperties()
    {
        // Arrange
        var parent = new ParentResource
        {
            Child = new SecureResource()
        };

        // Act
        var context = await parent.ToTemplateContext();

        // Assert
        context.Should().ContainKey("ParentName");
        context.Should().ContainKey("Child");
        
        var childContext = context["Child"] as Dictionary<string, object>;
        childContext.Should().NotBeNull();
        childContext!.Should().ContainKey("SafePublicProperty");
        childContext.Should().NotContainKey("UnsafePublicProperty");
    }

    [Test]
    public async Task ToTemplateContext_LegacyResource_ShouldNotExposePublicPropertiesWithoutAttribute()
    {
        // Arrange
        var resource = new LegacyResource();

        // Act
        var context = await resource.ToTemplateContext();

        // Assert - Public property without [TemplateProperty] should NOT be exposed
        context.Should().NotContainKey("PublicProperty");
        
        // Private field is not a property with get/set, so it won't be exposed
        context.Should().NotContainKey("_privateProperty");
    }

    [Test]
    public async Task ToTemplateContext_PublicPropertyWithoutAttributeAndLegacyFlagEnabled_ShouldExposeForMigration()
    {
        // Arrange
        var mockFeatureManager = new Mock<IFeatureManager>();
        mockFeatureManager
            .Setup(x => x.IsEnabledAsync(FeatureFlags.EnableLegacyNonPublicTemplateProperties))
            .ReturnsAsync(true);

        var resource = new SecureResource();
        SetFeatureManager(resource, mockFeatureManager.Object);

        // Act
        var context = await resource.ToTemplateContext();

        // Assert - Public property without [TemplateProperty] should ALSO be exposed under legacy flag
        // (restoring the full pre-v2.16 behavior that exposed ALL properties)
        context.Should().ContainKey("UnsafePublicProperty");
        context["UnsafePublicProperty"].Should().Be("should_not_expose");
    }

    [Test]
    public async Task ToTemplateContext_LegacyResource_WithLegacyFlagEnabled_ShouldExposePublicProperties()
    {
        // Arrange - a resource that has not migrated to [TemplateProperty] at all
        var mockFeatureManager = new Mock<IFeatureManager>();
        mockFeatureManager
            .Setup(x => x.IsEnabledAsync(FeatureFlags.EnableLegacyNonPublicTemplateProperties))
            .ReturnsAsync(true);

        var resource = new LegacyResource();
        SetFeatureManager(resource, mockFeatureManager.Object);

        // Act
        var context = await resource.ToTemplateContext();

        // Assert - Public property on legacy (un-migrated) resource should be exposed under legacy flag
        context.Should().ContainKey("PublicProperty");
        context["PublicProperty"].Should().Be("public");
    }

    [Test]
    public async Task ToTemplateContext_PublicPropertyWithoutAttributeAndLegacyFlagEnabled_ShouldLogDeprecationWarning()
    {
        // Arrange
        var mockFeatureManager = new Mock<IFeatureManager>();
        mockFeatureManager
            .Setup(x => x.IsEnabledAsync(FeatureFlags.EnableLegacyNonPublicTemplateProperties))
            .ReturnsAsync(true);

        var mockLogger = new Mock<ILogger>();
        var resource = new SecureResource();
        SetFeatureManager(resource, mockFeatureManager.Object);
        SetLogger(resource, mockLogger.Object);

        // Act
        await resource.ToTemplateContext();

        // Assert - deprecation warning logged for public properties exposed via legacy flag
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("UnsafePublicProperty") &&
                    v.ToString()!.Contains("DEPRECATED") &&
                    v.ToString()!.Contains(FeatureFlags.EnableLegacyNonPublicTemplateProperties)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected deprecation warning for public property exposed via legacy flag");
    }

    [Test]
    public async Task ToTemplateContext_PublicPropertyWithoutAttributeAndLegacyFlagEnabled_ShouldLogDeprecationWarningOnceAcrossMultipleInvocations()
    {
        // Arrange
        var mockFeatureManager = new Mock<IFeatureManager>();
        mockFeatureManager
            .Setup(x => x.IsEnabledAsync(FeatureFlags.EnableLegacyNonPublicTemplateProperties))
            .ReturnsAsync(true);

        var mockLogger = new Mock<ILogger>();
        var resource = new SecureResource();
        SetFeatureManager(resource, mockFeatureManager.Object);
        SetLogger(resource, mockLogger.Object);

        // Act - call twice
        await resource.ToTemplateContext();
        await resource.ToTemplateContext();

        // Assert - warning should only fire once per type+property, not once per invocation
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("UnsafePublicProperty") &&
                    v.ToString()!.Contains("DEPRECATED") &&
                    v.ToString()!.Contains(FeatureFlags.EnableLegacyNonPublicTemplateProperties)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected deprecation warning to be emitted only once per type+property across multiple ToTemplateContext calls");
    }

    [Test]
    public async Task ToTemplateContext_WithoutLogger_ShouldNotThrowException()
    {
        // Arrange
        var resource = new SecureResource(); // No logger set

        // Act
        Func<Task> act = async () => await resource.ToTemplateContext();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task ToTemplateContext_MultiplePropertiesWithMixedAttributes_ShouldOnlyExposeCorrectOnes()
    {
        // Arrange
        var resource = new SecureResource
        {
            SafePublicProperty = "exposed",
            UnsafePublicProperty = "hidden",
        };

        // Act
        var context = await resource.ToTemplateContext();

        // Assert
        context.Should().ContainKey("SafePublicProperty");
        context["SafePublicProperty"].Should().Be("exposed");
        
        context.Should().NotContainKey("UnsafePublicProperty");
        context.Should().NotContainKey("IgnoredProperty");
    }

    [Test]
    public async Task ToTemplateContext_ShouldNotExposeInheritedFrameworkProperties()
    {
        // Arrange
        var resource = new SecureResource();

        // Act
        var context = await resource.ToTemplateContext();

        // Assert - Framework properties should not be exposed
        context.Should().NotContainKey("GetType");
        context.Should().NotContainKey("ToString");
        context.Should().NotContainKey("GetHashCode");
        context.Should().NotContainKey("Equals");
        context.Should().NotContainKey("FeatureManager");
        context.Should().NotContainKey("Logger");
    }

    [Test]
    public async Task ToTemplateContext_NonPublicPropertiesWithoutAttribute_ShouldLogMigrationWarning()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var resource = new SecureResource();
        SetLogger(resource, mockLogger.Object);

        // Act
        await resource.ToTemplateContext();

        // Assert - verify migration warning for excluded non-public properties
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Non-public property") && 
                    v.ToString()!.Contains("will not be exposed") &&
                    v.ToString()!.Contains(FeatureFlags.EnableLegacyNonPublicTemplateProperties) &&
                    v.ToString()!.Contains("ProtectedProperty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected migration warning for protected property exclusion");
        
        // Verify warning includes migration guidance via feature flag
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("enable feature flag") &&
                    v.ToString()!.Contains(FeatureFlags.EnableLegacyNonPublicTemplateProperties)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Expected warning to include migration feature flag guidance");
    }

    [Test]
    public async Task ToTemplateContext_PublicPropertiesWithoutAttribute_ShouldLogDebugMessage()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var resource = new SecureResource();
        SetLogger(resource, mockLogger.Object);

        // Act
        await resource.ToTemplateContext();

        // Assert - Verify that debug messages are logged for public properties without [TemplateProperty]
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("UnsafePublicProperty") && 
                    v.ToString()!.Contains("not marked with [TemplateProperty]") &&
                    v.ToString()!.Contains("will not be exposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected debug message for public property without [TemplateProperty]");
    }

    [Test]
    public async Task ToTemplateContext_PublicPropertiesWithoutAttribute_ShouldLogOnceAcrossMultipleInvocations()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var resource = new SecureResource();
        SetLogger(resource, mockLogger.Object);

        // Act
        await resource.ToTemplateContext();
        await resource.ToTemplateContext();

        // Assert - migration log should only be emitted once per type/property
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("UnsafePublicProperty") &&
                    v.ToString()!.Contains("not marked with [TemplateProperty]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected migration debug message to be emitted only once for repeated ToTemplateContext calls");
    }
}








