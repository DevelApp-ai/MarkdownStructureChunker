using MarkdownStructureChunker.Core.Configuration;
using MarkdownStructureChunker.Core.Models;
using MarkdownStructureChunker.Core.Strategies;
using Xunit;

namespace MarkdownStructureChunker.Tests.Strategies;

public class PatternBasedStrategyHierarchyTests
{
    [Fact]
    public void ProcessText_WithHierarchicalStructure_BuildsParentChildRelationships()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Chapter 1
Chapter content here.
## Section 1.1
Section content.
### Subsection 1.1.1
Subsection content.
## Section 1.2
More section content.
# Chapter 2
Second chapter content.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var chapter1 = chunks.FirstOrDefault(c => c.CleanTitle == "Chapter 1");
        var section11 = chunks.FirstOrDefault(c => c.CleanTitle == "Section 1.1");
        var subsection111 = chunks.FirstOrDefault(c => c.CleanTitle == "Subsection 1.1.1");
        var section12 = chunks.FirstOrDefault(c => c.CleanTitle == "Section 1.2");
        var chapter2 = chunks.FirstOrDefault(c => c.CleanTitle == "Chapter 2");
        
        Assert.NotNull(chapter1);
        Assert.NotNull(section11);
        Assert.NotNull(subsection111);
        Assert.NotNull(section12);
        Assert.NotNull(chapter2);
        
        // Check parent relationships
        Assert.Null(chapter1.Parent); // Top level has no parent
        Assert.Equal(chapter1.Id, section11.ParentId);
        Assert.Equal(section11.Id, subsection111.ParentId);
        Assert.Equal(chapter1.Id, section12.ParentId);
        Assert.Null(chapter2.Parent); // Top level has no parent
        
        // Check children relationships
        Assert.Equal(2, chapter1.Children.Count); // Section 1.1 and Section 1.2
        Assert.Single(section11.Children); // Subsection 1.1.1
        Assert.Empty(subsection111.Children); // No children
        Assert.Empty(section12.Children); // No children
        Assert.Empty(chapter2.Children); // No children
        
        // Verify specific child relationships
        Assert.Contains(section11, chapter1.Children);
        Assert.Contains(section12, chapter1.Children);
        Assert.Contains(subsection111, section11.Children);
    }

    [Fact]
    public void ProcessText_WithParentReferences_SetsCorrectParentProperties()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Main Title
Main content.
## Subtitle A
Content A.
### Sub-subtitle A1
Content A1.
## Subtitle B
Content B.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var mainTitle = chunks.FirstOrDefault(c => c.CleanTitle == "Main Title");
        var subtitleA = chunks.FirstOrDefault(c => c.CleanTitle == "Subtitle A");
        var subSubtitleA1 = chunks.FirstOrDefault(c => c.CleanTitle == "Sub-subtitle A1");
        var subtitleB = chunks.FirstOrDefault(c => c.CleanTitle == "Subtitle B");
        
        Assert.NotNull(mainTitle);
        Assert.NotNull(subtitleA);
        Assert.NotNull(subSubtitleA1);
        Assert.NotNull(subtitleB);
        
        // Check Parent property references
        Assert.Null(mainTitle.Parent);
        Assert.NotNull(subtitleA.Parent);
        Assert.Equal(mainTitle.Id, subtitleA.Parent.Id);
        Assert.NotNull(subSubtitleA1.Parent);
        Assert.Equal(subtitleA.Id, subSubtitleA1.Parent.Id);
        Assert.NotNull(subtitleB.Parent);
        Assert.Equal(mainTitle.Id, subtitleB.Parent.Id);
    }

    [Fact]
    public void ProcessText_WithComplexHierarchy_NavigatesCorrectly()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Book Title
