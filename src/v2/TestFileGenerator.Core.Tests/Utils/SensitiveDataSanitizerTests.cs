using FluentAssertions;
using UTMO.Text.FileGenerator.Utils;

namespace TestFileGenerator.Core.Tests.Utils;

/// <summary>
/// Tests for the SensitiveDataSanitizer utility.
/// Verifies that sensitive data is properly redacted when sanitizing template contexts.
/// </summary>
[TestFixture]
public class SensitiveDataSanitizerTests
{
    [Test]
    public void Sanitize_WithPasswordField_ShouldRedact()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            { "LoginName", "admin" },
            { "Password", "SuperSecret123!" }
        };

        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(context);

        // Assert
        sanitized.Should().NotBeNull();
        sanitized!["LoginName"].Should().Be("admin"); // "LoginName" is not a sensitive keyword
        sanitized["Password"].Should().Be("***REDACTED***");
    }

    [Test]
    public void Sanitize_WithApiKeyField_ShouldRedact()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            { "ApiKey", "sk-1234567890abcdef" },
            { "ServiceName", "MyService" }
        };

        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(context);

        // Assert
        sanitized!["ApiKey"].Should().Be("***REDACTED***");
        sanitized["ServiceName"].Should().Be("MyService");
    }

    [Test]
    public void Sanitize_WithConnectionString_ShouldRedact()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            { "ConnectionString", "Server=prod;User=sa;Password=secret" }
        };

        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(context);

        // Assert
        sanitized!["ConnectionString"].Should().Be("***REDACTED***");
    }

    [Test]
    public void Sanitize_WithMultipleSensitiveFields_ShouldRedactAll()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            { "Database", "prod_db" },
            { "DbPassword", "mysecret" },
            { "ApiSecret", "sk-xyz" },
            { "AuthToken", "bearer-token-xyz" },
            { "PublicUrl", "https://example.com" }
        };

        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(context);

        // Assert
        sanitized!["Database"].Should().Be("***REDACTED***"); // "Database" is a sensitive keyword
        sanitized["DbPassword"].Should().Be("***REDACTED***");
        sanitized["ApiSecret"].Should().Be("***REDACTED***");
        sanitized["AuthToken"].Should().Be("***REDACTED***");
        sanitized["PublicUrl"].Should().Be("https://example.com");
    }

    [Test]
    public void Sanitize_WithNullInput_ShouldReturnNull()
    {
        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(null);

        // Assert
        sanitized.Should().BeNull();
    }

    [Test]
    public void Sanitize_WithEmptyDictionary_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var context = new Dictionary<string, object>();

        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(context);

        // Assert
        sanitized.Should().BeEmpty();
    }

    [Test]
    public void GetContextKeys_ShouldReturnOnlyKeys()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            { "Field1", "value1" },
            { "SecretKey", "super_secret" },
            { "Field3", 123 }
        };

        // Act
        var keys = SensitiveDataSanitizer.GetContextKeys(context);

        // Assert
        keys.Should().HaveCount(3);
        keys.Should().Contain("Field1");
        keys.Should().Contain("SecretKey");
        keys.Should().Contain("Field3");
    }

    [Test]
    public void GetContextKeys_WithNullInput_ShouldReturnEmptyList()
    {
        // Act
        var keys = SensitiveDataSanitizer.GetContextKeys(null);

        // Assert
        keys.Should().BeEmpty();
    }

    [Test]
    public void Sanitize_DoesNotModifyOriginal()
    {
        // Arrange
        var original = new Dictionary<string, object>
        {
            { "Password", "secret123" },
            { "Username", "admin" }
        };
        var originalCopy = new Dictionary<string, object>(original);

        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(original);

        // Assert
        original.Should().Equal(originalCopy, "Original dictionary should not be modified");
        sanitized!["Password"].Should().Be("***REDACTED***");
        original["Password"].Should().Be("secret123");
    }

    [Test]
    public void Sanitize_WithCaseInsensitiveSensitiveKeywords_ShouldRedact()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            { "PASSWORD", "secret1" },
            { "PassWord", "secret2" },
            { "password", "secret3" },
            { "pAsSwOrD", "secret4" }
        };

        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(context);

        // Assert
        sanitized!["PASSWORD"].Should().Be("***REDACTED***");
        sanitized["PassWord"].Should().Be("***REDACTED***");
        sanitized["password"].Should().Be("***REDACTED***");
        sanitized["pAsSwOrD"].Should().Be("***REDACTED***");
    }

    [Test]
    public void Sanitize_WithBearerTokenString_ShouldRedact()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            { "Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." }
        };

        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(context);

        // Assert
        sanitized!["Authorization"].Should().Be("***REDACTED***");
    }

    [Test]
    public void Sanitize_WithConnectionStringPattern_ShouldRedact()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            { "ConnectionString", "Server=localhost;User=admin;Password=secret;" }
        };

        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(context);

        // Assert
        sanitized!["ConnectionString"].Should().Be("***REDACTED***");
    }

    [Test]
    public void Sanitize_WithCredentialPattern_ShouldRedact()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            { "DatabaseUrl", "user:password@localhost:5432" }
        };

        // Act
        var sanitized = SensitiveDataSanitizer.Sanitize(context);

        // Assert
        sanitized!["DatabaseUrl"].Should().Be("***REDACTED***");
    }
}


