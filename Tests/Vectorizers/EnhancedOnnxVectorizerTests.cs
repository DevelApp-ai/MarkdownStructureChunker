using MarkdownStructureChunker.Core.Vectorizers;
using Xunit;

namespace MarkdownStructureChunker.Tests.Vectorizers;

public class EnhancedOnnxVectorizerTests
{
    [Fact]
    public async Task VectorizeBatchAsync_WithMultipleTexts_ReturnsCorrectNumberOfVectors()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();
        var texts = new[] { "First text", "Second text", "Third text" };

        // Act
        var results = await vectorizer.VectorizeBatchAsync(texts);

        // Assert
        Assert.Equal(3, results.Length);
        Assert.All(results, vector => Assert.Equal(1024, vector.Length));
    }

    [Fact]
    public async Task VectorizeBatchAsync_WithEmptyCollection_ReturnsEmptyArray()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();
        var texts = Array.Empty<string>();

        // Act
        var results = await vectorizer.VectorizeBatchAsync(texts);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task VectorizeBatchAsync_WithQueryFlag_ProcessesAllAsQueries()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();
        var texts = new[] { "What is AI?", "How does ML work?" };

        // Act
        var queryResults = await vectorizer.VectorizeBatchAsync(texts, isQuery: true);
        var passageResults = await vectorizer.VectorizeBatchAsync(texts, isQuery: false);

        // Assert
        Assert.Equal(2, queryResults.Length);
        Assert.Equal(2, passageResults.Length);
        
        // Query and passage vectors should be different due to prefixes
        for (int i = 0; i < texts.Length; i++)
        {
            Assert.False(queryResults[i].SequenceEqual(passageResults[i]));
        }
    }

    [Fact]
    public async Task VectorizeAsync_WithEnhancedDeterministicFallback_ProducesConsistentResults()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer(); // No model path, uses enhanced fallback
        var text = "This is a test text for enhanced deterministic vectorization";

        // Act
        var vector1 = await vectorizer.VectorizeAsync(text);
        var vector2 = await vectorizer.VectorizeAsync(text);

        // Assert
        Assert.Equal(vector1, vector2);
        Assert.Equal(1024, vector1.Length);
        
        // Vector should be normalized
        var magnitude = Math.Sqrt(vector1.Sum(x => x * x));
        Assert.True(Math.Abs(magnitude - 1.0) < 0.001, "Vector should be normalized");
    }

    [Fact]
    public void OnnxVectorizerFactory_CreateDefault_ReturnsValidVectorizer()
    {
        // Act
        using var vectorizer = OnnxVectorizerFactory.CreateDefault();

        // Assert
        Assert.NotNull(vectorizer);
        Assert.Equal(1024, vectorizer.VectorDimension);
    }

    [Fact]
    public void OnnxVectorizerFactory_CreateDeterministic_ReturnsValidVectorizer()
    {
        // Act
        using var vectorizer = OnnxVectorizerFactory.CreateDeterministic();

        // Assert
        Assert.NotNull(vectorizer);
        Assert.Equal(1024, vectorizer.VectorDimension);
    }

    [Fact]
    public async Task VectorizeAsync_WithSpecialCharacters_HandlesGracefully()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();
        var textWithSpecialChars = "Text with émojis 🚀, symbols @#$%, and unicode characters: αβγδε";

        // Act
        var vector = await vectorizer.VectorizeAsync(textWithSpecialChars);

        // Assert
        Assert.Equal(1024, vector.Length);
        Assert.True(vector.Any(x => Math.Abs(x) > 0), "Vector should have non-zero components");
        
        // Vector should be normalized
        var magnitude = Math.Sqrt(vector.Sum(x => x * x));
        Assert.True(Math.Abs(magnitude - 1.0) < 0.001, "Vector should be normalized");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t\r")]
    public async Task VectorizeAsync_WithEmptyOrWhitespaceText_ReturnsZeroVector(string text)
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();

        // Act
        var vector = await vectorizer.VectorizeAsync(text);

        // Assert
        Assert.Equal(1024, vector.Length);
        Assert.All(vector, component => Assert.Equal(0f, component));
    }

    [Fact]
    public async Task VectorizeBatchAsync_WithMixedContent_ProcessesAllCorrectly()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();
        var texts = new[]
        {
            "Short text",
            "Medium length text with more words and content",
            "Very long text that contains many words and should test the tokenizer's ability to handle longer sequences",
            "",
            "Text with numbers 123 and symbols !@#$%"
        };

        // Act
        var results = await vectorizer.VectorizeBatchAsync(texts);

        // Assert
        Assert.Equal(5, results.Length);
        Assert.All(results, vector => Assert.Equal(1024, vector.Length));
        
        // Empty text should produce zero vector
        Assert.All(results[3], component => Assert.Equal(0f, component));
        
        // Other vectors should have non-zero components
        for (int i = 0; i < results.Length; i++)
        {
            if (i != 3) // Skip empty text
            {
                Assert.True(results[i].Any(x => Math.Abs(x) > 0), $"Vector {i} should have non-zero components");
            }
        }
    }

    [Fact]
    public void Constructor_WithInvalidModelPath_HandlesGracefully()
    {
        // Arrange & Act
        using var vectorizer = new OnnxVectorizer("/invalid/path/to/model.onnx");

        // Assert
        Assert.NotNull(vectorizer);
        Assert.Equal(1024, vectorizer.VectorDimension);
    }
}

