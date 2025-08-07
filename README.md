# MarkdownStructureChunker

A powerful .NET library for intelligent document structure analysis and chunking, designed to extract hierarchical content from various document formats with advanced keyword extraction and vectorization capabilities.

## Features

- **Pattern-Based Structure Recognition**: Automatically identifies and parses various document patterns including Markdown headings, numeric outlines, legal sections, and appendices
- **Hierarchical Content Organization**: Maintains parent-child relationships between document sections for contextual understanding
- **Advanced Keyword Extraction**: Supports both simple frequency-based and ML.NET-powered keyword extraction
- **ONNX Vectorization**: Integration with the intfloat/multilingual-e5-large model for semantic embeddings
- **Extensible Architecture**: Plugin-based design allows for custom chunking strategies and extractors
- **Comprehensive Testing**: 66+ unit and integration tests ensuring reliability

## Quick Start

### Installation

#### Via NuGet (Recommended)
```bash
dotnet add package MarkdownStructureChunker
```

#### Via Source Code
```bash
# Clone the repository
git clone https://github.com/DevelApp-ai/MarkdownStructureChunker.git
cd MarkdownStructureChunker

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Basic Usage

```csharp
using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Extractors;
using MarkdownStructureChunker.Core.Strategies;

// Create chunking strategy and keyword extractor
var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
var extractor = new SimpleKeywordExtractor();

// Initialize the chunker
var chunker = new StructureChunker(strategy, extractor);

// Process a document
var document = @"
# Introduction
This document introduces machine learning concepts.

## Background
Machine learning is a subset of artificial intelligence.

### Applications
ML has numerous applications in various industries.
";

var result = await chunker.ProcessAsync(document, "ml-guide");

// Access the structured chunks
foreach (var chunk in result.Chunks)
{
    Console.WriteLine($"Level {chunk.Level}: {chunk.CleanTitle}");
    Console.WriteLine($"Keywords: {string.Join(", ", chunk.Keywords)}");
    Console.WriteLine($"Content: {chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length))}...");
    Console.WriteLine();
}
```

## Supported Document Patterns

### Markdown Headings
```markdown
# Level 1 Heading
## Level 2 Heading
### Level 3 Heading
#### Level 4 Heading
##### Level 5 Heading
###### Level 6 Heading
```

### Numeric Outlines
```
1. First Level
1.1 Second Level
1.1.1 Third Level
1.2 Another Second Level
2. Another First Level
```

### Legal Sections
```
§ 42 Compliance Requirements
§ 43 Data Protection Standards
```

### Appendices
```
Appendix A: Technical Specifications
Appendix B: Reference Materials
```

### Letter Outlines
```
A. First Section
B. Second Section
C. Third Section
```

## Architecture

The library follows a modular architecture with clear separation of concerns:

```
MarkdownStructureChunker.Core/
├── Models/
│   ├── ChunkNode.cs          # Individual chunk data structure
│   ├── DocumentGraph.cs      # Complete document structure
│   └── ChunkingRule.cs       # Pattern matching rules
├── Interfaces/
│   ├── IChunkingStrategy.cs  # Strategy pattern interface
│   ├── IKeywordExtractor.cs  # Keyword extraction interface
│   └── ILocalVectorizer.cs   # Vectorization interface
├── Strategies/
│   └── PatternBasedStrategy.cs # Default pattern-based implementation
├── Extractors/
│   ├── SimpleKeywordExtractor.cs # Frequency-based extraction
│   └── MLNetKeywordExtractor.cs  # ML.NET-powered extraction
├── Vectorizers/
│   └── OnnxVectorizer.cs     # ONNX model integration
└── StructureChunker.cs       # Main orchestrator class
```

## Advanced Usage

### Custom Chunking Rules

```csharp
// Create custom rules for specific document patterns
var customRules = new List<ChunkingRule>
{
    new ChunkingRule("CustomHeader", @"^SECTION\s+(\d+):\s+(.*)", level: 1, priority: 0),
    new ChunkingRule("Subsection", @"^(\d+\.\d+)\s+(.*)", priority: 10),
    // Add more custom patterns as needed
};

var strategy = new PatternBasedStrategy(customRules);
```

### ML.NET Keyword Extraction

```csharp
// Use ML.NET for more sophisticated keyword extraction
using var mlExtractor = new MLNetKeywordExtractor();
var chunker = new StructureChunker(strategy, mlExtractor);

