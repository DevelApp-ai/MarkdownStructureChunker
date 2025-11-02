using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Extractors;
using MarkdownStructureChunker.Core.Models;
using MarkdownStructureChunker.Core.Strategies;

namespace MarkdownStructureChunker.Examples;

/// <summary>
/// Example demonstrating the structure-first ingestion architecture using AST-based parsing.
/// This example shows how to use the new Markdig-based strategy to create structural graphs.
/// </summary>
public class StructureFirstExample
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("MarkdownStructureChunker - Structure-First Ingestion Architecture Example");
        Console.WriteLine("=========================================================================\n");

        // Example 1: Traditional chunking (backward compatibility)
        await TraditionalChunkingExample();

        // Example 2: Structure-first processing with AST
        await StructureFirstExample();

        // Example 3: Exploring the structural graph
        await StructuralGraphExample();

        Console.WriteLine("\nAll structure-first examples completed successfully!");
    }

    /// <summary>
    /// Demonstrates traditional chunking for comparison.
    /// </summary>
    private static async Task TraditionalChunkingExample()
    {
        Console.WriteLine("Example 1: Traditional Pattern-Based Chunking");
        Console.WriteLine("----------------------------------------------");

        var markdown = @"# Machine Learning Guide

This guide covers fundamental concepts of machine learning.

## Supervised Learning

Supervised learning uses labeled training data.

### Classification

Classification predicts discrete categories.

### Regression

Regression predicts continuous values.

## Unsupervised Learning

Unsupervised learning finds patterns in unlabeled data.

### Clustering

Clustering groups similar data points.

```python
from sklearn.cluster import KMeans
kmeans = KMeans(n_clusters=3)
kmeans.fit(data)
```";

        // Create traditional chunker
        var patternStrategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        var keywordExtractor = new SimpleKeywordExtractor();
        using var chunker = new StructureChunker(patternStrategy, keywordExtractor);

        var result = await chunker.ProcessAsync(markdown, "ml-guide-traditional");

        Console.WriteLine($"Total chunks: {result.Chunks.Count}");
        Console.WriteLine($"Has structural graph: {result.HasStructuralGraph}");
        Console.WriteLine();

        foreach (var chunk in result.Chunks.Take(3))
        {
            Console.WriteLine($"Chunk: {chunk.CleanTitle} (Level {chunk.Level})");
            Console.WriteLine($"Type: {chunk.ChunkType}");
            Console.WriteLine($"Content preview: {chunk.Content.Substring(0, Math.Min(80, chunk.Content.Length))}...");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Demonstrates structure-first processing with AST.
    /// </summary>
    private static async Task StructureFirstExample()
    {
        Console.WriteLine("Example 2: Structure-First AST-Based Processing");
        Console.WriteLine("-----------------------------------------------");

        var markdown = @"# Machine Learning Guide

This guide covers fundamental concepts of machine learning.

## Supervised Learning

Supervised learning uses labeled training data to make predictions.

### Classification

Classification algorithms predict discrete categories or classes.

#### Decision Trees

Decision trees create a model of decisions to reach a prediction.

#### Neural Networks

Neural networks mimic the human brain's interconnected structure.

### Regression

Regression algorithms predict continuous numerical values.

## Unsupervised Learning

Unsupervised learning finds hidden patterns in unlabeled data.

### Clustering

Clustering groups similar data points together.

```python
from sklearn.cluster import KMeans
import numpy as np

# Example clustering
data = np.random.rand(100, 2)
kmeans = KMeans(n_clusters=3)
clusters = kmeans.fit_predict(data)
```

### Dimensionality Reduction

Reduces the number of features while preserving important information.";

        // Create structure-first chunker
        using var chunker = StructureChunker.CreateStructureFirst();

        var result = await chunker.ProcessWithStructureAsync(markdown, "ml-guide-structured");

        Console.WriteLine($"Total traditional chunks: {result.Chunks.Count}");
        Console.WriteLine($"Has structural graph: {result.HasStructuralGraph}");
        Console.WriteLine($"Structural elements: {result.StructuralElements.Count}");
        Console.WriteLine($"Structural relationships: {result.StructuralEdges.Count}");
        Console.WriteLine();

        // Show structural elements by type
        var elementsByType = result.StructuralElements.GroupBy(e => e.ElementType);
        foreach (var group in elementsByType)
        {
            Console.WriteLine($"  {group.Key}: {group.Count()} elements");
        }
        Console.WriteLine();

        // Show heading hierarchy
        Console.WriteLine("Heading Structure:");
        var headings = result.StructuralElements
            .Where(e => e.ElementType == "heading")
            .OrderBy(e => e.StartOffset)
            .ToList();

        foreach (var heading in headings)
        {
            var indent = new string(' ', (heading.Level - 1) * 2);
            Console.WriteLine($"{indent}H{heading.Level}: {heading.Content}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates exploring and navigating the structural graph.
    /// </summary>
    private static async Task StructuralGraphExample()
    {
        Console.WriteLine("Example 3: Structural Graph Navigation");
        Console.WriteLine("--------------------------------------");

        var markdown = @"# Technical Documentation

## API Reference

### Authentication

All API calls require authentication.

#### OAuth 2.0

We support OAuth 2.0 for secure authentication.

##### Setup

Configure your OAuth application settings.

##### Usage

Use the OAuth flow to obtain access tokens.

#### API Keys

Alternative authentication using API keys.

### Endpoints

Available API endpoints and their usage.

## Examples

Code examples for common use cases.

```javascript
// Example API call
fetch('/api/users', {
  headers: {
    'Authorization': 'Bearer ' + token
  }
})
.then(response => response.json())
.then(data => console.log(data));
```";

        using var chunker = StructureChunker.CreateStructureFirst();
        var result = await chunker.ProcessWithStructureAsync(markdown, "tech-docs");

        Console.WriteLine("Exploring Structural Relationships:");
        Console.WriteLine();

        // Find root elements
        var rootElements = result.RootElements.ToList();
        Console.WriteLine($"Root elements: {rootElements.Count}");

        foreach (var root in rootElements)
        {
            Console.WriteLine($"  Root: {root.ElementType} - {root.Content.Substring(0, Math.Min(40, root.Content.Length))}...");
            ExploreChildren(result, root, 1);
        }

        Console.WriteLine();
        Console.WriteLine("Relationship Types:");
        var relationshipGroups = result.StructuralEdges.GroupBy(e => e.RelationshipType);
        foreach (var group in relationshipGroups)
        {
            Console.WriteLine($"  {group.Key}: {group.Count()} relationships");
        }

        Console.WriteLine();
        Console.WriteLine("Example: Finding content under 'Authentication' section:");
        var authElement = result.StructuralElements
            .FirstOrDefault(e => e.ElementType == "heading" && e.Content.Contains("Authentication"));

        if (authElement != null)
        {
            Console.WriteLine($"Found: {authElement.Content}");
            var children = result.GetChildElements(authElement.Id).ToList();
            Console.WriteLine($"  Has {children.Count} direct children:");
            
            foreach (var child in children)
            {
                Console.WriteLine($"    {child.ElementType}: {child.Content.Substring(0, Math.Min(30, child.Content.Length))}...");
            }
        }
    }

    /// <summary>
    /// Recursively explores and displays the children of a structural element.
    /// </summary>
    private static void ExploreChildren(DocumentGraph graph, StructuralElement element, int depth)
    {
        if (depth > 3) return; // Limit depth for readability

        var children = graph.GetChildElements(element.Id).ToList();
        foreach (var child in children)
        {
            var indent = new string(' ', depth * 4);
            var contentPreview = child.Content.Length > 30 
                ? child.Content.Substring(0, 30) + "..." 
                : child.Content;
            
            Console.WriteLine($"{indent}├─ {child.ElementType}: {contentPreview}");
            ExploreChildren(graph, child, depth + 1);
        }
    }
}