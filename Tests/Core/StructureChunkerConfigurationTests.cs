using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Configuration;
using MarkdownStructureChunker.Core.Models;
using Xunit;

namespace MarkdownStructureChunker.Tests.Core;

public class StructureChunkerConfigurationTests
{
    [Fact]
    public void Constructor_WithConfiguration_CreatesChunker()
    {
        // Arrange
        var config = ChunkerConfiguration.CreateDefault();

        // Act
        using var chunker = new StructureChunker(config);

        // Assert
        Assert.NotNull(chunker);
        Assert.Equal(config, chunker.Configuration);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StructureChunker((ChunkerConfiguration)null!));
    }

    [Fact]
    public void Constructor_WithInvalidConfiguration_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            MaxChunkSize = 0 // Invalid
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StructureChunker(config));
    }

    [Fact]
    public async Task ChunkAsync_WithSimpleContent_ReturnsChunks()
    {
        // Arrange
        var config = ChunkerConfiguration.CreateDefault();
        using var chunker = new StructureChunker(config);
        var content = "# Title\nThis is some content.\n## Subtitle\nMore content here.";

        // Act
        var chunks = await chunker.ChunkAsync(content);

        // Assert
        Assert.NotNull(chunks);
        Assert.NotEmpty(chunks);
    }

    [Fact]
    public async Task ChunkAsync_WithEmptyContent_ReturnsEmptyEnumerable()
    {
        // Arrange
        var config = ChunkerConfiguration.CreateDefault();
        using var chunker = new StructureChunker(config);

        // Act
        var chunks = await chunker.ChunkAsync("");

        // Assert
        Assert.NotNull(chunks);
        Assert.Empty(chunks);
    }

    [Fact]
    public async Task ChunkAsync_WithCancellationToken_SupportsCancellation()
    {
        // Arrange
        var config = ChunkerConfiguration.CreateDefault();
        using var chunker = new StructureChunker(config);
        var content = "# Title\nContent";
        using var cts = new CancellationTokenSource();

        // Act
        var chunks = await chunker.ChunkAsync(content, cts.Token);

        // Assert
        Assert.NotNull(chunks);
    }

    [Fact]
    public async Task ProcessAsync_WithCancellationToken_SupportsCancellation()
    {
        // Arrange
        var config = ChunkerConfiguration.CreateDefault();
        using var chunker = new StructureChunker(config);
        var content = "# Title\nContent";
        using var cts = new CancellationTokenSource();

        // Act
        var result = await chunker.ProcessAsync(content, "test-doc", cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-doc", result.SourceId);
    }

    [Fact]
    public async Task ProcessAsync_WithKeywordsDisabled_DoesNotExtractKeywords()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            ExtractKeywords = false
        };
        using var chunker = new StructureChunker(config);
        var content = "# Title\nThis is some content with keywords like machine learning and artificial intelligence.";

        // Act
        var result = await chunker.ProcessAsync(content, "test-doc");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Chunks);
        
        // All chunks should have empty keyword lists
        Assert.All(result.Chunks, chunk => Assert.Empty(chunk.Keywords));
    }

    [Fact]
    public async Task ProcessAsync_WithMaxKeywordsLimit_RespectsLimit()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            ExtractKeywords = true,
            MaxKeywordsPerChunk = 2
        };
        using var chunker = new StructureChunker(config);
        var content = "# Title\nThis is some content with many keywords like machine learning, artificial intelligence, natural language processing, deep learning, and neural networks.";

        // Act
        var result = await chunker.ProcessAsync(content, "test-doc");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Chunks);
        
        // All chunks should have at most 2 keywords
        Assert.All(result.Chunks, chunk => Assert.True(chunk.Keywords.Count <= 2));
    }

    [Fact]
    public void Configuration_WithLegacyConstructor_ReturnsNull()
    {
        // Arrange
        var strategy = new MarkdownStructureChunker.Core.Strategies.PatternBasedStrategy(
            MarkdownStructureChunker.Core.Strategies.PatternBasedStrategy.CreateDefaultRules());
        var extractor = new MarkdownStructureChunker.Core.Extractors.SimpleKeywordExtractor();

        // Act
        using var chunker = new StructureChunker(strategy, extractor);

        // Assert
        Assert.Null(chunker.Configuration);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var config = ChunkerConfiguration.CreateDefault();
        var chunker = new StructureChunker(config);

        // Act & Assert
        chunker.Dispose();
        chunker.Dispose(); // Should not throw
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessAsync_WithExtractKeywordsConfiguration_RespectsSettings(bool extractKeywords)
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            ExtractKeywords = extractKeywords
        };
        using var chunker = new StructureChunker(config);
        var content = "# Title\nThis content contains technical terms like algorithm and database.";

        // Act
        var result = await chunker.ProcessAsync(content, "test-doc");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Chunks);

        if (extractKeywords)
        {
            // Should have keywords extracted
            Assert.Contains(result.Chunks, chunk => chunk.Keywords.Any());
        }
        else
        {
            // Should not have keywords
            Assert.All(result.Chunks, chunk => Assert.Empty(chunk.Keywords));
        }
    }

    [Fact]
    public async Task ChunkAsync_WithNullContent_ReturnsEmptyEnumerable()
    {
        // Arrange
        var config = ChunkerConfiguration.CreateDefault();
        using var chunker = new StructureChunker(config);

        // Act
        var chunks = await chunker.ChunkAsync(null!);

        // Assert
        Assert.NotNull(chunks);
        Assert.Empty(chunks);
    }

    [Fact]
    public async Task ChunkAsync_WithWhitespaceContent_ReturnsEmptyEnumerable()
    {
        // Arrange
        var config = ChunkerConfiguration.CreateDefault();
        using var chunker = new StructureChunker(config);

        // Act
        var chunks = await chunker.ChunkAsync("   \n\t   ");

        // Assert
        Assert.NotNull(chunks);
        Assert.Empty(chunks);
    }

    [Fact]
    public async Task ProcessAsync_BackwardCompatibility_WorksWithExistingAPI()
    {
        // Arrange
        var config = ChunkerConfiguration.CreateDefault();
        using var chunker = new StructureChunker(config);
        var content = "# Title\nContent";

        // Act - Test the original ProcessAsync method without cancellation token
        var result = await chunker.ProcessAsync(content, "test-doc");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-doc", result.SourceId);
        Assert.NotEmpty(result.Chunks);
    }

    [Fact]
    public void Process_SynchronousMethod_WorksWithConfiguration()
    {
        // Arrange
        var config = ChunkerConfiguration.CreateDefault();
        using var chunker = new StructureChunker(config);
        var content = "# Title\nContent";

        // Act
        var result = chunker.Process(content, "test-doc");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-doc", result.SourceId);
        Assert.NotEmpty(result.Chunks);
    }
}

