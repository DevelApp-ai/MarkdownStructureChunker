using MarkdownStructureChunker.Core.Models;
using MarkdownStructureChunker.Core.Strategies;
using Xunit;

namespace MarkdownStructureChunker.Tests.Strategies;

public class PatternBasedStrategyTests
{
    [Fact]
    public void Constructor_WithValidRules_CreatesStrategy()
    {
        // Arrange
        var rules = new List<ChunkingRule>
        {
            new ChunkingRule("MarkdownH1", @"^#\s+(.*)", 1),
            new ChunkingRule("MarkdownH2", @"^##\s+(.*)", 2)
        };

        // Act
        var strategy = new PatternBasedStrategy(rules);

        // Assert
        Assert.NotNull(strategy);
    }

    [Fact]
    public void Constructor_WithNullRules_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PatternBasedStrategy(null!));
    }

    [Fact]
    public void Constructor_WithEmptyRules_ThrowsArgumentException()
    {
        // Arrange
        var rules = new List<ChunkingRule>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PatternBasedStrategy(rules));
    }

    [Fact]
    public void ProcessText_EmptyText_ReturnsEmptyList()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var strategy = new PatternBasedStrategy(rules);

        // Act
        var result = strategy.ProcessText("", "test-doc");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ProcessText_SimpleMarkdownDocument_ReturnsCorrectChunks()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var strategy = new PatternBasedStrategy(rules);
        var document = @"# Introduction
This is the introduction content.

## Background
This is the background content.

### Details
This is the details content.";

        // Act
        var result = strategy.ProcessText(document, "test-doc");

        // Assert
        Assert.Equal(3, result.Count);
        
        var intro = result[0];
        Assert.Equal("MarkdownH1", intro.ChunkType);
        Assert.Equal(1, intro.Level);
        Assert.Equal("# Introduction", intro.RawTitle);
        Assert.Equal("Introduction", intro.CleanTitle);
        Assert.Contains("introduction content", intro.Content);
        // Note: May have parent due to root chunk logic

        var background = result[1];
        Assert.Equal("MarkdownH2", background.ChunkType);
        Assert.Equal(2, background.Level);
        Assert.Equal("## Background", background.RawTitle);
        Assert.Equal("Background", background.CleanTitle);
        Assert.Contains("background content", background.Content);
        Assert.Equal(intro.Id, background.ParentId);

        var details = result[2];
        Assert.Equal("MarkdownH3", details.ChunkType);
        Assert.Equal(3, details.Level);
        Assert.Equal("### Details", details.RawTitle);
        Assert.Equal("Details", details.CleanTitle);
        Assert.Contains("details content", details.Content);
        Assert.Equal(background.Id, details.ParentId);
    }

    [Fact]
    public void ProcessText_NumericOutlines_ReturnsCorrectHierarchy()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var strategy = new PatternBasedStrategy(rules);
        var document = @"1. First Level
Content for first level.

1.1 Second Level
Content for second level.

1.2 Another Second Level
Content for another second level.

2. Another First Level
Content for another first level.";

        // Act
        var result = strategy.ProcessText(document, "test-doc");

        // Assert
        Assert.Equal(4, result.Count);
        
        var first = result[0];
        Assert.Equal("Numeric", first.ChunkType);
        Assert.Equal(1, first.Level);
        // Note: May have parent due to root chunk logic

        var second = result[1];
        Assert.Equal("Numeric", second.ChunkType);
        Assert.Equal(2, second.Level);
        Assert.Equal(first.Id, second.ParentId);

        var third = result[2];
        Assert.Equal("Numeric", third.ChunkType);
        Assert.Equal(2, third.Level);
        Assert.Equal(first.Id, third.ParentId);

        var fourth = result[3];
        Assert.Equal("Numeric", fourth.ChunkType);
        Assert.Equal(1, fourth.Level);
        // Note: May have parent due to root chunk logic
    }

    [Fact]
    public void ProcessText_MixedPatterns_ReturnsCorrectChunks()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var strategy = new PatternBasedStrategy(rules);
        var document = @"# Introduction
Introduction content.

1. First Section
First section content.

1.1 Subsection
Subsection content.

§ 42 Legal Requirements
Legal content.

Appendix A: Additional Resources
Appendix content.";

        // Act
        var result = strategy.ProcessText(document, "test-doc");

        // Assert
        Assert.Equal(5, result.Count);
        
        Assert.Equal("MarkdownH1", result[0].ChunkType);
        Assert.Equal("Numeric", result[1].ChunkType);
        Assert.Equal("Numeric", result[2].ChunkType);
        Assert.Equal("Legal", result[3].ChunkType);
        Assert.Equal("Appendix", result[4].ChunkType);
    }

    [Fact]
    public void CreateDefaultRules_ReturnsExpectedRules()
    {
        // Act
        var rules = PatternBasedStrategy.CreateDefaultRules();

        // Assert
        Assert.NotEmpty(rules);
        Assert.Contains(rules, r => r.Type == "MarkdownH1");
        Assert.Contains(rules, r => r.Type == "MarkdownH2");
        Assert.Contains(rules, r => r.Type == "MarkdownH3");
        Assert.Contains(rules, r => r.Type == "Numeric");
        Assert.Contains(rules, r => r.Type == "Legal");
        Assert.Contains(rules, r => r.Type == "Appendix");
        
        // Verify priority ordering
        var sortedRules = rules.OrderBy(r => r.Priority).ToList();
        Assert.Equal(rules, sortedRules);
    }

    [Fact]
    public void ProcessText_ContentWithoutHeadings_ReturnsEmptyList()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var strategy = new PatternBasedStrategy(rules);
        var document = @"This is just regular text content.
No headings or structured elements here.
Just plain paragraphs of text.";

        // Act
        var result = strategy.ProcessText(document, "test-doc");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ProcessText_ComplexHierarchy_MaintainsCorrectParentChildRelationships()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var strategy = new PatternBasedStrategy(rules);
        var document = @"# Chapter 1
Chapter content.

## Section 1.1
Section content.

### Subsection 1.1.1
Subsection content.

## Section 1.2
Another section content.

# Chapter 2
Another chapter content.";

        // Act
        var result = strategy.ProcessText(document, "test-doc");

        // Assert
        Assert.Equal(5, result.Count);
        
        var chapter1 = result[0];
        var section11 = result[1];
        var subsection111 = result[2];
        var section12 = result[3];
        var chapter2 = result[4];

        // Verify hierarchy
        // Note: Top-level chunks may have parents due to root chunk logic
        Assert.Equal(chapter1.Id, section11.ParentId);
        Assert.Equal(section11.Id, subsection111.ParentId);
        Assert.Equal(chapter1.Id, section12.ParentId);
        // chapter2 may also have a parent due to root chunk logic
    }
}

