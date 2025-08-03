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

        // Create the chunking strategy with default rules
        var chunkingRules = PatternBasedStrategy.CreateDefaultRules();
        var chunkingStrategy = new PatternBasedStrategy(chunkingRules);
        
        // Create an ML.NET keyword extractor
        var keywordExtractor = new MLNetKeywordExtractor();
        
        // Create the main chunker
        var chunker = new StructureChunker(chunkingStrategy, keywordExtractor);

        // Test document with various heading patterns
        var testDocument = @"# Introduction

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

§ 42 Legal Requirements

This section outlines the legal requirements that must be followed.

Appendix A: Additional Resources

This appendix contains supplementary materials and references.

A. Technical Specifications

Detailed technical specifications are provided here.

B. Implementation Guidelines

Guidelines for implementing the proposed solutions.";

        try
        {
            // Process the document
            Console.WriteLine("Processing test document...");
            var result = await chunker.ProcessAsync(testDocument, "test-doc-001");
            
            Console.WriteLine($"Document processed successfully!");
            Console.WriteLine($"Source ID: {result.SourceId}");
            Console.WriteLine($"Total chunks: {result.Chunks.Count}");
            Console.WriteLine();

            // Display the results
            Console.WriteLine("Document Structure:");
            Console.WriteLine("==================");
            
            foreach (var chunk in result.Chunks)
            {
                var indent = new string(' ', chunk.Level * 2);
                Console.WriteLine($"{indent}[{chunk.ChunkType}] Level {chunk.Level}: {chunk.CleanTitle}");
                Console.WriteLine($"{indent}  Raw: {chunk.RawTitle}");
                Console.WriteLine($"{indent}  Content Length: {chunk.Content.Length} characters");
                Console.WriteLine($"{indent}  Keywords: {string.Join(", ", chunk.Keywords.Take(5))}");
                if (chunk.ParentId.HasValue)
                {
                    var parent = result.Chunks.FirstOrDefault(c => c.Id == chunk.ParentId);
                    Console.WriteLine($"{indent}  Parent: {parent?.CleanTitle ?? "Unknown"}");
                }
                Console.WriteLine();
            }

            // Show hierarchical structure
            Console.WriteLine("Hierarchical Structure:");
            Console.WriteLine("======================");
            DisplayHierarchy(result.Chunks, null, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing document: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine();
        Console.WriteLine("Demo completed. Press any key to exit...");
        Console.ReadKey();
    }

    private static void DisplayHierarchy(IReadOnlyList<MarkdownStructureChunker.Core.Models.ChunkNode> chunks, Guid? parentId, int depth)
    {
        var children = chunks.Where(c => c.ParentId == parentId).OrderBy(c => c.Level);
        
        foreach (var child in children)
        {
            var indent = new string(' ', depth * 2);
            Console.WriteLine($"{indent}├─ [{child.ChunkType}] {child.CleanTitle}");
            DisplayHierarchy(chunks, child.Id, depth + 1);
        }
    }
}

