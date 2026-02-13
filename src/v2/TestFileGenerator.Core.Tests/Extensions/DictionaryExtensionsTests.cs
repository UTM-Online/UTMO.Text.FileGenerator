using FluentAssertions;
using Moq;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.Extensions;

namespace TestFileGenerator.Core.Tests.Extensions;

/// <summary>
/// Tests for DictionaryExtensions helper methods.
/// </summary>
[TestFixture]
public class DictionaryExtensionsTests
{
    [Test]
    public void Merge_WithNonOverlappingKeys_ShouldCombineDictionaries()
    {
        // Arrange
        var dict1 = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        
        var dict2 = new Dictionary<string, object>
        {
            { "key3", "value3" },
            { "key4", "value4" }
        };

        // Act
        var result = dict1.Merge(dict2);

        // Assert
        result.Should().HaveCount(4);
        result["key1"].Should().Be("value1");
        result["key2"].Should().Be("value2");
        result["key3"].Should().Be("value3");
        result["key4"].Should().Be("value4");
    }

    [Test]
    public void Merge_WithDuplicateKey_ShouldThrowLocalContextKeyExistsException()
    {
        // Arrange
        var dict1 = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "duplicate", "original" }
        };
        
        var dict2 = new Dictionary<string, object>
        {
            { "key2", "value2" },
            { "duplicate", "new" }
        };

        // Act
        var act = () => dict1.Merge(dict2);

        // Assert
        act.Should().Throw<LocalContextKeyExistsException>()
            .Which.Key.Should().Be("duplicate");
    }

    [Test]
    public void Merge_WithEmptySourceDictionary_ShouldReturnOriginal()
    {
        // Arrange
        var dict1 = new Dictionary<string, object>
        {
            { "key1", "value1" }
        };
        
        var dict2 = new Dictionary<string, object>();

        // Act
        var result = dict1.Merge(dict2);

        // Assert
        result.Should().HaveCount(1);
        result["key1"].Should().Be("value1");
    }

    [Test]
    public void AddOrUpdate_WithNewKey_ShouldAddEntry()
    {
        // Arrange
        var dict = new Dictionary<string, string>
        {
            { "existing", "value" }
        };

        // Act
        var result = dict.AddOrUpdate("newKey", "newValue");

        // Assert
        result.Should().BeSameAs(dict);
        dict.Should().ContainKey("newKey");
        dict["newKey"].Should().Be("newValue");
    }

    [Test]
    public void AddOrUpdate_WithExistingKey_ShouldUpdateEntry()
    {
        // Arrange
        var dict = new Dictionary<string, string>
        {
            { "existing", "oldValue" }
        };

        // Act
        var result = dict.AddOrUpdate("existing", "newValue");

        // Assert
        result.Should().BeSameAs(dict);
        dict["existing"].Should().Be("newValue");
    }

    [Test]
    public async Task ToTemplateContext_WithValidModels_ShouldConvertToDictionaries()
    {
        // Arrange
        var mockModel1 = new Mock<ITemplateModel>();
        mockModel1.Setup(m => m.ToTemplateContext())
            .ReturnsAsync(new Dictionary<string, object> { { "prop1", "value1" } });
        
        var mockModel2 = new Mock<ITemplateModel>();
        mockModel2.Setup(m => m.ToTemplateContext())
            .ReturnsAsync(new Dictionary<string, object> { { "prop2", "value2" } });

        var dict = new Dictionary<string, ITemplateModel>
        {
            { "model1", mockModel1.Object },
            { "model2", mockModel2.Object }
        };

        // Act
        var result = await dict.ToTemplateContext();

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().ContainKey("prop1");
        result[1].Should().ContainKey("prop2");
    }

    [Test]
    public void JoinDictionary_WithNonOverlappingKeys_ShouldCombine()
    {
        // Arrange
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "value1" }
        };
        
        var dict2 = new Dictionary<string, string>
        {
            { "key2", "value2" }
        };

        // Act
        var result = dict1.JoinDictionary(dict2);

        // Assert
        result.Should().BeSameAs(dict1);
        dict1.Should().HaveCount(2);
        dict1["key1"].Should().Be("value1");
        dict1["key2"].Should().Be("value2");
    }

    [Test]
    public void JoinDictionary_WithDuplicateKeys_ShouldUpdateWithLatest()
    {
        // Arrange
        var dict1 = new Dictionary<string, string>
        {
            { "key1", "oldValue" },
            { "key2", "value2" }
        };
        
        var dict2 = new Dictionary<string, string>
        {
            { "key1", "newValue" },
            { "key3", "value3" }
        };

        // Act
        var result = dict1.JoinDictionary(dict2);

        // Assert
        result.Should().BeSameAs(dict1);
        dict1.Should().HaveCount(3);
        dict1["key1"].Should().Be("newValue"); // Updated
        dict1["key2"].Should().Be("value2"); // Unchanged
        dict1["key3"].Should().Be("value3"); // Added
    }
}
