# Getting Started with MarkdownStructureChunker

MarkdownStructureChunker is a powerful .NET library for intelligently parsing and chunking markdown documents while preserving their hierarchical structure. This guide will help you get started with both basic and advanced features.

## Installation

Install the package via NuGet:

```bash
dotnet add package MarkdownStructureChunker
```

Or via Package Manager Console:

```powershell
Install-Package MarkdownStructureChunker
```

## Quick Start

### Basic Usage (Legacy API)

```csharp
using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Strategies;
using MarkdownStructureChunker.Core.Extractors;

// Create chunker with default settings
var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
var extractor = new MLNetKeywordExtractor();
using var chunker = new StructureChunker(strategy, extractor);

// Process markdown content
var markdown = @"
# Chapter 1: Introduction
This is the introduction to our document.

## Section 1.1: Overview
Here's an overview of the topic.

### Subsection 1.1.1: Details
Detailed information goes here.
";

var chunks = await chunker.ProcessAsync(markdown, "document-1");

foreach (var chunk in chunks)
{
    Console.WriteLine($"Title: {chunk.CleanTitle}");
    Console.WriteLine($"Level: {chunk.Level}");
    Console.WriteLine($"Content: {chunk.Content}");
    Console.WriteLine($"Keywords: {string.Join(", ", chunk.Keywords)}");
    Console.WriteLine("---");
}
```

### Modern Configuration-Based API (v1.1.0+)

```csharp
using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Configuration;

// Create configuration
var config = new ChunkerConfiguration
{
    MaxChunkSize = 1000,
    ChunkOverlap = 200,
    PreserveStructure = true,
    SplitOnSentences = true,
    PreserveOriginalMarkdown = true,
    CalculateOffsets = true,
    ExtractKeywords = true
};

// Create chunker with configuration
using var chunker = new StructureChunker(config);

// Process with async support and cancellation
var cancellationToken = new CancellationTokenSource().Token;
var chunks = await chunker.ChunkAsync(markdown, cancellationToken);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Title: {chunk.CleanTitle}");
    Console.WriteLine($"Type: {chunk.ChunkTypeEnum}");
    Console.WriteLine($"Level: {chunk.SectionLevel}");
    Console.WriteLine($"Is Heading: {chunk.IsHeading}");
    Console.WriteLine($"Parent: {chunk.ParentHeading}");
    Console.WriteLine($"Offsets: {chunk.StartOffset}-{chunk.EndOffset}");
    Console.WriteLine($"Hierarchy: {string.Join(" > ", chunk.HeadingHierarchy)}");
    Console.WriteLine($"Content: {chunk.Content}");
    Console.WriteLine($"Children Count: {chunk.Children.Count}");
    
    if (chunk.PreserveOriginalMarkdown)
    {
        Console.WriteLine($"Original Markdown: {chunk.OriginalMarkdown}");
    }
    
    Console.WriteLine("---");
}
```

## Configuration Options

### ChunkerConfiguration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxChunkSize` | `int` | 1000 | Maximum size of a chunk in characters |
| `MinChunkSize` | `int` | 100 | Minimum size of a chunk in characters |
| `ChunkOverlap` | `int` | 200 | Overlap between chunks in characters |
| `PreserveStructure` | `bool` | true | Preserve document structure when chunking |
| `SplitOnSentences` | `bool` | true | Split content on sentence boundaries |
| `RespectSectionBoundaries` | `bool` | true | Don't cross major section boundaries |
| `IncludeHeadingHierarchy` | `bool` | true | Include heading hierarchy in chunk metadata |
| `ExtractKeywords` | `bool` | true | Extract keywords from chunk content |
| `MaxKeywordsPerChunk` | `int` | 10 | Maximum number of keywords to extract per chunk |
| `CalculateOffsets` | `bool` | true | Calculate precise character offsets for chunks |
| `PreserveOriginalMarkdown` | `bool` | false | Preserve original markdown formatting |

### Factory Methods

```csharp
// Default configuration
var defaultConfig = ChunkerConfiguration.CreateDefault();

// Optimized for large documents
var largeDocConfig = ChunkerConfiguration.CreateForLargeDocuments();

// Optimized for small documents
var smallDocConfig = ChunkerConfiguration.CreateForSmallDocuments();

// Performance-optimized configuration
var performanceConfig = ChunkerConfiguration.CreateForPerformance();
```

## Advanced Features

### Hierarchical Navigation

