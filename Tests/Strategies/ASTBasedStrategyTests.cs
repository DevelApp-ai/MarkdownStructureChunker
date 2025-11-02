using MarkdownStructureChunker.Core.Models;
using MarkdownStructureChunker.Core.Strategies;
using Xunit;

namespace MarkdownStructureChunker.Tests.Strategies;

/// <summary>
/// Tests for the AST-based chunking strategy that implements structure-first ingestion architecture.
/// </summary>
public class ASTBasedStrategyTests
{
    [Fact]
    public void ProcessText_WithSimpleMarkdown_ReturnsStructuredChunks()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Introduction

This is the introduction section.

## Background

Some background information.

### Details

More detailed information.";

        // Act
        var chunks = strategy.ProcessText(markdown, "test-doc");

        // Assert
        Assert.NotEmpty(chunks);
        
        // Should have at least the headings
        var headings = chunks.Where(c => c.IsHeading).ToList();
        Assert.True(headings.Count >= 3);
        
        // Verify heading hierarchy
        var h1 = headings.FirstOrDefault(h => h.Level == 1);
        Assert.NotNull(h1);
        Assert.Equal("Introduction", h1.CleanTitle);
        
        var h2 = headings.FirstOrDefault(h => h.Level == 2);
        Assert.NotNull(h2);
        Assert.Equal("Background", h2.CleanTitle);
        
        var h3 = headings.FirstOrDefault(h => h.Level == 3);
        Assert.NotNull(h3);
        Assert.Equal("Details", h3.CleanTitle);
    }

    [Fact]
    public void ProcessTextToStructure_WithMarkdown_ReturnsStructuralElements()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Main Title

This is a paragraph.

## Section Title

Another paragraph.

- List item 1
- List item 2

```csharp
var code = ""example"";
```";

        // Act
        var (elements, edges, chunks) = strategy.ProcessTextToStructure(markdown, "test-doc");

        // Assert
        Assert.NotEmpty(elements);
        Assert.NotEmpty(edges);
        Assert.NotEmpty(chunks);
        
        // Verify we have different types of structural elements
        Assert.Contains(elements, e => e.ElementType == "heading");
        Assert.Contains(elements, e => e.ElementType == "paragraph");
        Assert.Contains(elements, e => e.ElementType == "list");
        Assert.Contains(elements, e => e.ElementType == "code_block");
        
        // Verify hierarchical relationships
        var headingElements = elements.Where(e => e.ElementType == "heading").ToList();
        Assert.True(headingElements.Count >= 2);
        
        // Should have edges representing relationships
        Assert.Contains(edges, e => e.RelationshipType == RelationshipTypes.HAS_SUBSECTION);
    }

    [Fact]
    public void ProcessTextToStructure_WithHierarchicalMarkdown_CreatesCorrectRelationships()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Chapter 1

Introduction content.

## Section 1.1

Section content.

### Subsection 1.1.1

Subsection content.

## Section 1.2

More section content.";

        // Act
        var (elements, edges, chunks) = strategy.ProcessTextToStructure(markdown, "test-doc");

        // Assert
        var headingElements = elements.Where(e => e.ElementType == "heading").ToList();
        
        // Should have proper hierarchy: h1 -> h2 -> h3, h1 -> h2
        var h1 = headingElements.FirstOrDefault(h => h.Level == 1);
        var h2Elements = headingElements.Where(h => h.Level == 2).ToList();
        var h3 = headingElements.FirstOrDefault(h => h.Level == 3);
        
        Assert.NotNull(h1);
        Assert.Equal(2, h2Elements.Count);
        Assert.NotNull(h3);
        
        // Verify relationships exist
        Assert.Contains(edges, e => 
            e.SourceElementId == h1.Id && 
            h2Elements.Any(h2 => h2.Id == e.TargetElementId) &&
            e.RelationshipType == RelationshipTypes.HAS_SUBSECTION);
            
        Assert.Contains(edges, e => 
            h2Elements.Any(h2 => h2.Id == e.SourceElementId) && 
            e.TargetElementId == h3.Id &&
            e.RelationshipType == RelationshipTypes.HAS_SUBSECTION);
    }

    [Fact]
    public void ProcessText_WithEmptyInput_ReturnsEmptyList()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();

        // Act
        var chunks = strategy.ProcessText("", "test-doc");

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public void ProcessText_WithNullInput_ReturnsEmptyList()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();

        // Act
        var chunks = strategy.ProcessText(null!, "test-doc");

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public void ProcessTextToStructure_WithCodeBlocks_PreservesCodeContent()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Code Examples

Here is some code:

```csharp
public class Example
{
    public string Name { get; set; }
}
```

And another block:

```javascript
const example = {
    name: 'test'
};
```";

        // Act
        var (elements, edges, chunks) = strategy.ProcessTextToStructure(markdown, "test-doc");

        // Assert
        var codeElements = elements.Where(e => e.ElementType == "code_block").ToList();
        Assert.Equal(2, codeElements.Count);
        
        // Verify code content is preserved
        Assert.Contains(codeElements, e => e.Content.Contains("public class Example"));
        Assert.Contains(codeElements, e => e.Content.Contains("const example"));
    }

    [Fact]
    public void ProcessTextToStructure_WithLists_IdentifiesListStructure()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Task List

## Items to Complete

- First item
- Second item
  - Nested item
  - Another nested item
- Third item

## Numbered List

1. Step one
2. Step two
3. Step three";

        // Act
        var (elements, edges, chunks) = strategy.ProcessTextToStructure(markdown, "test-doc");

        // Assert
        var listElements = elements.Where(e => e.ElementType == "list").ToList();
        Assert.True(listElements.Count >= 2);
        
        // Verify list content includes bullet points
        Assert.Contains(listElements, e => e.Content.Contains("• First item"));
    }

    [Fact]
    public void ProcessText_SetsCorrectOffsets()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Title

Content here.

## Subtitle

More content.";

        // Act
        var chunks = strategy.ProcessText(markdown, "test-doc");

        // Assert
        foreach (var chunk in chunks)
        {
            Assert.True(chunk.StartOffset >= 0);
            Assert.True(chunk.EndOffset > chunk.StartOffset);
            Assert.True(chunk.EndOffset <= markdown.Length);
        }
        
        // First chunk should start at beginning
        var firstChunk = chunks.OrderBy(c => c.StartOffset).First();
        Assert.Equal(0, firstChunk.StartOffset);
    }
}