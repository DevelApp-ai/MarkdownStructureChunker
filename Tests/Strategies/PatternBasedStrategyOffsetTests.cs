using MarkdownStructureChunker.Core.Configuration;
using MarkdownStructureChunker.Core.Models;
using MarkdownStructureChunker.Core.Strategies;
using Xunit;

namespace MarkdownStructureChunker.Tests.Strategies;

public class PatternBasedStrategyOffsetTests
{
    [Fact]
    public void ProcessText_WithOffsetCalculation_CalculatesCorrectOffsets()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            CalculateOffsets = true,
            PreserveOriginalMarkdown = false
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = "# First Heading\nSome content here.\n## Second Heading\nMore content.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var firstChunk = chunks.FirstOrDefault(c => c.CleanTitle == "First Heading");
        var secondChunk = chunks.FirstOrDefault(c => c.CleanTitle == "Second Heading");
        
        Assert.NotNull(firstChunk);
        Assert.NotNull(secondChunk);
        
        // First chunk should start at the beginning
        Assert.Equal(0, firstChunk.StartOffset);
        Assert.True(firstChunk.EndOffset > firstChunk.StartOffset);
        
        // Second chunk should start after the first
        Assert.True(secondChunk.StartOffset > firstChunk.StartOffset);
        Assert.True(secondChunk.EndOffset > secondChunk.StartOffset);
    }

    [Fact]
    public void ProcessText_WithOriginalMarkdownPreservation_PreservesMarkdown()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            PreserveOriginalMarkdown = true,
            CalculateOffsets = true
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = "# Main Title\nThis is **bold** text with *italic* formatting.\n## Subtitle\nMore content.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var mainChunk = chunks.FirstOrDefault(c => c.CleanTitle == "Main Title");
        Assert.NotNull(mainChunk);
        Assert.NotNull(mainChunk.OriginalMarkdown);
        Assert.Contains("# Main Title", mainChunk.OriginalMarkdown);
    }

    [Fact]
    public void ProcessText_WithoutOriginalMarkdownPreservation_DoesNotPreserveMarkdown()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            PreserveOriginalMarkdown = false
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = "# Title\nContent here.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk => Assert.Equal(string.Empty, chunk.OriginalMarkdown));
    }

    [Fact]
    public void ProcessText_WithHeadingHierarchy_BuildsCorrectHierarchy()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            IncludeHeadingHierarchy = true
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Chapter 1
Content for chapter 1.
## Section 1.1
Content for section 1.1.
### Subsection 1.1.1
Content for subsection.
## Section 1.2
Content for section 1.2.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var chapter = chunks.FirstOrDefault(c => c.CleanTitle == "Chapter 1");
        var section11 = chunks.FirstOrDefault(c => c.CleanTitle == "Section 1.1");
        var subsection = chunks.FirstOrDefault(c => c.CleanTitle == "Subsection 1.1.1");
        var section12 = chunks.FirstOrDefault(c => c.CleanTitle == "Section 1.2");
        
        Assert.NotNull(chapter);
        Assert.NotNull(section11);
        Assert.NotNull(subsection);
        Assert.NotNull(section12);
        
        // Check heading hierarchy
        Assert.Single(chapter.HeadingHierarchy);
        Assert.Equal("Chapter 1", chapter.HeadingHierarchy.First());
        
        Assert.Equal(2, section11.HeadingHierarchy.Count());
        Assert.Equal("Chapter 1", section11.HeadingHierarchy.First());
        Assert.Equal("Section 1.1", section11.HeadingHierarchy.Skip(1).First());
        
        Assert.Equal(3, subsection.HeadingHierarchy.Count());
        Assert.Equal("Chapter 1", subsection.HeadingHierarchy.First());
        Assert.Equal("Section 1.1", subsection.HeadingHierarchy.Skip(1).First());
        Assert.Equal("Subsection 1.1.1", subsection.HeadingHierarchy.Skip(2).First());
        
        Assert.Equal(2, section12.HeadingHierarchy.Count());
        Assert.Equal("Chapter 1", section12.HeadingHierarchy.First());
        Assert.Equal("Section 1.2", section12.HeadingHierarchy.Skip(1).First());
    }

    [Fact]
    public void ProcessText_WithSectionLevels_SetsCorrectSectionLevels()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Level 1
Content
## Level 2
Content
### Level 3
Content";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var level1 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 1");
        var level2 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 2");
        var level3 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 3");
        
        Assert.NotNull(level1);
        Assert.NotNull(level2);
        Assert.NotNull(level3);
        
        Assert.Equal(1, level1.SectionLevel);
        Assert.Equal(2, level2.SectionLevel);
        Assert.Equal(3, level3.SectionLevel);
        
        Assert.True(level1.IsHeading);
        Assert.True(level2.IsHeading);
        Assert.True(level3.IsHeading);
    }

    [Fact]
    public void ProcessText_WithParentHeadings_SetsCorrectParentHeadings()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Main Chapter
