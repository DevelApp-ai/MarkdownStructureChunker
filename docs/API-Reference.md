# API Reference

This document provides detailed information about the public APIs available in the MarkdownStructureChunker library.

## Core Classes

### StructureChunker

The main orchestrator class that coordinates document processing.

```csharp
public class StructureChunker
{
    public StructureChunker(IChunkingStrategy strategy, IKeywordExtractor extractor)
    
    public async Task<DocumentGraph> ProcessAsync(string document, string sourceId)
    public DocumentGraph Process(string document, string sourceId)
}
```

**Parameters:**
- `strategy`: Implementation of `IChunkingStrategy` for pattern recognition
- `extractor`: Implementation of `IKeywordExtractor` for keyword extraction
- `document`: The document content to process
- `sourceId`: Unique identifier for the document

**Returns:** `DocumentGraph` containing the structured chunks

**Example:**
```csharp
var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
var extractor = new SimpleKeywordExtractor();
var chunker = new StructureChunker(strategy, extractor);

var result = await chunker.ProcessAsync(document, "doc-001");
```

## Data Models

### DocumentGraph

Represents the complete structured document with all chunks.

```csharp
public record DocumentGraph
{
    public string SourceId { get; init; }
    public List<ChunkNode> Chunks { get; init; }
    public DateTime ProcessedAt { get; init; }
}
```

**Properties:**
- `SourceId`: Unique identifier for the source document
- `Chunks`: List of all extracted chunks in processing order
- `ProcessedAt`: Timestamp when processing completed

### ChunkNode

Represents an individual chunk of content with metadata.

```csharp
public record ChunkNode
{
    public Guid Id { get; init; }
    public string ChunkType { get; init; }
    public int Level { get; init; }
    public string RawTitle { get; init; }
    public string CleanTitle { get; init; }
    public string Content { get; init; }
    public List<string> Keywords { get; init; }
    public Guid? ParentId { get; init; }
}
```

**Properties:**
- `Id`: Unique identifier for the chunk
- `ChunkType`: Type of pattern matched (e.g., "MarkdownH1", "Numeric", "Legal")
- `Level`: Hierarchical level (1 = top level, 2 = subsection, etc.)
- `RawTitle`: Original title text as found in document
- `CleanTitle`: Cleaned title with formatting removed
- `Content`: Full content of the chunk including nested content
- `Keywords`: Extracted keywords for the chunk
- `ParentId`: ID of parent chunk (null for top-level chunks)

### ChunkingRule

Defines pattern matching rules for document structure recognition.

```csharp
public class ChunkingRule
{
    public ChunkingRule(string type, string pattern, int level, int priority = 0)
    public ChunkingRule(string type, string pattern, int priority = 0)
    
    public string Type { get; }
    public Regex Pattern { get; }
    public int? FixedLevel { get; }
    public int Priority { get; }
    
    public ChunkingMatch? TryMatch(string line)
}
```

**Constructors:**
- Fixed level constructor: Creates rule with predetermined hierarchical level
- Dynamic level constructor: Creates rule that calculates level from content

**Properties:**
- `Type`: Identifier for the rule type
- `Pattern`: Compiled regex pattern for matching
- `FixedLevel`: Fixed hierarchical level (null for dynamic calculation)
- `Priority`: Processing priority (lower numbers = higher priority)

**Methods:**
- `TryMatch(string line)`: Attempts to match pattern against input line

## Interfaces

### IChunkingStrategy

Defines the contract for document structure analysis strategies.

```csharp
public interface IChunkingStrategy
{
    List<ChunkNode> ProcessText(string text, string sourceId);
}
```

**Methods:**
- `ProcessText`: Analyzes text and returns structured chunks

**Implementations:**
- `PatternBasedStrategy`: Uses regex patterns to identify document structure

### IKeywordExtractor

Defines the contract for keyword extraction from text content.

```csharp
public interface IKeywordExtractor
{
    Task<List<string>> ExtractKeywordsAsync(string content, int maxKeywords = 10);
}
```

**Methods:**
- `ExtractKeywordsAsync`: Extracts keywords from content with optional limit

**Implementations:**
- `SimpleKeywordExtractor`: Frequency-based keyword extraction
- `MLNetKeywordExtractor`: ML.NET-powered keyword extraction

### ILocalVectorizer

Defines the contract for text vectorization using local models.

```csharp
public interface ILocalVectorizer
{
    int VectorDimension { get; }
    Task<float[]> VectorizeAsync(string text, bool isQuery = false);
}
```

**Properties:**
- `VectorDimension`: Dimension of generated vectors

**Methods:**
- `VectorizeAsync`: Converts text to vector embedding

**Implementations:**
- `OnnxVectorizer`: ONNX-based vectorization with E5 model support

## Strategy Implementations

### PatternBasedStrategy

Default implementation using regex patterns for structure recognition.

```csharp
public class PatternBasedStrategy : IChunkingStrategy
{
    public PatternBasedStrategy(List<ChunkingRule> rules)
    
    public static List<ChunkingRule> CreateDefaultRules()
    public List<ChunkNode> ProcessText(string text, string sourceId)
}
```

