using MarkdownStructureChunker.Core.Extensions;
using MarkdownStructureChunker.Core.Models;
using Xunit;

namespace MarkdownStructureChunker.Tests.Extensions;

/// <summary>
/// Tests for ChunkNode extension methods.
/// </summary>
public class ChunkNodeExtensionsTests
{
    private static ChunkNode CreateChunk(string title, params string[] keywords)
    {
        return new ChunkNode
        {
            Id = Guid.NewGuid(),
            CleanTitle = title,
            Content = $"Content for {title}",
            Keywords = keywords.ToList()
        };
    }

    [Fact]
    public void FindRelatedByKeywords_WithSharedKeywords_ReturnsRelatedChunks()
    {
        // Arrange
        var sourceChunk = CreateChunk("Source", "api", "rest", "endpoint");
        var relatedChunk1 = CreateChunk("Related1", "api", "web", "service");
        var relatedChunk2 = CreateChunk("Related2", "rest", "http", "protocol");
        var unrelatedChunk = CreateChunk("Unrelated", "database", "sql", "schema");
        
        var otherChunks = new[] { relatedChunk1, relatedChunk2, unrelatedChunk };

        // Act
        var result = sourceChunk.FindRelatedByKeywords(otherChunks).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(relatedChunk1, result);
        Assert.Contains(relatedChunk2, result);
        Assert.DoesNotContain(unrelatedChunk, result);
    }

    [Fact]
    public void FindRelatedByKeywords_WithMinimumSharedKeywords_RespectsMinimum()
    {
        // Arrange
        var sourceChunk = CreateChunk("Source", "api", "rest", "endpoint");
        var oneSharedChunk = CreateChunk("OneShared", "api", "database", "sql");
        var twoSharedChunk = CreateChunk("TwoShared", "api", "rest", "web");
        
        var otherChunks = new[] { oneSharedChunk, twoSharedChunk };

        // Act
        var result = sourceChunk.FindRelatedByKeywords(otherChunks, minimumSharedKeywords: 2).ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains(twoSharedChunk, result);
        Assert.DoesNotContain(oneSharedChunk, result);
    }

