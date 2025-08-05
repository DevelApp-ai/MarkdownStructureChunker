using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Extractors;
using MarkdownStructureChunker.Core.Strategies;
using MarkdownStructureChunker.Core.Vectorizers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MarkdownStructureChunker.Examples
{
    /// <summary>
    /// Basic usage example demonstrating core functionality of the MarkdownStructureChunker library.
    /// </summary>
    public class BasicUsageExample
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("MarkdownStructureChunker - Basic Usage Example");
            Console.WriteLine("==============================================\n");

            // Example 1: Simple document processing
            await SimpleDocumentProcessing();

            // Example 2: Advanced processing with ML.NET
            await AdvancedProcessingWithMLNet();

            // Example 3: Custom chunking rules
            await CustomChunkingRules();

            // Example 4: Vectorization example
            await VectorizationExample();

            Console.WriteLine("\nAll examples completed successfully!");
        }

        /// <summary>
        /// Demonstrates basic document processing with simple keyword extraction.
        /// </summary>
        private static async Task SimpleDocumentProcessing()
        {
            Console.WriteLine("Example 1: Simple Document Processing");
            Console.WriteLine("------------------------------------");

            // Create chunking strategy with default rules
            var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
            
            // Create simple keyword extractor
            var extractor = new SimpleKeywordExtractor();
            
            // Initialize the chunker
            var chunker = new StructureChunker(strategy, extractor);

            // Sample document
            var document = @"
# Machine Learning Guide

This guide provides an introduction to machine learning concepts and applications.

## Supervised Learning

Supervised learning algorithms learn from labeled training data to make predictions on new, unseen data.

### Classification

Classification algorithms predict discrete categories or classes for input data points.

### Regression

Regression algorithms predict continuous numerical values based on input features.

## Unsupervised Learning

Unsupervised learning finds patterns in data without labeled examples.

### Clustering

Clustering algorithms group similar data points together based on their characteristics.

### Dimensionality Reduction

Dimensionality reduction techniques reduce the number of features while preserving important information.
";

            try
            {
                // Process the document
                var result = await chunker.ProcessAsync(document, "ml-guide-001");

                Console.WriteLine($"Document processed successfully!");
                Console.WriteLine($"Source ID: {result.SourceId}");
                Console.WriteLine($"Total chunks: {result.Chunks.Count}\n");

                // Display chunk information
                foreach (var chunk in result.Chunks)
                {
                    Console.WriteLine($"Chunk: {chunk.CleanTitle} (Level {chunk.Level})");
                    Console.WriteLine($"Type: {chunk.ChunkType}");
                    Console.WriteLine($"Keywords: {string.Join(", ", chunk.Keywords)}");
                    Console.WriteLine($"Content preview: {chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length))}...");
                    Console.WriteLine($"Parent ID: {chunk.ParentId?.ToString() ?? "None"}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing document: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates advanced processing using ML.NET for keyword extraction.
        /// </summary>
        private static async Task AdvancedProcessingWithMLNet()
        {
            Console.WriteLine("Example 2: Advanced Processing with ML.NET");
            Console.WriteLine("-----------------------------------------");

            var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
            
            // Use ML.NET keyword extractor for more sophisticated analysis
            using var mlExtractor = new MLNetKeywordExtractor();
            var chunker = new StructureChunker(strategy, mlExtractor);

            var technicalDocument = @"
# Neural Network Architecture

Neural networks are computational models inspired by biological neural networks.

## Feedforward Networks

Feedforward neural networks process information in one direction from input to output layers.

### Multilayer Perceptrons

Multilayer perceptrons consist of multiple layers of interconnected neurons with nonlinear activation functions.

## Recurrent Networks

Recurrent neural networks can process sequences by maintaining internal state through feedback connections.

### LSTM Networks

Long Short-Term Memory networks address the vanishing gradient problem in traditional RNNs.
";

            try
            {
                var result = await chunker.ProcessAsync(technicalDocument, "neural-networks-guide");

                Console.WriteLine($"Advanced processing completed!");
                Console.WriteLine($"Chunks processed: {result.Chunks.Count}\n");

                foreach (var chunk in result.Chunks)
                {
                    Console.WriteLine($"Section: {chunk.CleanTitle}");
                    Console.WriteLine($"ML.NET Keywords: {string.Join(", ", chunk.Keywords)}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in advanced processing: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates creating and using custom chunking rules.
        /// </summary>
        private static async Task CustomChunkingRules()
        {
            Console.WriteLine("Example 3: Custom Chunking Rules");
            Console.WriteLine("--------------------------------");

            // Create custom rules for specific document patterns
            var customRules = new List<ChunkingRule>
            {
                // Custom header pattern
                new ChunkingRule("CustomHeader", @"^SECTION\s+(\d+):\s+(.*)", level: 1, priority: 0),
                
                // Custom subsection pattern
                new ChunkingRule("CustomSubsection", @"^(\d+\.\d+)\s+(.*)", priority: 10),
                
                // Custom note pattern
                new ChunkingRule("Note", @"^NOTE:\s+(.*)", level: 3, priority: 20)
            };

            var customStrategy = new PatternBasedStrategy(customRules);
            var extractor = new SimpleKeywordExtractor();
            var chunker = new StructureChunker(customStrategy, extractor);

            var customDocument = @"
SECTION 1: System Requirements

This section outlines the fundamental system requirements for deployment.

1.1 Hardware Requirements

The system requires minimum 8GB RAM and 4 CPU cores for optimal performance.

NOTE: These requirements may vary based on expected load and usage patterns.

1.2 Software Dependencies

The application depends on .NET 8.0 runtime and SQL Server 2019 or later.

SECTION 2: Installation Procedures

This section provides step-by-step installation instructions.

2.1 Pre-installation Checklist

Verify all system requirements are met before beginning installation process.
";

            try
            {
                var result = await chunker.ProcessAsync(customDocument, "custom-doc-001");

                Console.WriteLine($"Custom rules processing completed!");
                Console.WriteLine($"Detected patterns: {result.Chunks.Count}\n");

                foreach (var chunk in result.Chunks)
                {
                    Console.WriteLine($"Pattern: {chunk.ChunkType} - {chunk.CleanTitle}");
                    Console.WriteLine($"Level: {chunk.Level}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with custom rules: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates vectorization capabilities using ONNX models.
        /// </summary>
        private static async Task VectorizationExample()
        {
            Console.WriteLine("Example 4: Vectorization with ONNX");
            Console.WriteLine("----------------------------------");

            // Create vectorizer (will use placeholder implementation if model not available)
            using var vectorizer = OnnxVectorizerFactory.CreatePlaceholder();

            var sampleTexts = new[]
            {
                "Machine learning algorithms process data to identify patterns.",
                "Neural networks are inspired by biological brain structures.",
                "Deep learning uses multiple layers for feature extraction."
            };

            try
            {
                Console.WriteLine("Generating embeddings for sample texts...\n");

                foreach (var text in sampleTexts)
                {
                    // Generate embedding for passage
                    var embedding = await vectorizer.VectorizeAsync(text, isQuery: false);
                    
                    Console.WriteLine($"Text: {text}");
                    Console.WriteLine($"Embedding dimension: {embedding.Length}");
                    Console.WriteLine($"First 5 values: [{string.Join(", ", embedding.Take(5).Select(x => x.ToString("F4")))}]");
                    Console.WriteLine();
                }

                // Demonstrate context enrichment
                var ancestralTitles = new[] { "Machine Learning", "Neural Networks", "Deep Learning" };
                var enrichedContent = OnnxVectorizer.EnrichContentWithContext(
                    "Convolutional layers extract spatial features from input data.",
                    ancestralTitles
                );

                Console.WriteLine("Context enrichment example:");
                Console.WriteLine($"Original: Convolutional layers extract spatial features from input data.");
                Console.WriteLine($"Enriched: {enrichedContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in vectorization: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Example of processing documents from files.
    /// </summary>
    public class FileProcessingExample
    {
        public static async Task ProcessDocumentFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

            try
            {
                // Read document content
                var content = await File.ReadAllTextAsync(filePath);
                
                // Setup chunker
                var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
                var extractor = new SimpleKeywordExtractor();
                var chunker = new StructureChunker(strategy, extractor);

                // Process document
                var documentId = Path.GetFileNameWithoutExtension(filePath);
                var result = await chunker.ProcessAsync(content, documentId);

                // Save results to JSON
                var outputPath = Path.ChangeExtension(filePath, ".chunks.json");
                var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await File.WriteAllTextAsync(outputPath, json);
                
                Console.WriteLine($"Processed {filePath}");
                Console.WriteLine($"Generated {result.Chunks.Count} chunks");
                Console.WriteLine($"Results saved to {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }
    }
}

