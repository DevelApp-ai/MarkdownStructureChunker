using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Extractors;
using MarkdownStructureChunker.Core.Models;
using MarkdownStructureChunker.Core.Strategies;
using Xunit;

namespace MarkdownStructureChunker.Tests.Core;

/// <summary>
/// Tests for the structure-first ingestion architecture functionality in StructureChunker.
/// </summary>
public class StructureFirstTests
{
    [Fact]
    public void CreateStructureFirst_ReturnsChunkerWithASTStrategy()
    {
        // Arrange & Act
        using var chunker = StructureChunker.CreateStructureFirst();

        // Assert
        Assert.NotNull(chunker);
        
        // The chunker should be configured properly
        var result = chunker.Process("# Test\n\nContent", "test-doc");
        Assert.NotNull(result);
        Assert.NotEmpty(result.Chunks);
    }

    [Fact]
    public async Task ProcessWithStructureAsync_WithASTStrategy_ReturnsStructuralElements()
    {
        // Arrange
        using var chunker = StructureChunker.CreateStructureFirst();
        var markdown = @"# Introduction

This is the introduction section with important information.

## Background

The background provides context for understanding the main concepts.

### Historical Context

This subsection covers the historical development.

## Methodology

Our approach involves several key steps.";

        // Act
        var result = await chunker.ProcessWithStructureAsync(markdown, "test-doc");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-doc", result.SourceId);
        
        // Should have traditional chunks for backward compatibility
        Assert.NotEmpty(result.Chunks);
        
        // Should have structural elements from AST parsing
        Assert.True(result.HasStructuralGraph);
        Assert.NotEmpty(result.StructuralElements);
        Assert.NotEmpty(result.StructuralEdges);
        
        // Verify structural elements
        var headingElements = result.StructuralElements.Where(e => e.ElementType == "heading").ToList();
        Assert.True(headingElements.Count >= 4); // h1, h2, h3, h2
        
        // Verify hierarchical relationships
        var h1 = headingElements.FirstOrDefault(h => h.Level == 1);
        var h2Elements = headingElements.Where(h => h.Level == 2).ToList();
        var h3 = headingElements.FirstOrDefault(h => h.Level == 3);
        
        Assert.NotNull(h1);
        Assert.True(h2Elements.Count >= 2);
        Assert.NotNull(h3);
        
        // Verify relationships exist
        Assert.Contains(result.StructuralEdges, e => 
            e.SourceElementId == h1.Id && 
            h2Elements.Any(h2 => h2.Id == e.TargetElementId) &&
            e.RelationshipType == RelationshipTypes.HAS_SUBSECTION);
    }

    [Fact]
    public async Task ProcessWithStructureAsync_WithPatternStrategy_FallsBackToTraditionalProcessing()
    {
        // Arrange
        var patternStrategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        var keywordExtractor = new SimpleKeywordExtractor();
        using var chunker = new StructureChunker(patternStrategy, keywordExtractor);
        
        var markdown = @"# Test

Content here.";

        // Act
        var result = await chunker.ProcessWithStructureAsync(markdown, "test-doc");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Chunks);
        
