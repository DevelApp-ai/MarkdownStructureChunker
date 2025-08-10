using MarkdownStructureChunker.Core.Configuration;
using Xunit;

namespace MarkdownStructureChunker.Tests.Configuration;

public class ChunkerConfigurationTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsExpectedDefaults()
    {
        // Act
        var config = new ChunkerConfiguration();

        // Assert
        Assert.Equal(1000, config.MaxChunkSize);
        Assert.Equal(200, config.ChunkOverlap);
        Assert.True(config.PreserveStructure);
        Assert.Equal(100, config.MinChunkSize);
        Assert.True(config.SplitOnSentences);
        Assert.True(config.RespectSectionBoundaries);
        Assert.True(config.IncludeHeadingHierarchy);
        Assert.True(config.ExtractKeywords);
        Assert.Equal(10, config.MaxKeywordsPerChunk);
        Assert.True(config.CalculateOffsets);
        Assert.False(config.PreserveOriginalMarkdown);
    }

    [Fact]
    public void Validate_ValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            MaxChunkSize = 1000,
            MinChunkSize = 100,
            ChunkOverlap = 200,
            MaxKeywordsPerChunk = 5
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_MaxChunkSizeZero_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration { MaxChunkSize = 0 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("MaxChunkSize must be greater than 0", exception.Message);
    }

    [Fact]
    public void Validate_MinChunkSizeZero_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration { MinChunkSize = 0 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("MinChunkSize must be greater than 0", exception.Message);
    }

    [Fact]
    public void Validate_MinChunkSizeGreaterThanMaxChunkSize_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration 
        { 
            MaxChunkSize = 500,
            MinChunkSize = 600
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("MinChunkSize must be less than MaxChunkSize", exception.Message);
    }

    [Fact]
    public void Validate_NegativeChunkOverlap_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration { ChunkOverlap = -10 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("ChunkOverlap cannot be negative", exception.Message);
    }

    [Fact]
    public void Validate_ChunkOverlapGreaterThanMaxChunkSize_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration 
        { 
            MaxChunkSize = 500,
            ChunkOverlap = 600
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("ChunkOverlap must be less than MaxChunkSize", exception.Message);
    }

    [Fact]
    public void Validate_ZeroMaxKeywordsPerChunk_ThrowsArgumentException()
    {
        // Arrange
        var config = new ChunkerConfiguration { MaxKeywordsPerChunk = 0 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("MaxKeywordsPerChunk must be greater than 0", exception.Message);
    }

    [Fact]
    public void CreateDefault_ReturnsValidConfiguration()
    {
        // Act
        var config = ChunkerConfiguration.CreateDefault();

        // Assert
        Assert.NotNull(config);
        config.Validate(); // Should not throw
        Assert.Equal(1000, config.MaxChunkSize);
        Assert.Equal(200, config.ChunkOverlap);
        Assert.True(config.PreserveStructure);
    }

    [Fact]
    public void CreateForLargeDocuments_ReturnsOptimizedConfiguration()
    {
        // Act
        var config = ChunkerConfiguration.CreateForLargeDocuments();

        // Assert
        Assert.NotNull(config);
        config.Validate(); // Should not throw
        Assert.Equal(2000, config.MaxChunkSize);
        Assert.Equal(400, config.ChunkOverlap);
        Assert.Equal(200, config.MinChunkSize);
        Assert.True(config.PreserveStructure);
        Assert.True(config.SplitOnSentences);
        Assert.True(config.RespectSectionBoundaries);
    }

    [Fact]
    public void CreateForSmallDocuments_ReturnsOptimizedConfiguration()
    {
        // Act
        var config = ChunkerConfiguration.CreateForSmallDocuments();

        // Assert
        Assert.NotNull(config);
        config.Validate(); // Should not throw
        Assert.Equal(500, config.MaxChunkSize);
        Assert.Equal(100, config.ChunkOverlap);
        Assert.Equal(50, config.MinChunkSize);
        Assert.True(config.PreserveStructure);
        Assert.True(config.SplitOnSentences);
        Assert.True(config.RespectSectionBoundaries);
    }

    [Fact]
    public void CreateForPerformance_ReturnsOptimizedConfiguration()
    {
        // Act
        var config = ChunkerConfiguration.CreateForPerformance();

        // Assert
        Assert.NotNull(config);
        config.Validate(); // Should not throw
        Assert.Equal(1500, config.MaxChunkSize);
        Assert.Equal(150, config.ChunkOverlap);
        Assert.Equal(150, config.MinChunkSize);
        Assert.False(config.PreserveStructure);
        Assert.False(config.SplitOnSentences);
        Assert.False(config.RespectSectionBoundaries);
        Assert.False(config.IncludeHeadingHierarchy);
        Assert.False(config.ExtractKeywords);
        Assert.False(config.CalculateOffsets);
        Assert.False(config.PreserveOriginalMarkdown);
    }

    [Theory]
    [InlineData(1000, 200, 100, 10)]
    [InlineData(2000, 400, 200, 15)]
    [InlineData(500, 50, 50, 5)]
    public void Validate_ValidParameterCombinations_DoesNotThrow(
        int maxChunkSize, 
        int chunkOverlap, 
        int minChunkSize, 
        int maxKeywords)
    {
        // Arrange
        var config = new ChunkerConfiguration
        {
            MaxChunkSize = maxChunkSize,
            ChunkOverlap = chunkOverlap,
            MinChunkSize = minChunkSize,
            MaxKeywordsPerChunk = maxKeywords
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    [Fact]
    public void PropertySetters_AllowCustomization()
    {
        // Arrange & Act
        var config = new ChunkerConfiguration
        {
            MaxChunkSize = 1500,
            ChunkOverlap = 300,
            PreserveStructure = false,
            MinChunkSize = 150,
            SplitOnSentences = false,
            RespectSectionBoundaries = false,
            IncludeHeadingHierarchy = false,
            ExtractKeywords = false,
            MaxKeywordsPerChunk = 20,
            CalculateOffsets = false,
            PreserveOriginalMarkdown = true
        };

        // Assert
        Assert.Equal(1500, config.MaxChunkSize);
        Assert.Equal(300, config.ChunkOverlap);
        Assert.False(config.PreserveStructure);
        Assert.Equal(150, config.MinChunkSize);
        Assert.False(config.SplitOnSentences);
        Assert.False(config.RespectSectionBoundaries);
        Assert.False(config.IncludeHeadingHierarchy);
        Assert.False(config.ExtractKeywords);
        Assert.Equal(20, config.MaxKeywordsPerChunk);
        Assert.False(config.CalculateOffsets);
        Assert.True(config.PreserveOriginalMarkdown);
    }
}

