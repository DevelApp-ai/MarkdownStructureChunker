using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Extractors;
using MarkdownStructureChunker.Core.Strategies;

namespace MarkdownStructureChunker.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("MarkdownStructureChunker Demo");
        Console.WriteLine("============================");
        Console.WriteLine();

        Console.WriteLine("Choose demo mode:");
        Console.WriteLine("1. Traditional Pattern-Based Chunking");
        Console.WriteLine("2. Structure-First AST-Based Processing");
        Console.WriteLine("3. Both (for comparison)");
        Console.Write("Enter choice (1-3): ");
        
        var choice = Console.ReadLine();
        
        switch (choice)
        {
            case "1":
                await RunTraditionalDemo();
                break;
            case "2":
                await RunStructureFirstDemo();
                break;
            case "3":
                await RunComparisonDemo();
                break;
            default:
                Console.WriteLine("Invalid choice. Running comparison demo...");
                await RunComparisonDemo();
                break;
        }

        Console.WriteLine();
        Console.WriteLine("Demo completed. Press any key to exit...");
        Console.ReadKey();
    }

    static async Task RunTraditionalDemo()
    {
        Console.WriteLine("\n=== Traditional Pattern-Based Chunking ===");
        
        // Create the chunking strategy with default rules
        var chunkingRules = PatternBasedStrategy.CreateDefaultRules();
        var chunkingStrategy = new PatternBasedStrategy(chunkingRules);
        
        // Create an ML.NET keyword extractor
        var keywordExtractor = new MLNetKeywordExtractor();
        
        // Create the main chunker
        var chunker = new StructureChunker(chunkingStrategy, keywordExtractor);

        await ProcessTestDocument(chunker, "traditional-chunking");
    }

    static async Task RunStructureFirstDemo()
    {
        Console.WriteLine("\n=== Structure-First AST-Based Processing ===");
        
        // Create structure-first chunker
        using var chunker = StructureChunker.CreateStructureFirst();
        
        await ProcessTestDocumentWithStructure(chunker);
    }

    static async Task RunComparisonDemo()
    {
        Console.WriteLine("\n=== Comparison: Traditional vs Structure-First ===");
        
        // Traditional
        Console.WriteLine("\n1. Traditional Pattern-Based Approach:");
        var patternStrategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        var keywordExtractor = new MLNetKeywordExtractor();
        var traditionalChunker = new StructureChunker(patternStrategy, keywordExtractor);
        await ProcessTestDocument(traditionalChunker, "traditional");
        
        Console.WriteLine("\n" + new string('=', 60));
        
        // Structure-First
        Console.WriteLine("\n2. Structure-First AST-Based Approach:");
        using var structureChunker = StructureChunker.CreateStructureFirst();
        await ProcessTestDocumentWithStructure(structureChunker);
    }

    static async Task ProcessTestDocument(StructureChunker chunker, string sourceId)
    {
        var testDocument = GetTestDocument();

        try
        {
            // Process the document
            Console.WriteLine("Processing test document...");
            var result = await chunker.ProcessAsync(testDocument, sourceId);
            
            Console.WriteLine($"Document processed successfully!");
            Console.WriteLine($"Source ID: {result.SourceId}");
            Console.WriteLine($"Total chunks: {result.Chunks.Count}");
            Console.WriteLine($"Has structural graph: {result.HasStructuralGraph}");
            Console.WriteLine();

            // Display the results (first 5 chunks)
            Console.WriteLine("Document Structure (first 5 chunks):");
            Console.WriteLine("====================================");
            
            foreach (var chunk in result.Chunks.Take(5))
            {
                var indent = new string(' ', (chunk.Level - 1) * 2);
                Console.WriteLine($"{indent}[{chunk.ChunkType}] {chunk.CleanTitle}");
                Console.WriteLine($"{indent}  Keywords: {string.Join(", ", chunk.Keywords.Take(3))}");
                Console.WriteLine($"{indent}  Content: {chunk.Content.Substring(0, Math.Min(60, chunk.Content.Length))}...");
                Console.WriteLine();
            }

            // Display hierarchy
            Console.WriteLine("Hierarchical Structure:");
            Console.WriteLine("======================");
            DisplayHierarchy(result.Chunks, null, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing document: {ex.Message}");
        }
    }

    static async Task ProcessTestDocumentWithStructure(StructureChunker chunker)
    {
        var testDocument = GetTestDocument();

        try
        {
            // Process with structure-first approach
            Console.WriteLine("Processing test document with structure-first approach...");
            var result = await chunker.ProcessWithStructureAsync(testDocument, "structure-first-demo");
            
            Console.WriteLine($"Document processed successfully!");
            Console.WriteLine($"Source ID: {result.SourceId}");
            Console.WriteLine($"Traditional chunks: {result.Chunks.Count}");
            Console.WriteLine($"Has structural graph: {result.HasStructuralGraph}");
            
            if (result.HasStructuralGraph)
            {
                Console.WriteLine($"Structural elements: {result.StructuralElements.Count}");
                Console.WriteLine($"Structural relationships: {result.StructuralEdges.Count}");
                
                // Show element types
                var elementsByType = result.StructuralElements.GroupBy(e => e.ElementType);
                Console.WriteLine("\nStructural Elements by Type:");
                foreach (var group in elementsByType)
                {
                    Console.WriteLine($"  {group.Key}: {group.Count()}");
                }
                
                // Show relationship types
                var relationshipsByType = result.StructuralEdges.GroupBy(e => e.RelationshipType);
                Console.WriteLine("\nRelationships by Type:");
                foreach (var group in relationshipsByType)
                {
                    Console.WriteLine($"  {group.Key}: {group.Count()}");
                }
                
                Console.WriteLine("\nStructural Hierarchy:");
                Console.WriteLine("====================");
                DisplayStructuralHierarchy(result);
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing document: {ex.Message}");
        }
    }

    static void DisplayStructuralHierarchy(MarkdownStructureChunker.Core.Models.DocumentGraph result)
    {
        var rootElements = result.RootElements.Where(e => e.ElementType == "heading").ToList();
        
        foreach (var root in rootElements)
        {
            DisplayStructuralElement(result, root, 0);
        }
    }

    static void DisplayStructuralElement(MarkdownStructureChunker.Core.Models.DocumentGraph graph, MarkdownStructureChunker.Core.Models.StructuralElement element, int depth)
    {
        var indent = new string(' ', depth * 2);
        var contentPreview = element.Content.Length > 50 
            ? element.Content.Substring(0, 50) + "..." 
            : element.Content;
            
        Console.WriteLine($"{indent}├─ [{element.ElementType}] {contentPreview}");
        
        var children = graph.GetChildElements(element.Id)
            .Where(e => e.ElementType == "heading") // Only show heading children for clarity
            .ToList();
            
        foreach (var child in children)
        {
            DisplayStructuralElement(graph, child, depth + 1);
        }
    }

    private static void DisplayHierarchy(IReadOnlyList<MarkdownStructureChunker.Core.Models.ChunkNode> chunks, Guid? parentId, int depth)
    {
        var children = chunks.Where(c => c.ParentId == parentId).OrderBy(c => c.Level).Take(5); // Limit for readability
        
        foreach (var child in children)
        {
            var indent = new string(' ', depth * 2);
            Console.WriteLine($"{indent}├─ [{child.ChunkType}] {child.CleanTitle}");
            DisplayHierarchy(chunks, child.Id, depth + 1);
        }
    }

    private static string GetTestDocument()
    {
        return @"# Introduction

This is the introduction section of our document. It contains important background information about the topic we're discussing.

## Background

The background provides context for understanding the main concepts.

### Historical Context

This subsection covers the historical development of the field.

## Methodology

Our approach involves several key steps and considerations.

1. Data Collection

We collected data from multiple sources to ensure comprehensive coverage.

1.1 Primary Sources

Primary sources included direct observations and measurements.

1.2 Secondary Sources

Secondary sources provided additional context and validation.

2. Analysis Framework

The analysis framework consists of several components.

2.1 Statistical Methods

We employed various statistical techniques for data analysis.

```python
import pandas as pd
import numpy as np

# Example data analysis
data = pd.read_csv('dataset.csv')
correlation = data.corr()
print(correlation)
```

§ 42 Legal Requirements

This section outlines the legal requirements that must be followed.

Appendix A: Additional Resources

This appendix contains supplementary materials and references.

A. Technical Specifications

Detailed technical specifications are provided here.

B. Implementation Guidelines

Guidelines for implementing the proposed solutions.";
    }
}

