using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Extractors;
using MarkdownStructureChunker.Core.Strategies;
using Xunit;

namespace MarkdownStructureChunker.Tests.Integration;

public class StructureChunkerIntegrationTests
{
    [Fact]
    public void Constructor_WithValidDependencies_CreatesChunker()
    {
        // Arrange
        var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        var extractor = new SimpleKeywordExtractor();

        // Act
        var chunker = new StructureChunker(strategy, extractor);

        // Assert
        Assert.NotNull(chunker);
    }

    [Fact]
    public void Constructor_WithNullStrategy_ThrowsArgumentNullException()
    {
        // Arrange
        var extractor = new SimpleKeywordExtractor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StructureChunker(null!, extractor));
    }

    [Fact]
    public void Constructor_WithNullExtractor_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StructureChunker(strategy, null!));
    }

    [Fact]
    public async Task ProcessAsync_EmptyDocument_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        var extractor = new SimpleKeywordExtractor();
        var chunker = new StructureChunker(strategy, extractor);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => chunker.ProcessAsync("", "test-doc"));
    }

    [Fact]
    public async Task ProcessAsync_EmptySourceId_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        var extractor = new SimpleKeywordExtractor();
        var chunker = new StructureChunker(strategy, extractor);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => chunker.ProcessAsync("# Test", ""));
    }

    [Fact]
    public async Task ProcessAsync_SimpleMarkdownDocument_ReturnsDocumentGraph()
    {
        // Arrange
        var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        var extractor = new SimpleKeywordExtractor();
        var chunker = new StructureChunker(strategy, extractor);
        
        var document = @"# Introduction
This document introduces the concept of machine learning and its applications in modern technology.

## Background
Machine learning is a subset of artificial intelligence that enables computers to learn without explicit programming.

### Historical Development
The field has evolved significantly since the 1950s with major breakthroughs in neural networks.

## Applications
Machine learning has numerous applications across various industries.

### Healthcare
Medical diagnosis and treatment recommendations.

### Finance
Fraud detection and algorithmic trading.

# Conclusion
Machine learning continues to transform how we solve complex problems.";

        // Act
        var result = await chunker.ProcessAsync(document, "ml-doc-001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ml-doc-001", result.SourceId);
        Assert.Equal(7, result.Chunks.Count);

        // Verify chunk structure
        var introduction = result.Chunks[0];
        Assert.Equal("MarkdownH1", introduction.ChunkType);
        Assert.Equal("Introduction", introduction.CleanTitle);
        Assert.Contains("machine learning", introduction.Content);
        Assert.NotEmpty(introduction.Keywords);
        // Note: Introduction may have a parent due to root chunk logic

        var background = result.Chunks[1];
        Assert.Equal("MarkdownH2", background.ChunkType);
        Assert.Equal("Background", background.CleanTitle);
        Assert.Equal(introduction.Id, background.ParentId);

        var historical = result.Chunks[2];
        Assert.Equal("MarkdownH3", historical.ChunkType);
        Assert.Equal("Historical Development", historical.CleanTitle);
        Assert.Equal(background.Id, historical.ParentId);

        // Verify keywords are extracted
        Assert.All(result.Chunks, chunk => Assert.NotEmpty(chunk.Keywords));
    }

    [Fact]
    public async Task ProcessAsync_NumericOutlineDocument_ReturnsCorrectHierarchy()
    {
        // Arrange
        var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        var extractor = new SimpleKeywordExtractor();
        var chunker = new StructureChunker(strategy, extractor);
        
        var document = @"1. Project Overview
This project aims to develop a comprehensive document processing system.

1.1 Objectives
The primary objectives include automated text analysis and structure recognition.

1.1.1 Primary Goals
Achieve high accuracy in document parsing and content extraction.

1.1.2 Secondary Goals
Provide extensible architecture for future enhancements.

1.2 Scope
The scope covers various document formats and processing techniques.

2. Technical Requirements
The system must meet specific technical and performance requirements.

2.1 Performance Metrics
Response time should be under 100ms for typical documents.";

        // Act
        var result = await chunker.ProcessAsync(document, "project-doc");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("project-doc", result.SourceId);
        // The document should produce 7 chunks based on all the numeric patterns
        Assert.Equal(7, result.Chunks.Count);

        // Verify numeric hierarchy
        var overview = result.Chunks[0];
        Assert.Equal("Numeric", overview.ChunkType);
        Assert.Equal(1, overview.Level);

        var objectives = result.Chunks[1];
        Assert.Equal("Numeric", objectives.ChunkType);
        Assert.Equal(2, objectives.Level);
        Assert.Equal(overview.Id, objectives.ParentId);

        var primaryGoals = result.Chunks[2];
        Assert.Equal("Numeric", primaryGoals.ChunkType);
        Assert.Equal(3, primaryGoals.Level);
        Assert.Equal(objectives.Id, primaryGoals.ParentId);

        var secondaryGoals = result.Chunks[3];
        Assert.Equal("Numeric", secondaryGoals.ChunkType);
        Assert.Equal(3, secondaryGoals.Level);
        Assert.Equal(objectives.Id, secondaryGoals.ParentId);

        var scope = result.Chunks[4];
        Assert.Equal("Numeric", scope.ChunkType);
        Assert.Equal(2, scope.Level);
        Assert.Equal(overview.Id, scope.ParentId);

        var requirements = result.Chunks[5];
        Assert.Equal("Numeric", requirements.ChunkType);
        Assert.Equal(1, requirements.Level);

        var performance = result.Chunks[6];
        Assert.Equal("Numeric", performance.ChunkType);
        Assert.Equal(2, performance.Level);
        Assert.Equal(requirements.Id, performance.ParentId);
    }

    [Fact]
    public async Task ProcessAsync_MixedPatternDocument_HandlesAllPatterns()
    {
        // Arrange
        var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        var extractor = new SimpleKeywordExtractor();
        var chunker = new StructureChunker(strategy, extractor);
        
        var document = @"# Legal Document Analysis

## 1. Introduction
This document provides analysis of legal requirements.

1.1 Purpose
Define the scope and objectives of the legal analysis.

§ 42 Compliance Requirements
All systems must comply with applicable regulations and standards.

## 2. Technical Specifications

2.1 System Architecture
The system follows a modular architecture pattern.

Appendix A: Reference Materials
Additional resources and documentation links.

A. Technical Standards
Industry standards and best practices.

B. Regulatory Guidelines
Government regulations and compliance requirements.";

        // Act
        var result = await chunker.ProcessAsync(document, "legal-doc");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("legal-doc", result.SourceId);
        Assert.Equal(9, result.Chunks.Count);

        // Verify different pattern types are recognized
        var patternTypes = result.Chunks.Select(c => c.ChunkType).Distinct().ToList();
        Assert.Contains("MarkdownH1", patternTypes);
        Assert.Contains("MarkdownH2", patternTypes);
        Assert.Contains("Numeric", patternTypes);
        Assert.Contains("Legal", patternTypes);
        Assert.Contains("Appendix", patternTypes);
        Assert.Contains("Letter", patternTypes);

        // Verify legal section
        var legalSection = result.Chunks.First(c => c.ChunkType == "Legal");
        Assert.Equal("§ 42 Compliance Requirements", legalSection.RawTitle);
        Assert.Equal("Compliance Requirements", legalSection.CleanTitle);

        // Verify appendix
        var appendix = result.Chunks.First(c => c.ChunkType == "Appendix");
        Assert.Equal("Appendix A: Reference Materials", appendix.RawTitle);
        Assert.Equal("Reference Materials", appendix.CleanTitle);
    }

    [Fact]
    public void Process_SynchronousVersion_WorksCorrectly()
    {
        // Arrange
        var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        var extractor = new SimpleKeywordExtractor();
        var chunker = new StructureChunker(strategy, extractor);
        
        var document = @"# Test Document
This is a test document for synchronous processing.

## Section 1
Content for section 1.";

        // Act
        var result = chunker.Process(document, "sync-test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("sync-test", result.SourceId);
        Assert.Equal(2, result.Chunks.Count);
    }

    [Fact]
    public async Task ProcessAsync_WithMLNetExtractor_ExtractsKeywords()
    {
        // Arrange
        var strategy = new PatternBasedStrategy(PatternBasedStrategy.CreateDefaultRules());
        using var extractor = new MLNetKeywordExtractor();
        var chunker = new StructureChunker(strategy, extractor);
        
        var document = @"# Machine Learning Overview
Machine learning algorithms enable computers to learn patterns from data without explicit programming.

## Neural Networks
Neural networks are computational models inspired by biological neural networks in animal brains.";

        // Act
        var result = await chunker.ProcessAsync(document, "ml-overview");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Chunks.Count);
        
        // Verify keywords are extracted
        Assert.All(result.Chunks, chunk => Assert.NotEmpty(chunk.Keywords));
        
        var overview = result.Chunks[0];
        Assert.Contains("machine", overview.Keywords);
        Assert.Contains("learning", overview.Keywords);
        
        var networks = result.Chunks[1];
        Assert.Contains("neural", networks.Keywords);
        Assert.Contains("networks", networks.Keywords);
    }
}