Introduction to the book.
## Chapter 1: Getting Started
Chapter 1 introduction.
### 1.1 Prerequisites
Prerequisites content.
### 1.2 Installation
Installation content.
#### 1.2.1 Windows Installation
Windows specific steps.
#### 1.2.2 Linux Installation
Linux specific steps.
### 1.3 Configuration
Configuration content.
## Chapter 2: Advanced Topics
Chapter 2 introduction.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var book = chunks.FirstOrDefault(c => c.CleanTitle == "Book Title");
        var chapter1 = chunks.FirstOrDefault(c => c.CleanTitle == "Chapter 1: Getting Started");
        var prerequisites = chunks.FirstOrDefault(c => c.CleanTitle == "1.1 Prerequisites");
        var installation = chunks.FirstOrDefault(c => c.CleanTitle == "1.2 Installation");
        var windowsInstall = chunks.FirstOrDefault(c => c.CleanTitle == "1.2.1 Windows Installation");
        var linuxInstall = chunks.FirstOrDefault(c => c.CleanTitle == "1.2.2 Linux Installation");
        var configuration = chunks.FirstOrDefault(c => c.CleanTitle == "1.3 Configuration");
        var chapter2 = chunks.FirstOrDefault(c => c.CleanTitle == "Chapter 2: Advanced Topics");
        
        Assert.NotNull(book);
        Assert.NotNull(chapter1);
        Assert.NotNull(prerequisites);
        Assert.NotNull(installation);
        Assert.NotNull(windowsInstall);
        Assert.NotNull(linuxInstall);
        Assert.NotNull(configuration);
        Assert.NotNull(chapter2);
        
        // Test navigation from book level
        Assert.Equal(2, book.Children.Count); // Chapter 1 and Chapter 2
        Assert.Contains(chapter1, book.Children);
        Assert.Contains(chapter2, book.Children);
        
        // Test navigation from chapter level
        Assert.Equal(3, chapter1.Children.Count); // Prerequisites, Installation, Configuration
        Assert.Contains(prerequisites, chapter1.Children);
        Assert.Contains(installation, chapter1.Children);
        Assert.Contains(configuration, chapter1.Children);
        
        // Test navigation from section level
        Assert.Equal(2, installation.Children.Count); // Windows and Linux installation
        Assert.Contains(windowsInstall, installation.Children);
        Assert.Contains(linuxInstall, installation.Children);
        
        // Test upward navigation
        Assert.Equal(book, chapter1.Parent);
        Assert.Equal(chapter1, installation.Parent);
        Assert.Equal(installation, windowsInstall.Parent);
        Assert.Equal(installation, linuxInstall.Parent);
    }

    [Fact]
    public void ProcessText_WithFlatStructure_HandlesCorrectly()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Title 1
Content 1.
# Title 2
Content 2.
# Title 3
Content 3.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var title1 = chunks.FirstOrDefault(c => c.CleanTitle == "Title 1");
        var title2 = chunks.FirstOrDefault(c => c.CleanTitle == "Title 2");
        var title3 = chunks.FirstOrDefault(c => c.CleanTitle == "Title 3");
        
        Assert.NotNull(title1);
        Assert.NotNull(title2);
        Assert.NotNull(title3);
        
        // All should be top-level with no parents or children
        Assert.Null(title1.Parent);
        Assert.Null(title2.Parent);
        Assert.Null(title3.Parent);
        
        Assert.Empty(title1.Children);
        Assert.Empty(title2.Children);
        Assert.Empty(title3.Children);
    }

    [Fact]
    public void ProcessText_WithSkippedLevels_HandlesCorrectly()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Level 1
Content 1.
### Level 3 (skipped level 2)
Content 3.
## Level 2 (after level 3)
Content 2.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var level1 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 1");
        var level3 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 3 (skipped level 2)");
        var level2 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 2 (after level 3)");
        
        Assert.NotNull(level1);
        Assert.NotNull(level3);
        Assert.NotNull(level2);
        
        // Level 3 should be child of Level 1 (skipped level 2)
        Assert.Equal(level1, level3.Parent);
        Assert.Equal(level1, level2.Parent);
        
        // Level 1 should have both as children
        Assert.Equal(2, level1.Children.Count);
        Assert.Contains(level3, level1.Children);
        Assert.Contains(level2, level1.Children);
    }

    [Fact]
    public void ProcessText_WithEmptyDocument_ReturnsEmptyList()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = "";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public void ProcessText_WithSingleHeading_HasNoParentOrChildren()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Single Heading
