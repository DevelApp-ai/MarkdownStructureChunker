using MarkdownStructureChunker.Core.Models;
using MarkdownStructureChunker.Core.Strategies;
using Xunit;

namespace MarkdownStructureChunker.Tests.Strategies;

/// <summary>
/// Tests for the improved AST-based chunking strategy that addresses hierarchical assumptions and link detection.
/// </summary>
public class ASTBasedStrategyImprovedTests
{
    [Fact]
    public void ProcessTextToStructure_WithNonSequentialHeadings_CreatesCorrectRelationshipTypes()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Main Title (H1)

Content here.

### Skipped H2, Direct H3

This jumps from H1 to H3.

## Now H2 After H3

This H2 comes after an H3.

#### H4 Under H2

Deep nesting.";

        // Act
        var (elements, edges, chunks) = strategy.ProcessTextToStructure(markdown, "test-doc");

        // Assert
        var headingElements = elements.Where(e => e.ElementType == "heading").ToList();
        Assert.Equal(4, headingElements.Count);

        // Check for HAS_NESTED_SECTION relationship (H1 -> H3)
        var nestedRelationships = edges.Where(e => e.RelationshipType == RelationshipTypes.HAS_NESTED_SECTION).ToList();
        Assert.True(nestedRelationships.Count >= 1, "Should have at least one HAS_NESTED_SECTION relationship");

        // Check for HAS_SUBSECTION relationship (H1 -> H2, H2 -> H4)
        var subsectionRelationships = edges.Where(e => e.RelationshipType == RelationshipTypes.HAS_SUBSECTION).ToList();
        Assert.True(subsectionRelationships.Count >= 1, "Should have at least one HAS_SUBSECTION relationship");

        // Verify that H1 -> H3 uses HAS_NESTED_SECTION
        var h1 = headingElements.First(h => h.Level == 1);
        var h3 = headingElements.First(h => h.Level == 3);
        
        var h1ToH3Edge = edges.FirstOrDefault(e => 
            e.SourceElementId == h1.Id && 
            e.TargetElementId == h3.Id);
            
        Assert.NotNull(h1ToH3Edge);
        Assert.Equal(RelationshipTypes.HAS_NESTED_SECTION, h1ToH3Edge.RelationshipType);
    }

    [Fact]
    public void ProcessTextToStructure_WithDocumentLinks_DetectsAndClassifiesLinks()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Documentation

Check out [this internal document](./other-doc.md) for more details.

Also see [external link](https://example.com) and [another internal doc](../docs/guide.md).

Email us at [support](mailto:support@example.com) or visit our [main site](#section).";

        // Act
        var (elements, edges, chunks) = strategy.ProcessTextToStructure(markdown, "test-doc");

        // Assert - Check that links are detected in elements
        var elementsWithLinks = elements.Where(e => e.Links.Any()).ToList();
        Assert.True(elementsWithLinks.Count >= 1, "Should find elements containing links");

        var allLinks = elementsWithLinks.SelectMany(e => e.Links).ToList();
        Assert.True(allLinks.Count >= 4, $"Should find at least 4 links, found {allLinks.Count}");

        // Verify link types
        Assert.Contains(allLinks, l => l.Type == LinkType.Internal && l.Url == "./other-doc.md");
        Assert.Contains(allLinks, l => l.Type == LinkType.External && l.Url == "https://example.com");
        Assert.Contains(allLinks, l => l.Type == LinkType.Internal && l.Url == "../docs/guide.md");
        Assert.Contains(allLinks, l => l.Type == LinkType.Email && l.Url == "mailto:support@example.com");
        Assert.Contains(allLinks, l => l.Type == LinkType.Anchor && l.Url == "#section");
    }

    [Fact]
    public void ProcessTextToStructure_WithInternalLinks_CreatesLinkRelationships()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Main Document

See [related document](./related.md) for more information.

Also check [another doc](../folder/other.md).";

        // Act
        var (elements, edges, chunks) = strategy.ProcessTextToStructure(markdown, "test-doc");

        // Assert - Check for LINKS_TO relationships
        var linkRelationships = edges.Where(e => e.RelationshipType == RelationshipTypes.LINKS_TO).ToList();
        Assert.True(linkRelationships.Count >= 2, $"Should find at least 2 link relationships, found {linkRelationships.Count}");

        // Verify link metadata
        foreach (var linkEdge in linkRelationships)
        {
            Assert.True(linkEdge.Metadata.ContainsKey("LinkUrl"), "Link edge should contain LinkUrl metadata");
            Assert.True(linkEdge.Metadata.ContainsKey("LinkText"), "Link edge should contain LinkText metadata");
            Assert.True(linkEdge.Metadata.ContainsKey("LinkType"), "Link edge should contain LinkType metadata");
        }

        // Check that link elements are created
        var linkElements = elements.Where(e => e.ElementType == "link").ToList();
        Assert.True(linkElements.Count >= 2, $"Should create link elements, found {linkElements.Count}");
    }

    [Fact]
    public void ProcessTextToStructure_WithComplexHierarchy_HandlesCorrectly()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Chapter 1

Introduction.

### Section 1.1 (skipped H2)

Direct from H1 to H3.

