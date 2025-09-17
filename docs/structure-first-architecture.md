# Structure-First Ingestion Architecture

This document describes the new structure-first ingestion architecture implemented in MarkdownStructureChunker, which provides enhanced document processing using Abstract Syntax Tree (AST) parsing with Markdig.

## Overview

The structure-first architecture addresses the limitations of traditional text chunking by:

1. **Parsing documents to AST first**: Using Markdig to create a robust Abstract Syntax Tree representation
2. **Creating structural elements**: Converting AST nodes to graph-native structural elements  
3. **Establishing relationships**: Building explicit hierarchical and containment relationships
4. **Maintaining compatibility**: Preserving backward compatibility with existing chunk-based APIs

## Key Components

### StructuralElement

Represents individual elements from the document's AST:

```csharp
var element = new StructuralElement
{
    ElementType = "heading",           // heading, paragraph, list, code_block
    Content = "Machine Learning",      // Extracted text content
    Level = 2,                        // Hierarchical level (for headings)
    StartOffset = 150,                // Character position in document
    EndOffset = 167,                  // End position
    OriginalMarkdown = "## Machine Learning", // Original markdown
    SourceId = "doc-123"
};
```

### GraphEdge

Represents relationships between structural elements:

```csharp
var edge = new GraphEdge
{
    SourceElementId = parentHeading.Id,
    TargetElementId = childSection.Id,
    RelationshipType = RelationshipTypes.HAS_SUBSECTION
};
```

### Enhanced DocumentGraph

The DocumentGraph now supports both traditional chunks and structural elements:

```csharp
public record DocumentGraph
{
    // Traditional API (backward compatible)
    public IReadOnlyList<ChunkNode> Chunks { get; init; }
    
    // Structure-first API
    public IReadOnlyList<StructuralElement> StructuralElements { get; init; }
    public IReadOnlyList<GraphEdge> StructuralEdges { get; init; }
    
    // Navigation methods
    public IEnumerable<StructuralElement> RootElements { get; }
    public IEnumerable<StructuralElement> GetChildElements(Guid elementId);
    public StructuralElement? GetParentElement(Guid elementId);
}
```

## Usage Examples

### Basic Structure-First Processing

```csharp
using MarkdownStructureChunker.Core;

// Create structure-first chunker
using var chunker = StructureChunker.CreateStructureFirst();

var markdown = @"
# Machine Learning Guide

## Supervised Learning
Supervised learning uses labeled data.

### Classification
Predicts discrete categories.

### Regression  
Predicts continuous values.

## Unsupervised Learning
Finds patterns in unlabeled data.
";

// Process with structure-first approach
var result = await chunker.ProcessWithStructureAsync(markdown, "ml-guide");

Console.WriteLine($"Structural elements: {result.StructuralElements.Count}");
Console.WriteLine($"Relationships: {result.StructuralEdges.Count}");
Console.WriteLine($"Has structural graph: {result.HasStructuralGraph}");
```

### Exploring Document Structure

```csharp
// Find root headings
var rootHeadings = result.RootElements
    .Where(e => e.ElementType == "heading")
    .ToList();

foreach (var heading in rootHeadings)
{
    Console.WriteLine($"Root: {heading.Content}");
    
    // Get child sections
    var children = result.GetChildElements(heading.Id);
    foreach (var child in children.Where(c => c.ElementType == "heading"))
    {
        Console.WriteLine($"  Child: {child.Content}");
    }
}
```

### Analyzing Relationships

```csharp
// Group relationships by type
var relationshipGroups = result.StructuralEdges.GroupBy(e => e.RelationshipType);

foreach (var group in relationshipGroups)
{
    Console.WriteLine($"{group.Key}: {group.Count()} relationships");
}

// Find hierarchical relationships
var hierarchicalEdges = result.StructuralEdges
    .Where(e => e.RelationshipType == RelationshipTypes.HAS_SUBSECTION)
    .ToList();

Console.WriteLine($"Hierarchical relationships: {hierarchicalEdges.Count}");
```

### Working with Different Element Types

```csharp
// Get elements by type
var headings = result.StructuralElements
    .Where(e => e.ElementType == "heading")
    .OrderBy(e => e.StartOffset)
    .ToList();

var codeBlocks = result.StructuralElements
    .Where(e => e.ElementType == "code_block")
    .ToList();

var lists = result.StructuralElements
    .Where(e => e.ElementType == "list")
    .ToList();

Console.WriteLine($"Found {headings.Count} headings, {codeBlocks.Count} code blocks, {lists.Count} lists");
```

## Relationship Types

The system supports several relationship types:

- **HAS_SUBSECTION**: Parent heading to child heading (h1 → h2)
- **CONTAINS**: Section contains content (heading → paragraph)
- **FOLLOWS**: Sequential relationship (paragraph A → paragraph B)
- **PRECEDES**: Reverse sequential relationship
- **SIBLING**: Same hierarchical level
- **PARENT_OF**: Inverse of HAS_SUBSECTION

## Backward Compatibility

The structure-first approach maintains full backward compatibility:

```csharp
// Traditional API still works
var chunks = await chunker.ChunkAsync(markdown);

// ProcessAsync still returns traditional chunks
var traditionalResult = await chunker.ProcessAsync(markdown, "doc-id");

// Structure-first is opt-in via ProcessWithStructureAsync
var structuralResult = await chunker.ProcessWithStructureAsync(markdown, "doc-id");
```

## Factory Methods

### CreateStructureFirst()

Creates a chunker configured for structure-first processing:

```csharp
using var chunker = StructureChunker.CreateStructureFirst();
// Uses ASTBasedStrategy with SimpleKeywordExtractor
```

### CreateStructureFirstWithConfiguration()

Creates a structure-first chunker with custom configuration:

```csharp
var config = ChunkerConfiguration.CreateDefault();
using var chunker = StructureChunker.CreateStructureFirstWithConfiguration(config);
```

## Benefits of Structure-First Architecture

1. **Precise Structure Capture**: AST parsing captures exact document structure
2. **Explicit Relationships**: Clear parent-child and containment relationships
3. **Better Navigation**: Easy traversal of document hierarchy
4. **Rich Metadata**: Preserve original markdown, offsets, and element types
5. **Graph-Ready**: Direct mapping to graph databases and knowledge graphs
6. **Extensible**: Easy to add semantic analysis on top of structural foundation

## Integration with Aevenalytics

The structure-first architecture serves as the foundation for semantic enrichment:

1. **MarkdownStructureChunker** creates the structural scaffold
2. **Aevenalytics** adds semantic layer (entities, relationships, embeddings)
3. **Result**: Multi-layered knowledge graph with both structure and semantics

This separation of concerns enables:
- Deterministic structural processing
- Probabilistic semantic analysis  
- Independent updates to either layer
- Robust, scalable document processing pipeline

## Performance Considerations

- **Memory**: Structural elements require additional memory vs. flat chunks
- **Processing**: AST parsing has slight overhead vs. regex patterns
- **Benefits**: More accurate structure detection, fewer parsing errors
- **Recommendation**: Use structure-first for complex documents requiring precise structure

## Migration Guide

Existing applications can gradually adopt structure-first processing:

1. **Phase 1**: Continue using existing APIs
2. **Phase 2**: Try `ProcessWithStructureAsync()` alongside existing processing
3. **Phase 3**: Leverage structural elements for enhanced functionality
4. **Phase 4**: Transition primary workflows to structure-first approach

The dual API approach ensures zero breaking changes while enabling enhanced capabilities.