**Methods:**
- `CreateDefaultRules()`: Returns pre-configured rules for common patterns
- `ProcessText()`: Processes document using configured rules

**Default Patterns:**
- Markdown headings: `# ## ### #### ##### ######`
- Numeric outlines: `1. 1.1 1.1.1 2.3.4.5`
- Legal sections: `§ 42 Section Title`
- Appendices: `Appendix A: Title`
- Letter outlines: `A. B. C.`

## Extractor Implementations

### SimpleKeywordExtractor

Basic frequency-based keyword extraction.

```csharp
public class SimpleKeywordExtractor : IKeywordExtractor
{
    public async Task<List<string>> ExtractKeywordsAsync(string content, int maxKeywords = 10)
}
```

**Features:**
- Stop word filtering
- Frequency-based ranking
- Minimum word length filtering
- Case normalization

### MLNetKeywordExtractor

Advanced keyword extraction using ML.NET.

```csharp
public class MLNetKeywordExtractor : IKeywordExtractor, IDisposable
{
    public async Task<List<string>> ExtractKeywordsAsync(string content, int maxKeywords = 10)
    public void Dispose()
}
```

**Features:**
- Text featurization using ML.NET
- Advanced tokenization
- TF-IDF scoring
- Automatic resource cleanup

## Vectorizer Implementations

### OnnxVectorizer

ONNX-based text vectorization with E5 model support.

```csharp
public class OnnxVectorizer : ILocalVectorizer, IDisposable
{
    public OnnxVectorizer(string? modelPath = null)
    
    public int VectorDimension { get; } // Returns 1024
    
    public async Task<float[]> VectorizeAsync(string text, bool isQuery = false)
    public static string EnrichContentWithContext(string chunkContent, IEnumerable<string> ancestralTitles)
    public void Dispose()
}
```

**Features:**
- Support for intfloat/multilingual-e5-large model
- Proper "passage:" and "query:" prefixing
- Context enrichment with ancestral titles
- Fallback placeholder implementation
- Vector normalization

### OnnxVectorizerFactory

Factory for creating vectorizer instances.

```csharp
public static class OnnxVectorizerFactory
{
    public static OnnxVectorizer CreateDefault()
    public static OnnxVectorizer CreateWithPath(string modelPath)
    public static OnnxVectorizer CreatePlaceholder()
}
```

**Methods:**
- `CreateDefault()`: Creates vectorizer with default model paths
- `CreateWithPath()`: Creates vectorizer with custom model path
- `CreatePlaceholder()`: Creates placeholder vectorizer for testing

## Error Handling

### Common Exceptions

**ArgumentException**
- Thrown when invalid parameters are provided
- Common scenarios: empty document content, invalid source ID

**InvalidOperationException**
- Thrown when operations cannot be completed
- Common scenarios: processing errors, invalid state

**Example Error Handling:**
```csharp
try
{
    var result = await chunker.ProcessAsync(document, sourceId);
}
catch (ArgumentException ex)
{
    // Handle invalid input
    Console.WriteLine($"Invalid input: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // Handle processing errors
    Console.WriteLine($"Processing error: {ex.Message}");
}
```

## Performance Considerations

### Memory Usage
- Documents are processed in memory
- Large documents (>10MB) may require chunking
- ML.NET components cache models in memory

### Threading
- All components are thread-safe
- Supports concurrent document processing
- Use `ConfigureAwait(false)` for library calls in ASP.NET

### Initialization Costs
- ML.NET: 1-2 seconds first-time initialization
- ONNX models: 2-3 seconds + ~500MB RAM
- Simple extractors: Minimal initialization cost

## Best Practices

### Resource Management
```csharp
// Dispose of ML.NET extractors
using var extractor = new MLNetKeywordExtractor();

// Dispose of ONNX vectorizers
using var vectorizer = OnnxVectorizerFactory.CreateDefault();
```

### Dependency Injection
```csharp
// Register as singletons for better performance
services.AddSingleton<IChunkingStrategy>(provider => 
    new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules()));
services.AddSingleton<IKeywordExtractor, MLNetKeywordExtractor>();
services.AddSingleton<StructureChunker>();
```

### Custom Rules
```csharp
// Order rules by priority (lower = higher priority)
var rules = new List<ChunkingRule>
{
    new ChunkingRule("HighPriority", @"pattern1", level: 1, priority: 0),
    new ChunkingRule("LowPriority", @"pattern2", level: 2, priority: 10)
};
```

### Batch Processing
```csharp
// Process multiple documents concurrently
var tasks = documents.Select(doc => chunker.ProcessAsync(doc.Content, doc.Id));
var results = await Task.WhenAll(tasks);
```

## Version Compatibility

- **Target Framework**: .NET 8.0
- **ML.NET**: 3.0.1 or later
- **ONNX Runtime**: 1.22.1 or later
- **System.Numerics.Tensors**: 9.0.7 or later

## Migration Guide

### From Version 1.x to 2.x
- Update NuGet package references
- Replace deprecated method calls
- Update custom rule implementations

### Breaking Changes
- Interface signatures may change between major versions
- Check release notes for specific migration steps