## Section 1.2

Now we have H2.

#### Subsection 1.2.1 (skipped H3)

From H2 to H4.

### Section 1.3

Back to H3.

# Chapter 2

New chapter.";

        // Act
        var (elements, edges, chunks) = strategy.ProcessTextToStructure(markdown, "test-doc");

        // Assert
        var headingElements = elements.Where(e => e.ElementType == "heading").ToList();
        Assert.Equal(6, headingElements.Count); // 2 H1s, 1 H2, 2 H3s, 1 H4

        // Check relationship types
        var nestedRelationships = edges.Where(e => e.RelationshipType == RelationshipTypes.HAS_NESTED_SECTION).ToList();
        var directRelationships = edges.Where(e => e.RelationshipType == RelationshipTypes.HAS_SUBSECTION).ToList();

        // Should have both types of relationships
        Assert.True(nestedRelationships.Count > 0, "Should have nested section relationships");
        Assert.True(directRelationships.Count > 0, "Should have direct subsection relationships");

        // Verify specific relationships
        var chapter1 = headingElements.First(h => h.Content.Contains("Chapter 1"));
        var section11 = headingElements.First(h => h.Content.Contains("Section 1.1"));
        var section12 = headingElements.First(h => h.Content.Contains("Section 1.2"));

        // Chapter 1 -> Section 1.1 should be HAS_NESTED_SECTION (H1 -> H3)
        var chapter1ToSection11 = edges.FirstOrDefault(e => 
            e.SourceElementId == chapter1.Id && 
            e.TargetElementId == section11.Id);
        Assert.NotNull(chapter1ToSection11);
        Assert.Equal(RelationshipTypes.HAS_NESTED_SECTION, chapter1ToSection11.RelationshipType);

        // Chapter 1 -> Section 1.2 should be HAS_SUBSECTION (H1 -> H2)
        var chapter1ToSection12 = edges.FirstOrDefault(e => 
            e.SourceElementId == chapter1.Id && 
            e.TargetElementId == section12.Id);
        Assert.NotNull(chapter1ToSection12);
        Assert.Equal(RelationshipTypes.HAS_SUBSECTION, chapter1ToSection12.RelationshipType);
    }

    [Fact]
    public void DetermineLinkType_ClassifiesLinksCorrectly()
    {
        // This tests the link type classification logic
        var strategy = new ASTBasedStrategy();
        
        // Use reflection to access the private method for testing
        var method = typeof(ASTBasedStrategy).GetMethod("DetermineLinkType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Test various link types
        Assert.Equal(LinkType.External, method.Invoke(strategy, new[] { "https://example.com" }));
        Assert.Equal(LinkType.External, method.Invoke(strategy, new[] { "http://example.com" }));
        Assert.Equal(LinkType.Email, method.Invoke(strategy, new[] { "mailto:test@example.com" }));
        Assert.Equal(LinkType.Anchor, method.Invoke(strategy, new[] { "#section" }));
        Assert.Equal(LinkType.Internal, method.Invoke(strategy, new[] { "./doc.md" }));
        Assert.Equal(LinkType.Internal, method.Invoke(strategy, new[] { "../folder/doc.md" }));
        Assert.Equal(LinkType.Internal, method.Invoke(strategy, new[] { "folder/doc.html" }));
        Assert.Equal(LinkType.Other, method.Invoke(strategy, new[] { "ftp://example.com" }));
    }

    [Fact]
    public void ProcessTextToStructure_WithMixedContent_PreservesAllFeatures()
    {
        // Arrange
        var strategy = new ASTBasedStrategy();
        var markdown = @"# Main Title

Introduction paragraph.

### Direct H3

This skips H2.

- List item with [link](./doc.md)
- Another item

```python
# Code block
print('hello')
```

## Proper H2

Finally a proper H2.

Check [external site](https://example.com) and [email](mailto:test@example.com).";

        // Act
        var (elements, edges, chunks) = strategy.ProcessTextToStructure(markdown, "test-doc");

        // Assert - Should have various element types
        var elementTypes = elements.Select(e => e.ElementType).Distinct().ToList();
        Assert.Contains("heading", elementTypes);
        Assert.Contains("paragraph", elementTypes);
        Assert.Contains("list", elementTypes);
        Assert.Contains("code_block", elementTypes);

        // Should have various relationship types
        var relationshipTypes = edges.Select(e => e.RelationshipType).Distinct().ToList();
        Assert.Contains(RelationshipTypes.HAS_NESTED_SECTION, relationshipTypes);
        Assert.Contains(RelationshipTypes.HAS_SUBSECTION, relationshipTypes);
        Assert.Contains(RelationshipTypes.CONTAINS, relationshipTypes);

        // Should detect links
        var allLinks = elements.SelectMany(e => e.Links).ToList();
        Assert.True(allLinks.Count >= 3, $"Should detect at least 3 links, found {allLinks.Count}");
        
        var linkTypes = allLinks.Select(l => l.Type).Distinct().ToList();
        Assert.Contains(LinkType.Internal, linkTypes);
        Assert.Contains(LinkType.External, linkTypes);
        Assert.Contains(LinkType.Email, linkTypes);
    }
}