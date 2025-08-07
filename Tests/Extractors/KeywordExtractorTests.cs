using MarkdownStructureChunker.Core.Extractors;
using Xunit;

namespace MarkdownStructureChunker.Tests.Extractors;

public class SimpleKeywordExtractorTests
{
    [Fact]
    public async Task ExtractKeywordsAsync_EmptyContent_ReturnsEmptyList()
    {
        // Arrange
        var extractor = new SimpleKeywordExtractor();

        // Act
        var result = await extractor.ExtractKeywordsAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractKeywordsAsync_NullContent_ReturnsEmptyList()
    {
        // Arrange
        var extractor = new SimpleKeywordExtractor();

        // Act
        var result = await extractor.ExtractKeywordsAsync(null!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractKeywordsAsync_WhitespaceContent_ReturnsEmptyList()
    {
        // Arrange
        var extractor = new SimpleKeywordExtractor();

        // Act
        var result = await extractor.ExtractKeywordsAsync("   \n\t   ");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractKeywordsAsync_SimpleText_ReturnsKeywords()
    {
        // Arrange
        var extractor = new SimpleKeywordExtractor();
        var content = "This is a simple test document with important keywords and concepts.";

        // Act
        var result = await extractor.ExtractKeywordsAsync(content, maxKeywords: 5);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Count <= 5);
        
        // Should not contain stop words
        Assert.DoesNotContain("this", result);
        Assert.DoesNotContain("is", result);
        Assert.DoesNotContain("a", result);
        Assert.DoesNotContain("with", result);
        Assert.DoesNotContain("and", result);
        
        // Should contain meaningful words (but exact words may vary due to processing)
        var meaningfulWords = new[] { "simple", "test", "document", "important", "keywords", "concepts" };
        Assert.True(result.Intersect(meaningfulWords).Count() >= 3, 
            $"Expected at least 3 meaningful words, got: {string.Join(", ", result)}");
    }

    [Fact]
    public async Task ExtractKeywordsAsync_RepeatedWords_RanksFrequencyCorrectly()
    {
        // Arrange
        var extractor = new SimpleKeywordExtractor();
        var content = "Machine learning is important. Machine learning algorithms are powerful. Machine learning transforms data.";

        // Act
        var result = await extractor.ExtractKeywordsAsync(content, maxKeywords: 5);

        // Assert
        Assert.NotEmpty(result);
        
        // "machine" and "learning" should be top keywords due to frequency
        Assert.Contains("machine", result);
        Assert.Contains("learning", result);
        
        // The first keyword should be one of the most frequent
        Assert.True(result[0] == "machine" || result[0] == "learning");
    }

    [Fact]
    public async Task ExtractKeywordsAsync_TechnicalContent_ExtractsRelevantTerms()
    {
        // Arrange
        var extractor = new SimpleKeywordExtractor();
        var content = @"The neural network architecture consists of multiple layers including 
                       convolutional layers, pooling layers, and fully connected layers. 
                       The model uses backpropagation for training and optimization.";

        // Act
        var result = await extractor.ExtractKeywordsAsync(content, maxKeywords: 8);

        // Assert
        Assert.NotEmpty(result);
        
        // Should extract technical terms (but exact terms may vary)
        var technicalTerms = new[] { "neural", "network", "layers", "model", "architecture", "backpropagation" };
        Assert.True(result.Intersect(technicalTerms).Count() >= 3,
            $"Expected at least 3 technical terms from {string.Join(", ", technicalTerms)}, got: {string.Join(", ", result)}");
        
        // Should not contain common words
        Assert.DoesNotContain("the", result);
        Assert.DoesNotContain("and", result);
        Assert.DoesNotContain("for", result);
    }

    [Fact]
    public async Task ExtractKeywordsAsync_MaxKeywordsLimit_RespectsLimit()
    {
        // Arrange
        var extractor = new SimpleKeywordExtractor();
        var content = "One two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen";

        // Act
        var result = await extractor.ExtractKeywordsAsync(content, maxKeywords: 3);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task ExtractKeywordsAsync_ShortWords_FiltersOut()
    {
        // Arrange
        var extractor = new SimpleKeywordExtractor();
        var content = "A big cat sat on a mat. It was a fat cat.";

        // Act
        var result = await extractor.ExtractKeywordsAsync(content, maxKeywords: 10);

        // Assert
        Assert.NotEmpty(result);
        
        // Should not contain words shorter than 3 characters
        Assert.DoesNotContain("a", result);
        Assert.DoesNotContain("on", result);
        Assert.DoesNotContain("it", result);
        
        // Should contain longer words (but exact set may vary)
        var expectedWords = new[] { "big", "cat", "sat", "mat", "was", "fat" };
        Assert.True(result.Intersect(expectedWords).Count() >= 4,
            $"Expected at least 4 words from {string.Join(", ", expectedWords)}, got: {string.Join(", ", result)}");
    }
}

public class MLNetKeywordExtractorTests
{
    [Fact]
    public async Task ExtractKeywordsAsync_EmptyContent_ReturnsEmptyList()
    {
        // Arrange
        using var extractor = new MLNetKeywordExtractor();

        // Act
        var result = await extractor.ExtractKeywordsAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractKeywordsAsync_SimpleText_ReturnsKeywords()
    {
        // Arrange
        using var extractor = new MLNetKeywordExtractor();
        var content = "Machine learning algorithms process data to identify patterns and make predictions.";

        // Act
        var result = await extractor.ExtractKeywordsAsync(content, maxKeywords: 5);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Count <= 5);
        
        // Should extract meaningful terms
        Assert.Contains("machine", result);
        Assert.Contains("learning", result);
        Assert.Contains("algorithms", result);
    }

    [Fact]
    public async Task ExtractKeywordsAsync_TechnicalDocument_ExtractsRelevantTerms()
    {
        // Arrange
        using var extractor = new MLNetKeywordExtractor();
        var content = @"The transformer architecture revolutionized natural language processing. 
                       Self-attention mechanisms enable the model to focus on relevant parts 
                       of the input sequence. Multi-head attention provides multiple 
                       representation subspaces for better understanding.";

        // Act
        var result = await extractor.ExtractKeywordsAsync(content, maxKeywords: 8);

        // Assert
        Assert.NotEmpty(result);
        
        // Should extract some technical terms (but not necessarily all specific ones)
        Assert.Contains(result, k => k.Contains("attention") || k.Contains("model") || k.Contains("architecture"));
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var extractor = new MLNetKeywordExtractor();

        // Act & Assert
        extractor.Dispose();
        extractor.Dispose(); // Should not throw
    }
}

