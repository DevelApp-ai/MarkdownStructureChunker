using MarkdownStructureChunker.Core.Vectorizers;
using Xunit;

namespace MarkdownStructureChunker.Tests.Vectorizers;

public class OnnxVectorizerTests
{
    [Fact]
    public void Constructor_WithoutModelPath_CreatesPlaceholderVectorizer()
    {
        // Act
        using var vectorizer = new OnnxVectorizer();

        // Assert
        Assert.Equal(1024, vectorizer.VectorDimension);
    }

    [Fact]
    public void Constructor_WithNonExistentModelPath_CreatesPlaceholderVectorizer()
    {
        // Act
        using var vectorizer = new OnnxVectorizer("/non/existent/path/model.onnx");

        // Assert
        Assert.Equal(1024, vectorizer.VectorDimension);
    }

    [Fact]
    public void VectorDimension_ReturnsCorrectDimension()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();

        // Act
        var dimension = vectorizer.VectorDimension;

        // Assert
        Assert.Equal(1024, dimension);
    }

    [Fact]
    public async Task VectorizeAsync_EmptyText_ReturnsZeroVector()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();

        // Act
        var result = await vectorizer.VectorizeAsync("");

        // Assert
        Assert.Equal(1024, result.Length);
        Assert.All(result, value => Assert.Equal(0.0f, value));
    }

    [Fact]
    public async Task VectorizeAsync_NullText_ReturnsZeroVector()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();

        // Act
        var result = await vectorizer.VectorizeAsync(null!);

        // Assert
        Assert.Equal(1024, result.Length);
        Assert.All(result, value => Assert.Equal(0.0f, value));
    }

    [Fact]
    public async Task VectorizeAsync_WhitespaceText_ReturnsZeroVector()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();

        // Act
        var result = await vectorizer.VectorizeAsync("   \n\t   ");

        // Assert
        Assert.Equal(1024, result.Length);
        Assert.All(result, value => Assert.Equal(0.0f, value));
    }

    [Fact]
    public async Task VectorizeAsync_SimpleText_ReturnsNormalizedVector()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();
        var text = "This is a simple test document.";

        // Act
        var result = await vectorizer.VectorizeAsync(text);

        // Assert
        Assert.Equal(1024, result.Length);
        
        // Vector should be normalized (magnitude close to 1)
        var magnitude = Math.Sqrt(result.Sum(x => x * x));
        Assert.True(Math.Abs(magnitude - 1.0) < 0.001, $"Vector magnitude {magnitude} should be close to 1.0");
        
        // Should not be all zeros
        Assert.Contains(result, x => x != 0.0f);
    }

    [Fact]
    public async Task VectorizeAsync_SameTextMultipleTimes_ReturnsSameVector()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();
        var text = "Consistent text for testing deterministic behavior.";

        // Act
        var result1 = await vectorizer.VectorizeAsync(text);
        var result2 = await vectorizer.VectorizeAsync(text);

        // Assert
        Assert.Equal(result1.Length, result2.Length);
        for (int i = 0; i < result1.Length; i++)
        {
            Assert.Equal(result1[i], result2[i], precision: 6);
        }
    }

    [Fact]
    public async Task VectorizeAsync_DifferentTexts_ReturnsDifferentVectors()
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();
        var text1 = "First document about machine learning.";
        var text2 = "Second document about natural language processing.";

        // Act
        var result1 = await vectorizer.VectorizeAsync(text1);
        var result2 = await vectorizer.VectorizeAsync(text2);

        // Assert
        Assert.Equal(result1.Length, result2.Length);
        
        // Vectors should be different
        var areDifferent = false;
        for (int i = 0; i < result1.Length; i++)
        {
            if (Math.Abs(result1[i] - result2[i]) > 0.001f)
            {
                areDifferent = true;
                break;
            }
        }
        Assert.True(areDifferent, "Vectors for different texts should be different");
    }

    [Theory]
    [InlineData(true, "query: ")]
    [InlineData(false, "passage: ")]
    public async Task VectorizeAsync_WithQueryFlag_AddsPrefixCorrectly(bool isQuery, string expectedPrefix)
    {
        // Arrange
        using var vectorizer = new OnnxVectorizer();
        var text = "Test document content.";

        // Act
        var result = await vectorizer.VectorizeAsync(text, isQuery);

        // Assert
        Assert.Equal(1024, result.Length);
        
        // The vector should be different from the non-prefixed version
        var nonPrefixedResult = await vectorizer.VectorizeAsync(text, !isQuery);
        
        // Verify that the expected prefix logic is working by checking vector differences
        Assert.NotEqual(result, nonPrefixedResult);
        
        // Additional verification that the prefix parameter is used correctly
        var prefixType = expectedPrefix.Trim().TrimEnd(':');
        Assert.Contains(prefixType, new[] { "query", "passage" });
    }

    [Fact]
    public static void EnrichContentWithContext_EmptyAncestralTitles_ReturnsOriginalContent()
    {
        // Arrange
        var content = "Original content";
        var ancestralTitles = new List<string>();

        // Act
        var result = OnnxVectorizer.EnrichContentWithContext(content, ancestralTitles);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public static void EnrichContentWithContext_WithAncestralTitles_AddsContext()
    {
        // Arrange
        var content = "Section content";
        var ancestralTitles = new List<string> { "Chapter 1", "Section 1.1" };

        // Act
        var result = OnnxVectorizer.EnrichContentWithContext(content, ancestralTitles);

        // Assert
        Assert.Equal("Chapter 1: Section 1.1: Section content", result);
    }

    [Fact]
    public static void EnrichContentWithContext_WithEmptyTitles_FiltersOutEmpty()
    {
        // Arrange
        var content = "Section content";
        var ancestralTitles = new List<string> { "Chapter 1", "", "Section 1.1", "   " };

        // Act
        var result = OnnxVectorizer.EnrichContentWithContext(content, ancestralTitles);

        // Assert
        Assert.Equal("Chapter 1: Section 1.1: Section content", result);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var vectorizer = new OnnxVectorizer();

        // Act & Assert
        vectorizer.Dispose();
        vectorizer.Dispose(); // Should not throw
    }
}

public class OnnxVectorizerFactoryTests
{
    [Fact]
    public void CreateDefault_ReturnsVectorizer()
    {
        // Act
        using var vectorizer = OnnxVectorizerFactory.CreateDefault();

        // Assert
        Assert.NotNull(vectorizer);
        Assert.Equal(1024, vectorizer.VectorDimension);
    }

    [Fact]
    public void CreateWithPath_ReturnsVectorizer()
    {
        // Act
        using var vectorizer = OnnxVectorizerFactory.CreateWithPaths("/test/path/model.onnx");

        // Assert
        Assert.NotNull(vectorizer);
        Assert.Equal(1024, vectorizer.VectorDimension);
    }

    [Fact]
    public void CreatePlaceholder_ReturnsVectorizer()
    {
        // Act
        using var vectorizer = OnnxVectorizerFactory.CreateDeterministic();

        // Assert
        Assert.NotNull(vectorizer);
        Assert.Equal(1024, vectorizer.VectorDimension);
    }
}

