using MarkdownStructureChunker.Core.Configuration;
using Xunit;

namespace MarkdownStructureChunker.Tests.Configuration;

/// <summary>
/// Tests for custom keyword functionality in ChunkerConfiguration.
/// </summary>
public class CustomKeywordConfigurationTests
{
    [Fact]
    public void CustomKeywords_DefaultValue_IsEmpty()
    {
        // Arrange & Act
        var config = new ChunkerConfiguration();

        // Assert
        Assert.NotNull(config.CustomKeywords);
        Assert.Empty(config.CustomKeywords);
    }

    [Fact]
    public void SectionKeywordMappings_DefaultValue_IsEmpty()
    {
        // Arrange & Act
        var config = new ChunkerConfiguration();

        // Assert
        Assert.NotNull(config.SectionKeywordMappings);
        Assert.Empty(config.SectionKeywordMappings);
    }

    [Fact]
    public void PrioritizeCustomKeywords_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var config = new ChunkerConfiguration();

        // Assert
        Assert.True(config.PrioritizeCustomKeywords);
    }

    [Fact]
    public void InheritParentKeywords_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var config = new ChunkerConfiguration();

        // Assert
        Assert.False(config.InheritParentKeywords);
    }

    [Fact]
    public void CustomKeywords_CanBeSet()
    {
        // Arrange
        var keywords = new List<string> { "project", "documentation", "api" };
        var config = new ChunkerConfiguration();

        // Act
        config.CustomKeywords = keywords;

        // Assert
        Assert.Equal(keywords, config.CustomKeywords);
    }

    [Fact]
    public void SectionKeywordMappings_CanBeSet()
    {
        // Arrange
        var mappings = new Dictionary<string, IReadOnlyList<string>>
        {
            { "^# Introduction", new List<string> { "intro", "overview" } },
            { "API.*", new List<string> { "api", "endpoint", "rest" } }
        };
        var config = new ChunkerConfiguration();

        // Act
        config.SectionKeywordMappings = mappings;

        // Assert
        Assert.Equal(mappings, config.SectionKeywordMappings);
    }

    [Fact]
    public void Validate_WithValidCustomKeywords_DoesNotThrow()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            CustomKeywords = new List<string> { "valid", "keywords", "here" }
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithEmptyCustomKeyword_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            CustomKeywords = new List<string> { "valid", "", "keywords" }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("CustomKeywords cannot contain null or empty values", exception.Message);
    }

    [Fact]
    public void Validate_WithNullCustomKeyword_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            CustomKeywords = new List<string> { "valid", null!, "keywords" }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("CustomKeywords cannot contain null or empty values", exception.Message);
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyCustomKeyword_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            CustomKeywords = new List<string> { "valid", "   ", "keywords" }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("CustomKeywords cannot contain null or empty values", exception.Message);
    }

    [Fact]
    public void Validate_WithValidSectionMappings_DoesNotThrow()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            SectionKeywordMappings = new Dictionary<string, IReadOnlyList<string>>
            {
                { "^# Introduction", new List<string> { "intro", "overview" } },
                { "API.*", new List<string> { "api", "endpoint" } }
            }
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithEmptyPatternKey_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            SectionKeywordMappings = new Dictionary<string, IReadOnlyList<string>>
            {
                { "", new List<string> { "keywords" } }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("SectionKeywordMappings cannot contain null or empty pattern keys", exception.Message);
    }

    [Fact]
    public void Validate_WithInvalidRegexPattern_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            SectionKeywordMappings = new Dictionary<string, IReadOnlyList<string>>
            {
                { "[invalid regex", new List<string> { "keywords" } }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("SectionKeywordMappings contains invalid regex pattern", exception.Message);
    }

    [Fact]
    public void Validate_WithEmptyKeywordInMapping_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            SectionKeywordMappings = new Dictionary<string, IReadOnlyList<string>>
            {
                { "valid.*pattern", new List<string> { "valid", "", "keywords" } }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("contains null or empty keyword values", exception.Message);
    }

    [Fact]
    public void CreateWithCustomKeywords_WithValidKeywords_CreatesConfiguration()
    {
        // Arrange
        var keywords = new List<string> { "project", "documentation", "api" };

        // Act
        var config = ChunkerConfiguration.CreateWithCustomKeywords(keywords);

        // Assert
        Assert.Equal(keywords, config.CustomKeywords);
        Assert.True(config.PrioritizeCustomKeywords);
        Assert.True(config.ExtractKeywords);
        Assert.Equal(10, config.MaxKeywordsPerChunk); // Math.Max(10, 3 + 5) = 10
    }

    [Fact]
    public void CreateWithCustomKeywords_WithSectionMappings_CreatesConfiguration()
    {
        // Arrange
        var keywords = new List<string> { "project" };
        var mappings = new Dictionary<string, IReadOnlyList<string>>
        {
            { "API.*", new List<string> { "api", "rest" } }
        };

        // Act
        var config = ChunkerConfiguration.CreateWithCustomKeywords(keywords, mappings);

        // Assert
        Assert.Equal(keywords, config.CustomKeywords);
        Assert.Equal(mappings, config.SectionKeywordMappings);
    }

    [Fact]
    public void CreateWithCustomKeywords_WithInheritance_CreatesConfiguration()
    {
        // Arrange
        var keywords = new List<string> { "project" };

        // Act
        var config = ChunkerConfiguration.CreateWithCustomKeywords(keywords, inheritParentKeywords: true);

        // Assert
        Assert.Equal(keywords, config.CustomKeywords);
        Assert.True(config.InheritParentKeywords);
    }

    [Fact]
    public void CreateWithCustomKeywords_WithNullKeywords_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ChunkerConfiguration.CreateWithCustomKeywords(null!));
    }

    [Fact]
    public void CreateForDocumentMapping_WithValidKeywords_CreatesConfiguration()
    {
        // Arrange
        var keywords = new List<string> { "project", "cross-ref" };

        // Act
        var config = ChunkerConfiguration.CreateForDocumentMapping(keywords);

        // Assert
        Assert.Equal(keywords, config.CustomKeywords);
        Assert.True(config.InheritParentKeywords);
        Assert.True(config.PrioritizeCustomKeywords);
        Assert.True(config.ExtractKeywords);
        Assert.Equal(15, config.MaxKeywordsPerChunk);
        Assert.True(config.IncludeHeadingHierarchy);
        Assert.True(config.PreserveStructure);
        Assert.True(config.CalculateOffsets);
    }

    [Fact]
    public void CreateForDocumentMapping_WithNullKeywords_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ChunkerConfiguration.CreateForDocumentMapping(null!));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PrioritizeCustomKeywords_CanBeSetToAnyValue(bool value)
    {
        // Arrange
        var config = new ChunkerConfiguration();

        // Act
        config.PrioritizeCustomKeywords = value;

        // Assert
        Assert.Equal(value, config.PrioritizeCustomKeywords);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InheritParentKeywords_CanBeSetToAnyValue(bool value)
    {
        // Arrange
        var config = new ChunkerConfiguration();

        // Act
        config.InheritParentKeywords = value;

        // Assert
        Assert.Equal(value, config.InheritParentKeywords);
    }

    [Fact]
    public void Validate_WithComplexValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            CustomKeywords = new List<string> { "project", "api", "documentation" },
            SectionKeywordMappings = new Dictionary<string, IReadOnlyList<string>>
            {
                { "^# Introduction", new List<string> { "intro", "overview" } },
                { "API.*Reference", new List<string> { "api", "endpoint", "rest" } },
                { @"\d+\.\d+", new List<string> { "version", "release" } }
            },
            PrioritizeCustomKeywords = false,
            InheritParentKeywords = true,
            MaxKeywordsPerChunk = 20
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }
}