    [Fact]
    public void FindRelatedByKeywords_WithNoKeywords_ReturnsEmpty()
    {
        // Arrange
        var sourceChunk = CreateChunk("Source");
        var otherChunk = CreateChunk("Other", "api", "rest");
        
        var otherChunks = new[] { otherChunk };

        // Act
        var result = sourceChunk.FindRelatedByKeywords(otherChunks).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindRelatedByKeywords_ExcludesSelf()
    {
        // Arrange
        var sourceChunk = CreateChunk("Source", "api", "rest");
        var otherChunks = new[] { sourceChunk };

        // Act
        var result = sourceChunk.FindRelatedByKeywords(otherChunks).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateKeywordSimilarity_WithIdenticalKeywords_ReturnsOne()
    {
        // Arrange
        var chunk1 = CreateChunk("Chunk1", "api", "rest", "endpoint");
        var chunk2 = CreateChunk("Chunk2", "api", "rest", "endpoint");

        // Act
        var similarity = chunk1.CalculateKeywordSimilarity(chunk2);

        // Assert
        Assert.Equal(1.0, similarity, precision: 2);
    }

    [Fact]
    public void CalculateKeywordSimilarity_WithNoSharedKeywords_ReturnsZero()
    {
        // Arrange
        var chunk1 = CreateChunk("Chunk1", "api", "rest");
        var chunk2 = CreateChunk("Chunk2", "database", "sql");

        // Act
        var similarity = chunk1.CalculateKeywordSimilarity(chunk2);

        // Assert
        Assert.Equal(0.0, similarity);
    }

    [Fact]
    public void CalculateKeywordSimilarity_WithPartialOverlap_ReturnsCorrectScore()
    {
        // Arrange
        var chunk1 = CreateChunk("Chunk1", "api", "rest", "endpoint");
        var chunk2 = CreateChunk("Chunk2", "api", "web", "service");

        // Act
        var similarity = chunk1.CalculateKeywordSimilarity(chunk2);

        // Assert
        // Intersection: 1 (api), Union: 5 (api, rest, endpoint, web, service)
        Assert.Equal(0.2, similarity, precision: 2);
    }

    [Fact]
    public void CalculateKeywordSimilarity_WithEmptyKeywords_ReturnsZero()
    {
        // Arrange
        var chunk1 = CreateChunk("Chunk1");
        var chunk2 = CreateChunk("Chunk2", "api", "rest");

        // Act
        var similarity = chunk1.CalculateKeywordSimilarity(chunk2);

        // Assert
        Assert.Equal(0.0, similarity);
    }

    [Fact]
    public void ContainsAnyKeyword_WithMatchingKeywords_ReturnsTrue()
    {
        // Arrange
        var chunk = CreateChunk("Chunk", "api", "rest", "endpoint");
        var searchKeywords = new[] { "database", "api", "sql" };

        // Act
        var result = chunk.ContainsAnyKeyword(searchKeywords);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsAnyKeyword_WithNoMatchingKeywords_ReturnsFalse()
    {
        // Arrange
        var chunk = CreateChunk("Chunk", "api", "rest", "endpoint");
        var searchKeywords = new[] { "database", "sql", "schema" };

        // Act
        var result = chunk.ContainsAnyKeyword(searchKeywords);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsAnyKeyword_IsCaseInsensitive()
    {
        // Arrange
        var chunk = CreateChunk("Chunk", "api", "rest", "endpoint");
        var searchKeywords = new[] { "API", "DATABASE" };

        // Act
        var result = chunk.ContainsAnyKeyword(searchKeywords);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsAllKeywords_WithAllMatching_ReturnsTrue()
    {
        // Arrange
        var chunk = CreateChunk("Chunk", "api", "rest", "endpoint", "web");
        var searchKeywords = new[] { "api", "rest", "endpoint" };

        // Act
        var result = chunk.ContainsAllKeywords(searchKeywords);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsAllKeywords_WithSomeMissing_ReturnsFalse()
    {
        // Arrange
        var chunk = CreateChunk("Chunk", "api", "rest");
        var searchKeywords = new[] { "api", "rest", "endpoint" };

        // Act
        var result = chunk.ContainsAllKeywords(searchKeywords);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetAllKeywords_FromMultipleChunks_ReturnsDistinctKeywords()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Chunk1", "api", "rest", "endpoint"),
            CreateChunk("Chunk2", "api", "web", "service"),
            CreateChunk("Chunk3", "database", "sql")
        };

        // Act
        var result = chunks.GetAllKeywords();

        // Assert
        Assert.Equal(6, result.Count);
        Assert.Contains("api", result);
        Assert.Contains("rest", result);
        Assert.Contains("endpoint", result);
        Assert.Contains("web", result);
        Assert.Contains("service", result);
        Assert.Contains("database", result);
        Assert.Contains("sql", result);
    }

    [Fact]
    public void GetAllKeywords_ReturnsOrderedKeywords()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Chunk1", "zebra", "alpha", "beta")
        };

        // Act
        var result = chunks.GetAllKeywords();

        // Assert
        Assert.Equal("alpha", result[0]);
        Assert.Equal("beta", result[1]);
        Assert.Equal("zebra", result[2]);
    }

    [Fact]
    public void GetTopKeywords_ReturnsFrequencyOrderedKeywords()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Chunk1", "api", "rest"),
            CreateChunk("Chunk2", "api", "web"),
            CreateChunk("Chunk3", "api", "database"),
            CreateChunk("Chunk4", "rest", "endpoint")
        };

        // Act
        var result = chunks.GetTopKeywords(3);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(("api", 3), result[0]);
        Assert.Equal(("rest", 2), result[1]);
        // Third item could be any of the single-occurrence keywords
        Assert.Equal(1, result[2].Frequency);
    }

    [Fact]
    public void CreateKeywordIndex_CreatesCorrectMapping()
    {
        // Arrange
        var chunk1 = CreateChunk("Chunk1", "api", "rest");
        var chunk2 = CreateChunk("Chunk2", "api", "web");
        var chunk3 = CreateChunk("Chunk3", "database");
        var chunks = new[] { chunk1, chunk2, chunk3 };

        // Act
        var index = chunks.CreateKeywordIndex();

        // Assert
        Assert.Equal(4, index.Count);
        
        Assert.Equal(2, index["api"].Count);
        Assert.Contains(chunk1, index["api"]);
        Assert.Contains(chunk2, index["api"]);
        
        Assert.Single(index["rest"]);
        Assert.Contains(chunk1, index["rest"]);
        
        Assert.Single(index["web"]);
        Assert.Contains(chunk2, index["web"]);
        
        Assert.Single(index["database"]);
        Assert.Contains(chunk3, index["database"]);
    }

    [Fact]
    public void CreateKeywordIndex_IsCaseInsensitive()
    {
        // Arrange
        var chunk1 = CreateChunk("Chunk1", "API", "rest");
        var chunk2 = CreateChunk("Chunk2", "api", "REST");
        var chunks = new[] { chunk1, chunk2 };

        // Act
        var index = chunks.CreateKeywordIndex();

        // Assert
        Assert.Equal(2, index.Count);
        Assert.True(index.ContainsKey("api") || index.ContainsKey("API"));
        Assert.True(index.ContainsKey("rest") || index.ContainsKey("REST"));
    }

    [Fact]
    public void GroupBySharedKeywords_GroupsChunksWithSharedKeywords()
    {
        // Arrange
        var chunk1 = CreateChunk("Chunk1", "api", "rest", "endpoint");
        var chunk2 = CreateChunk("Chunk2", "api", "rest", "web");
        var chunk3 = CreateChunk("Chunk3", "database", "sql", "schema");
        var chunk4 = CreateChunk("Chunk4", "database", "sql", "table");
        var chunks = new[] { chunk1, chunk2, chunk3, chunk4 };

        // Act
        var groups = chunks.GroupBySharedKeywords(2).ToList();

        // Assert
        Assert.Equal(2, groups.Count);
        
        // Each group should have 2 chunks
        Assert.All(groups, group => Assert.Equal(2, group.Count()));
    }
}

