# Getting Started with MarkdownStructureChunker

This guide will help you get up and running with the MarkdownStructureChunker library quickly and efficiently.

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** or **Visual Studio Code** (recommended)
- **Git** for version control

## Installation

### Option 1: Clone and Build from Source

```bash
# Clone the repository
git clone https://github.com/DevelApp-ai/MarkdownStructureChunker.git
cd MarkdownStructureChunker

# Build the solution
dotnet build

# Run tests to verify installation
dotnet test
```

### Option 2: Add as Project Reference

If you want to include the library in your existing project:

```bash
# Add the core library as a project reference
dotnet add reference path/to/MarkdownStructureChunker/Core/Core.csproj
```

### Option 3: NuGet Package (Future)

```bash
# Install via NuGet (when published)
dotnet add package MarkdownStructureChunker
```

## Your First Document Processing

Let's start with a simple example to process a Markdown document:

### Step 1: Create a New Console Application

```bash
dotnet new console -n MyDocumentProcessor
cd MyDocumentProcessor
```

### Step 2: Add Reference to MarkdownStructureChunker

```bash
dotnet add reference ../MarkdownStructureChunker/Core/Core.csproj
```

### Step 3: Write Your First Program

Create or update `Program.cs`:

```csharp
using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Extractors;
using MarkdownStructureChunker.Core.Strategies;

// Sample document content
var document = @"
# Getting Started with AI

Artificial Intelligence is transforming how we solve complex problems.

## Machine Learning Basics

Machine learning enables computers to learn from data without explicit programming.

### Supervised Learning

Supervised learning uses labeled data to train predictive models.

### Unsupervised Learning

Unsupervised learning finds patterns in data without labeled examples.

## Deep Learning

Deep learning uses neural networks with multiple layers for complex pattern recognition.
";

// Create the processing components
var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
var extractor = new SimpleKeywordExtractor();
var chunker = new StructureChunker(strategy, extractor);

// Process the document
try
{
    var result = await chunker.ProcessAsync(document, "ai-guide-001");
    
    Console.WriteLine($"Successfully processed document: {result.SourceId}");
    Console.WriteLine($"Found {result.Chunks.Count} chunks\n");
    
    // Display the results
    foreach (var chunk in result.Chunks)
    {
        Console.WriteLine($"📄 {chunk.CleanTitle} (Level {chunk.Level})");
        Console.WriteLine($"   Type: {chunk.ChunkType}");
        Console.WriteLine($"   Keywords: {string.Join(", ", chunk.Keywords)}");
        Console.WriteLine($"   Parent: {(chunk.ParentId.HasValue ? "Yes" : "Root")}");
        Console.WriteLine();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error processing document: {ex.Message}");
}
```

### Step 4: Run Your Program

```bash
dotnet run
```

You should see output similar to:

```
Successfully processed document: ai-guide-001
Found 5 chunks

📄 Getting Started with AI (Level 1)
   Type: MarkdownH1
   Keywords: artificial, intelligence, transforming, complex, problems
   Parent: Root

📄 Machine Learning Basics (Level 2)
   Type: MarkdownH2
   Keywords: machine, learning, computers, data, programming
   Parent: Yes

📄 Supervised Learning (Level 3)
   Type: MarkdownH3
   Keywords: supervised, learning, labeled, data, models
   Parent: Yes

📄 Unsupervised Learning (Level 3)
   Type: MarkdownH3
   Keywords: unsupervised, learning, patterns, data, examples
   Parent: Yes

📄 Deep Learning (Level 2)
   Type: MarkdownH2
   Keywords: deep, learning, neural, networks, recognition
   Parent: Yes
```

## Understanding the Output

Each chunk contains several important pieces of information:

- **CleanTitle**: The section title with formatting removed
- **Level**: Hierarchical depth (1 = main section, 2 = subsection, etc.)
- **ChunkType**: The pattern type that was matched (MarkdownH1, MarkdownH2, etc.)
- **Keywords**: Automatically extracted keywords from the content
- **Parent**: Whether this chunk has a parent section

## Working with Different Document Types

### Numeric Outlines

The library automatically recognizes numeric outline patterns:

```csharp
var numericDocument = @"
1. Project Overview
This project aims to develop an advanced document processing system.

1.1 Objectives
The primary objectives include automated analysis and classification.

1.1.1 Primary Goals
Achieve high accuracy in document parsing and content extraction.

1.2 Scope
The scope covers various document formats and processing techniques.

2. Technical Requirements
The system must meet specific performance and scalability requirements.
";

var result = await chunker.ProcessAsync(numericDocument, "project-doc");
```

### Legal Documents

Legal section patterns are also supported:

```csharp
var legalDocument = @"
# Software License Agreement

## § 1 Grant of License
The licensor grants the licensee the right to use the software.

## § 2 Restrictions
The licensee shall not reverse engineer the software.

## § 3 Termination
This agreement terminates upon breach of terms.

Appendix A: Technical Requirements
The software requires .NET 8.0 runtime environment.
";

var result = await chunker.ProcessAsync(legalDocument, "license-agreement");
```