var result = await chunker.ProcessAsync(document, "doc-id");
```

### ONNX Vectorization

```csharp
// Initialize with ONNX model for semantic embeddings
using var vectorizer = OnnxVectorizerFactory.CreateDefault();

// Vectorize chunk content with context
var enrichedContent = OnnxVectorizer.EnrichContentWithContext(
    chunk.Content, 
    GetAncestralTitles(chunk)
);

var embedding = await vectorizer.VectorizeAsync(enrichedContent, isQuery: false);
```

## Configuration

### Default Chunking Rules

The library comes with pre-configured rules that handle common document patterns:

1. **Markdown Headings** (Priority 0-6): `# ## ### #### ##### ######`
2. **Numeric Outlines** (Priority 10): `1. 1.1 1.1.1 2.3.4.5`
3. **Legal Sections** (Priority 20): `§ 42 Section Title`
4. **Appendices** (Priority 30): `Appendix A: Title`
5. **Letter Outlines** (Priority 40): `A. B. C.`

### Keyword Extraction Options

```csharp
// Simple extractor with custom parameters
var simpleExtractor = new SimpleKeywordExtractor();
var keywords = await simpleExtractor.ExtractKeywordsAsync(text, maxKeywords: 10);

// ML.NET extractor with advanced processing
using var mlExtractor = new MLNetKeywordExtractor();
var advancedKeywords = await mlExtractor.ExtractKeywordsAsync(text, maxKeywords: 15);
```

## Performance Considerations

- **Memory Usage**: The library processes documents in memory. For very large documents (>10MB), consider chunking the input
- **ML.NET Performance**: First-time initialization of ML.NET components may take 1-2 seconds
- **ONNX Model Loading**: Loading the multilingual-e5-large model requires ~500MB RAM and 2-3 seconds initialization
- **Concurrent Processing**: All components are thread-safe and support concurrent document processing

## Integration Examples

### ASP.NET Core Web API

```csharp
[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly StructureChunker _chunker;

    public DocumentController(StructureChunker chunker)
    {
        _chunker = chunker;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeDocument([FromBody] DocumentRequest request)
    {
        try
        {
            var result = await _chunker.ProcessAsync(request.Content, request.DocumentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error processing document: {ex.Message}");
        }
    }
}
```

### Dependency Injection Setup

```csharp
// Program.cs or Startup.cs
services.AddSingleton<IChunkingStrategy>(provider => 
    new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules()));
services.AddSingleton<IKeywordExtractor, MLNetKeywordExtractor>();
services.AddSingleton<StructureChunker>();
```

### Batch Processing

```csharp
public async Task ProcessDocumentBatch(IEnumerable<string> documents)
{
    var tasks = documents.Select(async (doc, index) =>
    {
        var result = await chunker.ProcessAsync(doc, $"doc-{index}");
        return result;
    });

    var results = await Task.WhenAll(tasks);
    
    // Process results...
}
```

## Error Handling

The library provides comprehensive error handling:

```csharp
try
{
    var result = await chunker.ProcessAsync(document, documentId);
}
catch (ArgumentException ex)
{
    // Handle invalid input parameters
    Console.WriteLine($"Invalid input: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // Handle processing errors
    Console.WriteLine($"Processing error: {ex.Message}");
}
catch (Exception ex)
{
    // Handle unexpected errors
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Testing

The library includes comprehensive test coverage:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Integration
```

Test categories:
- **Unit Tests**: Individual component testing
- **Integration Tests**: End-to-end workflow testing
- **Performance Tests**: Benchmarking and load testing

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Make your changes and add tests
4. Ensure all tests pass: `dotnet test`
5. Commit your changes: `git commit -m "Add your feature"`
6. Push to the branch: `git push origin feature/your-feature`
7. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

- [ ] Support for PDF document parsing
- [ ] Integration with Azure Cognitive Services
- [ ] Support for custom ONNX models
- [ ] Performance optimizations for large documents
- [ ] Additional language support for keyword extraction
- [ ] Real-time document processing capabilities

## Support

For questions, issues, or contributions, please:
- Open an issue on GitHub
- Check the [documentation](docs/)
- Review the [examples](examples/)

---

**MarkdownStructureChunker** - Intelligent document structure analysis for modern applications.