        // Should not have structural elements since it's using pattern-based strategy
        Assert.False(result.HasStructuralGraph);
        Assert.Empty(result.StructuralElements);
        Assert.Empty(result.StructuralEdges);
    }

    [Fact]
    public void DocumentGraph_RootElements_ReturnsCorrectRoots()
    {
        // Arrange
        var element1 = new StructuralElement { Id = Guid.NewGuid(), ElementType = "heading", Level = 1 };
        var element2 = new StructuralElement { Id = Guid.NewGuid(), ElementType = "heading", Level = 2 };
        var element3 = new StructuralElement { Id = Guid.NewGuid(), ElementType = "paragraph" };
        
        var edge1 = new GraphEdge 
        { 
            SourceElementId = element1.Id, 
            TargetElementId = element2.Id, 
            RelationshipType = RelationshipTypes.HAS_SUBSECTION 
        };
        
        var documentGraph = new DocumentGraph
        {
            SourceId = "test",
            StructuralElements = new[] { element1, element2, element3 },
            StructuralEdges = new[] { edge1 }
        };

        // Act
        var rootElements = documentGraph.RootElements.ToList();

        // Assert
        Assert.Equal(2, rootElements.Count);
        Assert.Contains(element1, rootElements);
        Assert.Contains(element3, rootElements);
        Assert.DoesNotContain(element2, rootElements); // element2 is a child of element1
    }

    [Fact]
    public void DocumentGraph_GetChildElements_ReturnsCorrectChildren()
    {
        // Arrange
        var parent = new StructuralElement { Id = Guid.NewGuid(), ElementType = "heading", Level = 1 };
        var child1 = new StructuralElement { Id = Guid.NewGuid(), ElementType = "heading", Level = 2 };
        var child2 = new StructuralElement { Id = Guid.NewGuid(), ElementType = "paragraph" };
        var unrelated = new StructuralElement { Id = Guid.NewGuid(), ElementType = "paragraph" };
        
        var edge1 = new GraphEdge 
        { 
            SourceElementId = parent.Id, 
            TargetElementId = child1.Id, 
            RelationshipType = RelationshipTypes.HAS_SUBSECTION 
        };
        
        var edge2 = new GraphEdge 
        { 
            SourceElementId = parent.Id, 
            TargetElementId = child2.Id, 
            RelationshipType = RelationshipTypes.CONTAINS 
        };
        
        var documentGraph = new DocumentGraph
        {
            SourceId = "test",
            StructuralElements = new[] { parent, child1, child2, unrelated },
            StructuralEdges = new[] { edge1, edge2 }
        };

        // Act
        var children = documentGraph.GetChildElements(parent.Id).ToList();

        // Assert
        Assert.Equal(2, children.Count);
        Assert.Contains(child1, children);
        Assert.Contains(child2, children);
        Assert.DoesNotContain(unrelated, children);
    }

    [Fact]
    public void DocumentGraph_GetParentElement_ReturnsCorrectParent()
    {
        // Arrange
        var parent = new StructuralElement { Id = Guid.NewGuid(), ElementType = "heading", Level = 1 };
        var child = new StructuralElement { Id = Guid.NewGuid(), ElementType = "heading", Level = 2 };
        
        var edge = new GraphEdge 
        { 
            SourceElementId = parent.Id, 
            TargetElementId = child.Id, 
            RelationshipType = RelationshipTypes.HAS_SUBSECTION 
        };
        
        var documentGraph = new DocumentGraph
        {
            SourceId = "test",
            StructuralElements = new[] { parent, child },
            StructuralEdges = new[] { edge }
        };

        // Act
        var foundParent = documentGraph.GetParentElement(child.Id);

        // Assert
        Assert.NotNull(foundParent);
        Assert.Equal(parent.Id, foundParent.Id);
    }

    [Fact]
    public async Task ProcessWithStructureAsync_PreservesOriginalMarkdown()
    {
        // Arrange
        using var chunker = StructureChunker.CreateStructureFirst();
        var markdown = @"# **Bold Title**

This is *emphasized* text with `inline code`.

## Subtitle with [link](http://example.com)

> This is a blockquote
> with multiple lines.

```csharp
public void Example() 
{
    Console.WriteLine(""Hello, World!"");
}
```";

        // Act
        var result = await chunker.ProcessWithStructureAsync(markdown, "test-doc");

        // Assert
        var structuralElements = result.StructuralElements;
        
        // Verify original markdown is preserved in structural elements
        var headingElement = structuralElements.FirstOrDefault(e => e.ElementType == "heading" && e.Level == 1);
        Assert.NotNull(headingElement);
        Assert.Contains("**Bold Title**", headingElement.OriginalMarkdown);
        
        var codeElement = structuralElements.FirstOrDefault(e => e.ElementType == "code_block");
        Assert.NotNull(codeElement);
        Assert.Contains("Console.WriteLine", codeElement.Content);
    }

    [Fact]
    public void GraphEdge_RelationshipTypes_HasExpectedConstants()
    {
        // Verify all expected relationship types are defined
        Assert.Equal("HAS_SUBSECTION", RelationshipTypes.HAS_SUBSECTION);
        Assert.Equal("CONTAINS", RelationshipTypes.CONTAINS);
        Assert.Equal("FOLLOWS", RelationshipTypes.FOLLOWS);
        Assert.Equal("PRECEDES", RelationshipTypes.PRECEDES);
        Assert.Equal("SIBLING", RelationshipTypes.SIBLING);
        Assert.Equal("PARENT_OF", RelationshipTypes.PARENT_OF);
    }
}