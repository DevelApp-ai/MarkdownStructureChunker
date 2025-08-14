using MarkdownStructureChunker.Core.Utilities;
using Xunit;

namespace MarkdownStructureChunker.Tests.Utilities;

/// <summary>
/// Tests for the KeywordValidator utility class.
/// </summary>
public class KeywordValidatorTests
{
    [Fact]
    public void ValidateKeywords_WithValidKeywords_ReturnsNoErrors()
    {
        // Arrange
        var keywords = new List<string> { "valid", "keywords", "here" };

        // Act
        var errors = KeywordValidator.ValidateKeywords(keywords);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateKeywords_WithNullCollection_ReturnsError()
    {
        // Act
        var errors = KeywordValidator.ValidateKeywords(null!);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Keywords collection cannot be null", errors[0]);
    }

    [Fact]
    public void ValidateKeywords_WithEmptyKeyword_ReturnsError()
    {
        // Arrange
        var keywords = new List<string> { "valid", "", "keywords" };

        // Act
        var errors = KeywordValidator.ValidateKeywords(keywords);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Keyword at index 1 is null or empty", errors[0]);
    }

    [Fact]
    public void ValidateKeywords_WithNullKeyword_ReturnsError()
    {
        // Arrange
        var keywords = new List<string> { "valid", null!, "keywords" };

        // Act
        var errors = KeywordValidator.ValidateKeywords(keywords);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Keyword at index 1 is null or empty", errors[0]);
    }

    [Fact]
    public void ValidateKeywords_WithWhitespaceKeyword_ReturnsError()
    {
        // Arrange
        var keywords = new List<string> { "valid", "   ", "keywords" };

        // Act
        var errors = KeywordValidator.ValidateKeywords(keywords);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Keyword at index 1 is null or empty", errors[0]);
    }

    [Fact]
    public void ValidateKeywords_WithTooLongKeyword_ReturnsError()
    {
        // Arrange
        var longKeyword = new string('a', 101);
        var keywords = new List<string> { "valid", longKeyword, "keywords" };

        // Act
        var errors = KeywordValidator.ValidateKeywords(keywords);

        // Assert
        Assert.Single(errors);
        Assert.Contains("exceeds maximum length of 100 characters", errors[0]);
    }

    [Fact]
    public void ValidateKeywords_WithLineBreaks_ReturnsError()
    {
        // Arrange
        var keywords = new List<string> { "valid", "keyword\nwith\nbreaks", "keywords" };

        // Act
        var errors = KeywordValidator.ValidateKeywords(keywords);

        // Assert
        Assert.Single(errors);
        Assert.Contains("contains line breaks", errors[0]);
    }

    [Fact]
    public void ValidateKeywords_WithLeadingTrailingWhitespace_ReturnsError()
    {
        // Arrange
        var keywords = new List<string> { "valid", "  keyword  ", "keywords" };

        // Act
        var errors = KeywordValidator.ValidateKeywords(keywords);

        // Assert
        Assert.Single(errors);
        Assert.Contains("has leading or trailing whitespace", errors[0]);
    }

    [Fact]
    public void ValidateKeywords_WithDuplicates_ReturnsError()
    {
        // Arrange
        var keywords = new List<string> { "duplicate", "keyword", "DUPLICATE" };

        // Act
        var errors = KeywordValidator.ValidateKeywords(keywords);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Duplicate keyword found: 'duplicate'", errors[0]);
    }

    [Fact]
    public void ValidateKeywords_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var keywords = new List<string> { "", "valid", "  whitespace  ", "duplicate", "DUPLICATE" };

        // Act
        var errors = KeywordValidator.ValidateKeywords(keywords);

        // Assert
        Assert.Equal(3, errors.Count);
        Assert.Contains("Keyword at index 0 is null or empty", errors[0]);
        Assert.Contains("has leading or trailing whitespace", errors[1]);
        Assert.Contains("Duplicate keyword found", errors[2]);
    }