```csharp
var chunks = await chunker.ChunkAsync(markdown);

// Navigate the document hierarchy
foreach (var chunk in chunks.Where(c => c.Parent == null)) // Root level chunks
{
    Console.WriteLine($"Root: {chunk.CleanTitle}");
    
    // Navigate children
    foreach (var child in chunk.Children)
    {
        Console.WriteLine($"  Child: {child.CleanTitle}");
        
        // Navigate grandchildren
        foreach (var grandchild in child.Children)
        {
            Console.WriteLine($"    Grandchild: {grandchild.CleanTitle}");
        }
    }
}
```

### Working with Offsets

```csharp
var config = new ChunkerConfiguration
{
    CalculateOffsets = true,
    PreserveOriginalMarkdown = true
};

using var chunker = new StructureChunker(config);
var chunks = await chunker.ChunkAsync(markdown);

foreach (var chunk in chunks)
{
    // Extract the exact content from original document
    var originalContent = markdown.Substring(chunk.StartOffset, 
        chunk.EndOffset - chunk.StartOffset);
    
    Console.WriteLine($"Chunk at {chunk.StartOffset}-{chunk.EndOffset}:");
    Console.WriteLine(originalContent);
    Console.WriteLine("---");
}
```

### Chunk Type Classification

```csharp
var chunks = await chunker.ChunkAsync(markdown);

// Group chunks by type
var chunksByType = chunks.GroupBy(c => c.ChunkTypeEnum);

foreach (var group in chunksByType)
{
    Console.WriteLine($"{group.Key}: {group.Count()} chunks");
    
    foreach (var chunk in group)
    {
        Console.WriteLine($"  - {chunk.CleanTitle}");
    }
}
```

### ONNX Vectorization

For semantic embeddings, you can use the ONNX vectorizer:

```csharp
using MarkdownStructureChunker.Core.Vectorizers;

// Basic usage (deterministic fallback)
using var vectorizer = new OnnxVectorizer();
var vector = await vectorizer.VectorizeAsync("Your text here");

// With ONNX model (requires model download)
var vectorizerWithModel = OnnxVectorizer.CreateWithPaths(
    modelPath: "/path/to/model.onnx",
    tokenizerPath: "/path/to/tokenizer.json"
);
var semanticVector = await vectorizerWithModel.VectorizeAsync("Your text here");

// Batch processing
var texts = new[] { "Text 1", "Text 2", "Text 3" };
var vectors = await vectorizer.VectorizeBatchAsync(texts);
```

For ONNX model setup, see the [ONNX Setup Guide](onnx-setup/README.md).

## Error Handling

```csharp
try
{
    var config = new ChunkerConfiguration
    {
        MaxChunkSize = 1000,
        MinChunkSize = 100
    };
    
    // Validate configuration
    config.Validate();
    
    using var chunker = new StructureChunker(config);
    var chunks = await chunker.ChunkAsync(markdown);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

## Performance Considerations

### For Large Documents

```csharp
var config = ChunkerConfiguration.CreateForLargeDocuments();
config.PreserveOriginalMarkdown = false; // Save memory
config.ExtractKeywords = false; // Skip expensive keyword extraction

using var chunker = new StructureChunker(config);
var chunks = await chunker.ChunkAsync(largeMarkdown);
```

### For High-Performance Scenarios

```csharp
var config = ChunkerConfiguration.CreateForPerformance();
using var chunker = new StructureChunker(config);

// Process multiple documents in parallel
var tasks = documents.Select(async doc => 
    await chunker.ChunkAsync(doc.Content));
var results = await Task.WhenAll(tasks);
```

## Migration from v1.0.x

If you're upgrading from v1.0.x, the legacy API remains fully supported:

```csharp
// v1.0.x code continues to work
var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
var extractor = new MLNetKeywordExtractor();
using var chunker = new StructureChunker(strategy, extractor);
var chunks = await chunker.ProcessAsync(markdown, "doc-id");

// New v1.1.0+ features are available via the configuration API
var config = ChunkerConfiguration.CreateDefault();
using var newChunker = new StructureChunker(config);
var enhancedChunks = await newChunker.ChunkAsync(markdown);
```

## Next Steps

- Explore the [ONNX Setup Guide](onnx-setup/README.md) for semantic embeddings
- Check out the [Container Examples](onnx-setup/container-examples.md) for deployment
- Review the [API Reference](api-reference.md) for detailed documentation
- See [Examples](examples/) for more usage patterns

## Support

For issues, questions, or contributions, please visit our [GitHub repository](https://github.com/DevelApp-ai/MarkdownStructureChunker).

