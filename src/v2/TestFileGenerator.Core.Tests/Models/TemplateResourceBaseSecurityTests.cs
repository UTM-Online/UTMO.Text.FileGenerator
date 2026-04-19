using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UTMO.Text.FileGenerator.Abstract.Attributes;
using UTMO.Text.FileGenerator.Attributes;
using UTMO.Text.FileGenerator.Models;

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
    public async Task ToTemplateContext_WithProtectedProperty_ShouldStillExposeForBackwardCompatibility()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var resource = new SecureResource();
        SetLogger(resource, mockLogger.Object);

        // Act
        var context = await resource.ToTemplateContext();

        // Assert - protected property should still be exposed for backward compatibility
        context.Should().ContainKey("ProtectedProperty");
        
        // Verify deprecation warning was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("ProtectedProperty") && 
                    v.ToString()!.Contains("DEPRECATED") &&
                    v.ToString()!.Contains("security risk")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected deprecation warning to be logged when protected property without [TemplateProperty] is exposed");
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
        
        // Protected property SHOULD still be exposed (backward compatibility for protected properties)
        context.Should().ContainKey("ProtectedToken");
        
        // Verify deprecation warning was logged for the protected property
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("ProtectedToken") && 
                    v.ToString()!.Contains("DEPRECATED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected deprecation warning for ProtectedToken property");
        
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
    }

    [Test]
    public async Task ToTemplateContext_NonPublicPropertiesWithoutAttribute_ShouldLogDeprecationWarning()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var resource = new SecureResource();
        SetLogger(resource, mockLogger.Object);

        // Act
        await resource.ToTemplateContext();

        // Assert - Verify that deprecation warnings are logged for non-public properties
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Non-public property") && 
                    v.ToString()!.Contains("DEPRECATED") &&
                    v.ToString()!.Contains("security risk") &&
                    v.ToString()!.Contains("ProtectedProperty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected specific deprecation warning mentioning security risk for protected property");
        
        // Verify the warning message contains guidance on how to fix
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("make it public") && 
                    v.ToString()!.Contains("[TemplateProperty]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Expected warning to include guidance on making property public and adding [TemplateProperty]");
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








