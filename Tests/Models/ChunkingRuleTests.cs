using MarkdownStructureChunker.Core.Models;
using Xunit;

namespace MarkdownStructureChunker.Tests.Models;

public class ChunkingRuleTests
{
    [Fact]
    public void Constructor_WithFixedLevel_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var rule = new ChunkingRule("MarkdownH1", @"^#\s+(.*)", level: 1, priority: 0);

        // Assert
        Assert.Equal("MarkdownH1", rule.Type);
        Assert.Equal(1, rule.FixedLevel);
        Assert.Equal(0, rule.Priority);
        Assert.NotNull(rule.Pattern);
    }

    [Fact]
    public void Constructor_WithoutFixedLevel_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var rule = new ChunkingRule("Numeric", @"^(\d+(\.\d+)*)\s+(.*)", priority: 10);

        // Assert
        Assert.Equal("Numeric", rule.Type);
        Assert.Null(rule.FixedLevel);
        Assert.Equal(10, rule.Priority);
        Assert.NotNull(rule.Pattern);
    }

    [Theory]
    [InlineData("# Introduction", "MarkdownH1", @"^#\s+(.*)", 1, "Introduction")]
    [InlineData("## Background", "MarkdownH2", @"^##\s+(.*)", 2, "Background")]
    [InlineData("### Details", "MarkdownH3", @"^###\s+(.*)", 3, "Details")]
    public void TryMatch_MarkdownHeadings_ReturnsCorrectMatch(string input, string expectedType, string pattern, int expectedLevel, string expectedCleanTitle)
    {
        // Arrange
        var rule = new ChunkingRule(expectedType, pattern, expectedLevel, priority: 0);

        // Act
        var match = rule.TryMatch(input);

        // Assert
        Assert.NotNull(match);
        Assert.Equal(expectedType, match.Type);
        Assert.Equal(expectedLevel, match.Level);
        Assert.Equal(input, match.RawTitle);
        Assert.Equal(expectedCleanTitle, match.CleanTitle);
    }

    [Theory]
    [InlineData("1. First Level", 1)]
    [InlineData("1.1 Second Level", 2)]
    [InlineData("1.1.1 Third Level", 3)]
    [InlineData("2.3.4.5 Fourth Level", 4)]
    public void TryMatch_NumericOutlines_CalculatesLevelCorrectly(string input, int expectedLevel)
    {
        // Arrange
        var rule = new ChunkingRule("Numeric", @"^(\d+(?:\.\d+)*\.?)\s+(.*)");

        // Act
        var match = rule.TryMatch(input);

        // Assert
        Assert.NotNull(match);
        Assert.Equal("Numeric", match.Type);
        Assert.Equal(expectedLevel, match.Level);
        Assert.Equal(input, match.RawTitle);
    }

    [Theory]
    [InlineData("§ 42 Legal Requirements", "Legal", @"^(§\s*\d+)\s+(.*)", "Legal Requirements")]
    [InlineData("Appendix A: Additional Resources", "Appendix", @"^Appendix\s+([A-Z])[\.:\-\s]+(.*)", "Additional Resources")]
    [InlineData("A. Technical Specifications", "Letter", @"^([A-Z])\.\s+(.*)", "Technical Specifications")]
    public void TryMatch_SpecialPatterns_ReturnsCorrectMatch(string input, string expectedType, string pattern, string expectedCleanTitle)
    {
        // Arrange
        var rule = new ChunkingRule(expectedType, pattern, level: 1, priority: 0);

        // Act
        var match = rule.TryMatch(input);

        // Assert
        Assert.NotNull(match);
        Assert.Equal(expectedType, match.Type);
        Assert.Equal(1, match.Level);
        Assert.Equal(input, match.RawTitle);
        Assert.Equal(expectedCleanTitle, match.CleanTitle);
    }

    [Theory]
    [InlineData("Regular text without pattern")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryMatch_NoMatch_ReturnsNull(string input)
    {
        // Arrange
        var rule = new ChunkingRule("MarkdownH1", @"^#\s+(.*)", 1);

        // Act
        var match = rule.TryMatch(input);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public void TryMatch_EmptyInput_ReturnsNull()
    {
        // Arrange
        var rule = new ChunkingRule("MarkdownH1", @"^#\s+(.*)", 1);

        // Act
        var match = rule.TryMatch(string.Empty);

        // Assert
        Assert.Null(match);
    }
}