Chapter content.
## First Section
Section content.
### Subsection
Subsection content.
## Second Section
More section content.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var mainChapter = chunks.FirstOrDefault(c => c.CleanTitle == "Main Chapter");
        var firstSection = chunks.FirstOrDefault(c => c.CleanTitle == "First Section");
        var subsection = chunks.FirstOrDefault(c => c.CleanTitle == "Subsection");
        var secondSection = chunks.FirstOrDefault(c => c.CleanTitle == "Second Section");
        
        Assert.NotNull(mainChapter);
        Assert.NotNull(firstSection);
        Assert.NotNull(subsection);
        Assert.NotNull(secondSection);
        
        // Check parent headings
        Assert.Equal(string.Empty, mainChapter.ParentHeading); // Top level has no parent
        Assert.Equal("Main Chapter", firstSection.ParentHeading);
        Assert.Equal("First Section", subsection.ParentHeading);
        Assert.Equal("Main Chapter", secondSection.ParentHeading);
    }

    [Fact]
    public void ProcessText_WithChunkTypeEnum_SetsCorrectChunkTypes()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Heading
Regular content here.
## Another Heading
More content.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var headingChunk = chunks.FirstOrDefault(c => c.CleanTitle == "Heading");
        var anotherHeadingChunk = chunks.FirstOrDefault(c => c.CleanTitle == "Another Heading");
        
        Assert.NotNull(headingChunk);
        Assert.NotNull(anotherHeadingChunk);
        
        Assert.Equal(ChunkType.Header, headingChunk.ChunkTypeEnum);
        Assert.Equal(ChunkType.Header, anotherHeadingChunk.ChunkTypeEnum);
    }

    [Fact]
    public void ProcessText_WithComplexDocument_CalculatesOffsetsCorrectly()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            CalculateOffsets = true,
            PreserveOriginalMarkdown = true
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Introduction
This is the introduction paragraph.

## Background
This section provides background information.

### Historical Context
Historical information goes here.

## Methodology
This section describes the methodology.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        // Verify that offsets are sequential and non-overlapping
        var sortedChunks = chunks.OrderBy(c => c.StartOffset).ToList();
        
        for (int i = 0; i < sortedChunks.Count - 1; i++)
        {
            var currentChunk = sortedChunks[i];
            var nextChunk = sortedChunks[i + 1];
            
            Assert.True(currentChunk.StartOffset < currentChunk.EndOffset, 
                $"Chunk '{currentChunk.CleanTitle}' should have start < end");
            Assert.True(currentChunk.EndOffset <= nextChunk.StartOffset, 
                $"Chunk '{currentChunk.CleanTitle}' should not overlap with '{nextChunk.CleanTitle}'");
        }
    }

    [Fact]
    public void ProcessText_WithEmptyLines_HandlesOffsetsCorrectly()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            CalculateOffsets = true
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = "# Title\n\n\nContent with empty lines.\n\n## Another Title\n\nMore content.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var firstChunk = chunks.FirstOrDefault(c => c.CleanTitle == "Title");
        var secondChunk = chunks.FirstOrDefault(c => c.CleanTitle == "Another Title");
        
        Assert.NotNull(firstChunk);
        Assert.NotNull(secondChunk);
        
        Assert.True(firstChunk.StartOffset >= 0);
        Assert.True(firstChunk.EndOffset > firstChunk.StartOffset);
        Assert.True(secondChunk.StartOffset >= firstChunk.EndOffset);
        Assert.True(secondChunk.EndOffset > secondChunk.StartOffset);
    }

    [Fact]
    public void ProcessText_WithSpecialCharacters_HandlesOffsetsCorrectly()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            CalculateOffsets = true,
            PreserveOriginalMarkdown = true
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = "# Título con acentos\nContenido con émojis 🚀 y símbolos @#$%.\n## Другой заголовок\nТекст на кириллице.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var firstChunk = chunks.FirstOrDefault(c => c.CleanTitle == "Título con acentos");
        var secondChunk = chunks.FirstOrDefault(c => c.CleanTitle == "Другой заголовок");
        
        Assert.NotNull(firstChunk);
        Assert.NotNull(secondChunk);
        
        Assert.True(firstChunk.StartOffset >= 0);
        Assert.True(firstChunk.EndOffset > firstChunk.StartOffset);
        Assert.True(secondChunk.StartOffset > firstChunk.StartOffset);
        Assert.True(secondChunk.EndOffset > secondChunk.StartOffset);
        
        // Verify original markdown preservation with special characters
        Assert.NotNull(firstChunk.OriginalMarkdown);
        Assert.Contains("Título con acentos", firstChunk.OriginalMarkdown);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProcessText_WithCalculateOffsetsConfiguration_RespectsSettings(bool calculateOffsets)
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            CalculateOffsets = calculateOffsets
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = "# Title\nContent here.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var chunk = chunks.First();
        if (calculateOffsets)
        {
            Assert.True(chunk.StartOffset >= 0);
            Assert.True(chunk.EndOffset >= chunk.StartOffset);
        }
        else
        {
            // When offset calculation is disabled, offsets should still be set (default behavior)
            // but we don't make strict assertions about their accuracy
            Assert.True(chunk.StartOffset >= 0);
            Assert.True(chunk.EndOffset >= 0);
        }
    }
}