Single content.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.Single(chunks);
        
        var singleChunk = chunks.First();
        Assert.Equal("Single Heading", singleChunk.CleanTitle);
        Assert.Null(singleChunk.Parent);
        Assert.Empty(singleChunk.Children);
    }

    [Fact]
    public void ProcessText_WithDeepNesting_BuildsCorrectHierarchy()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Level 1
Content 1.
## Level 2
Content 2.
### Level 3
Content 3.
#### Level 4
Content 4.
##### Level 5
Content 5.
###### Level 6
Content 6.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var level1 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 1");
        var level2 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 2");
        var level3 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 3");
        var level4 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 4");
        var level5 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 5");
        var level6 = chunks.FirstOrDefault(c => c.CleanTitle == "Level 6");
        
        Assert.NotNull(level1);
        Assert.NotNull(level2);
        Assert.NotNull(level3);
        Assert.NotNull(level4);
        Assert.NotNull(level5);
        Assert.NotNull(level6);
        
        // Check the chain of parent-child relationships
        Assert.Null(level1.Parent);
        Assert.Equal(level1, level2.Parent);
        Assert.Equal(level2, level3.Parent);
        Assert.Equal(level3, level4.Parent);
        Assert.Equal(level4, level5.Parent);
        Assert.Equal(level5, level6.Parent);
        
        // Check children counts
        Assert.Single(level1.Children);
        Assert.Single(level2.Children);
        Assert.Single(level3.Children);
        Assert.Single(level4.Children);
        Assert.Single(level5.Children);
        Assert.Empty(level6.Children);
        
        // Verify the chain
        Assert.Equal(level2, level1.Children.First());
        Assert.Equal(level3, level2.Children.First());
        Assert.Equal(level4, level3.Children.First());
        Assert.Equal(level5, level4.Children.First());
        Assert.Equal(level6, level5.Children.First());
    }

    [Fact]
    public void ProcessText_WithMixedContentAndHeadings_BuildsCorrectStructure()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Introduction
This is the introduction with some content.

## Background
Background information here.

Some content without a heading.

## Methodology
Methodology description.

### Data Collection
How data was collected.

### Analysis
How analysis was performed.

## Results
The results section.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var introduction = chunks.FirstOrDefault(c => c.CleanTitle == "Introduction");
        var background = chunks.FirstOrDefault(c => c.CleanTitle == "Background");
        var methodology = chunks.FirstOrDefault(c => c.CleanTitle == "Methodology");
        var dataCollection = chunks.FirstOrDefault(c => c.CleanTitle == "Data Collection");
        var analysis = chunks.FirstOrDefault(c => c.CleanTitle == "Analysis");
        var results = chunks.FirstOrDefault(c => c.CleanTitle == "Results");
        
        Assert.NotNull(introduction);
        Assert.NotNull(background);
        Assert.NotNull(methodology);
        Assert.NotNull(dataCollection);
        Assert.NotNull(analysis);
        Assert.NotNull(results);
        
        // Check structure
        Assert.Equal(3, introduction.Children.Count); // Background, Methodology, Results
        Assert.Contains(background, introduction.Children);
        Assert.Contains(methodology, introduction.Children);
        Assert.Contains(results, introduction.Children);
        
        Assert.Equal(2, methodology.Children.Count); // Data Collection, Analysis
        Assert.Contains(dataCollection, methodology.Children);
        Assert.Contains(analysis, methodology.Children);
        
        // Check parents
        Assert.Equal(introduction, background.Parent);
        Assert.Equal(introduction, methodology.Parent);
        Assert.Equal(methodology, dataCollection.Parent);
        Assert.Equal(methodology, analysis.Parent);
        Assert.Equal(introduction, results.Parent);
    }
}