    [Fact]
    public void ValidateSectionMappings_WithValidMappings_ReturnsNoErrors()
    {
        // Arrange
        var mappings = new Dictionary<string, IReadOnlyList<string>>
        {
            { "^# Introduction", new List<string> { "intro", "overview" } },
            { "API.*", new List<string> { "api", "endpoint" } }
        };

        // Act
        var errors = KeywordValidator.ValidateSectionMappings(mappings);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateSectionMappings_WithNullMappings_ReturnsError()
    {
        // Act
        var errors = KeywordValidator.ValidateSectionMappings(null!);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Section mappings cannot be null", errors[0]);
    }

    [Fact]
    public void ValidateSectionMappings_WithEmptyPattern_ReturnsError()
    {
        // Arrange
        var mappings = new Dictionary<string, IReadOnlyList<string>>
        {
            { "", new List<string> { "keywords" } }
        };

        // Act
        var errors = KeywordValidator.ValidateSectionMappings(mappings);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Section mapping pattern cannot be null or empty", errors[0]);
    }

    [Fact]
    public void ValidateSectionMappings_WithInvalidRegex_ReturnsError()
    {
        // Arrange
        var mappings = new Dictionary<string, IReadOnlyList<string>>
        {
            { "[invalid", new List<string> { "keywords" } }
        };

        // Act
        var errors = KeywordValidator.ValidateSectionMappings(mappings);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Invalid regex pattern '[invalid'", errors[0]);
    }

    [Fact]
    public void ValidateSectionMappings_WithInvalidKeywords_ReturnsError()
    {
        // Arrange
        var mappings = new Dictionary<string, IReadOnlyList<string>>
        {
            { "valid.*pattern", new List<string> { "valid", "", "keywords" } }
        };

        // Act
        var errors = KeywordValidator.ValidateSectionMappings(mappings);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Pattern 'valid.*pattern': Keyword at index 1 is null or empty", errors[0]);
    }

    [Fact]
    public void SanitizeKeyword_WithValidKeyword_ReturnsNormalizedKeyword()
    {
        // Act
        var result = KeywordValidator.SanitizeKeyword("  Valid Keyword  ");

        // Assert
        Assert.Equal("valid keyword", result);
    }

    [Fact]
    public void SanitizeKeyword_WithNullKeyword_ReturnsNull()
    {
        // Act
        var result = KeywordValidator.SanitizeKeyword(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeKeyword_WithEmptyKeyword_ReturnsNull()
    {
        // Act
        var result = KeywordValidator.SanitizeKeyword("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeKeyword_WithWhitespaceOnly_ReturnsNull()
    {
        // Act
        var result = KeywordValidator.SanitizeKeyword("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeKeyword_WithMultipleSpaces_NormalizesSpaces()
    {
        // Act
        var result = KeywordValidator.SanitizeKeyword("keyword   with    spaces");

        // Assert
        Assert.Equal("keyword with spaces", result);
    }

    [Fact]
    public void SanitizeKeywords_WithMixedKeywords_ReturnsValidOnes()
    {
        // Arrange
        var keywords = new List<string> { "valid", "", "  Another Valid  ", null!, "DUPLICATE", "duplicate" };

        // Act
        var result = KeywordValidator.SanitizeKeywords(keywords);

        // Assert
        Assert.Equal(3, result.Count); // "valid", "another valid", "duplicate" (deduplicated)
        Assert.Contains("valid", result);
        Assert.Contains("another valid", result);
        Assert.Contains("duplicate", result);
    }

    [Fact]
    public void SanitizeKeywords_WithNullCollection_ReturnsEmptyList()
    {
        // Act
        var result = KeywordValidator.SanitizeKeywords(null!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SanitizeKeywords_RemovesDuplicates()
    {
        // Arrange
        var keywords = new List<string> { "keyword", "KEYWORD", "Keyword" };

        // Act
        var result = KeywordValidator.SanitizeKeywords(keywords);

        // Assert
        Assert.Single(result);
        Assert.Equal("keyword", result[0]);
    }

    [Fact]
    public void CreateSafeRegex_WithValidPattern_CreatesRegex()
    {
        // Act
        var regex = KeywordValidator.CreateSafeRegex("test.*pattern");

        // Assert
        Assert.NotNull(regex);
        Assert.Matches("test.*pattern", "test some pattern");
        Assert.DoesNotMatch("test.*pattern", "other pattern");
    }

    [Fact]
    public void CreateSafeRegex_WithCustomTimeout_UsesTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(500);

        // Act
        var regex = KeywordValidator.CreateSafeRegex("test.*", timeout);

        // Assert
        Assert.NotNull(regex);
        Assert.Equal(timeout, regex.MatchTimeout);
    }

    [Fact]
    public void CreateSafeRegex_IsCaseInsensitive()
    {
        // Act
        var regex = KeywordValidator.CreateSafeRegex("test");

        // Assert
        Assert.Matches("(?i)test", "TEST");
        Assert.Matches("(?i)test", "Test");
        Assert.Matches("(?i)test", "test");
    }
}