## Advanced Features

### Using ML.NET for Better Keyword Extraction

For more sophisticated keyword extraction, use the ML.NET-powered extractor:

```csharp
using var mlExtractor = new MLNetKeywordExtractor();
var chunker = new StructureChunker(strategy, mlExtractor);

var result = await chunker.ProcessAsync(document, "doc-id");
```

**Note**: The first use of ML.NET components may take 1-2 seconds for initialization.

### Custom Chunking Rules

You can define custom patterns for specific document types:

```csharp
var customRules = new List<ChunkingRule>
{
    // Custom header pattern
    new ChunkingRule("CustomHeader", @"^SECTION\s+(\d+):\s+(.*)", level: 1, priority: 0),
    
    // Custom note pattern  
    new ChunkingRule("Note", @"^NOTE:\s+(.*)", level: 3, priority: 20)
};

var customStrategy = new PatternBasedStrategy(customRules);
var chunker = new StructureChunker(customStrategy, extractor);
```

### Text Vectorization

For semantic analysis, you can use the ONNX vectorizer:

```csharp
using var vectorizer = OnnxVectorizerFactory.CreatePlaceholder();

foreach (var chunk in result.Chunks)
{
    var embedding = await vectorizer.VectorizeAsync(chunk.Content, isQuery: false);
    Console.WriteLine($"Generated {embedding.Length}-dimensional vector for: {chunk.CleanTitle}");
}
```

## Common Patterns and Use Cases

### Processing Multiple Documents

```csharp
var documents = new[]
{
    ("doc1.md", content1),
    ("doc2.md", content2),
    ("doc3.md", content3)
};

var tasks = documents.Select(async doc =>
{
    var result = await chunker.ProcessAsync(doc.Item2, doc.Item1);
    return (FileName: doc.Item1, Result: result);
});

var results = await Task.WhenAll(tasks);

foreach (var (fileName, result) in results)
{
    Console.WriteLine($"{fileName}: {result.Chunks.Count} chunks");
}
```

### Saving Results to JSON

```csharp
using System.Text.Json;

var result = await chunker.ProcessAsync(document, "doc-id");

var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
{ 
    WriteIndented = true 
});

await File.WriteAllTextAsync("output.json", json);
```

### Building a Document Hierarchy

```csharp
// Create a dictionary for quick parent lookup
var chunkDict = result.Chunks.ToDictionary(c => c.Id, c => c);

// Find root chunks (no parent)
var rootChunks = result.Chunks.Where(c => !c.ParentId.HasValue);

foreach (var root in rootChunks)
{
    PrintHierarchy(root, chunkDict, 0);
}

static void PrintHierarchy(ChunkNode chunk, Dictionary<Guid, ChunkNode> allChunks, int indent)
{
    Console.WriteLine($"{new string(' ', indent * 2)}- {chunk.CleanTitle}");
    
    var children = allChunks.Values.Where(c => c.ParentId == chunk.Id);
    foreach (var child in children)
    {
        PrintHierarchy(child, allChunks, indent + 1);
    }
}
```

## Error Handling Best Practices

Always wrap document processing in try-catch blocks:

```csharp
try
{
    var result = await chunker.ProcessAsync(document, sourceId);
    // Process successful result
}
catch (ArgumentException ex)
{
    // Handle invalid input parameters
    Console.WriteLine($"Invalid input: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // Handle processing errors
    Console.WriteLine($"Processing failed: {ex.Message}");
}
catch (Exception ex)
{
    // Handle unexpected errors
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Performance Tips

1. **Reuse Components**: Create chunker instances once and reuse them
2. **Use Singletons**: In web applications, register components as singletons
3. **Dispose Resources**: Always dispose ML.NET and ONNX components
4. **Batch Processing**: Process multiple documents concurrently when possible

```csharp
// Good: Reuse chunker instance
var chunker = new StructureChunker(strategy, extractor);
foreach (var doc in documents)
{
    var result = await chunker.ProcessAsync(doc.Content, doc.Id);
}

// Better: Process concurrently
var tasks = documents.Select(doc => chunker.ProcessAsync(doc.Content, doc.Id));
var results = await Task.WhenAll(tasks);
```

## Next Steps

Now that you have the basics working, explore these advanced topics:

1. **[API Reference](API-Reference.md)** - Detailed documentation of all classes and methods
2. **[Custom Patterns](Custom-Patterns.md)** - Creating custom chunking rules for specific document types
3. **[Integration Guide](Integration-Guide.md)** - Integrating with web applications and services
4. **[Performance Optimization](Performance-Guide.md)** - Tips for high-performance document processing

## Getting Help

If you encounter issues or have questions:

1. Check the [API Reference](API-Reference.md) for detailed documentation
2. Look at the [examples](../examples/) directory for more code samples
3. Open an issue on the [GitHub repository](https://github.com/DevelApp-ai/MarkdownStructureChunker)
4. Review the test cases for usage patterns

## Contributing

We welcome contributions! See the main [README](../README.md) for contribution guidelines.

