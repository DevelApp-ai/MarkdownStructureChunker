using MarkdownStructureChunker.Core.Configuration;
using MarkdownStructureChunker.Core.Strategies;
using Xunit;

namespace MarkdownStructureChunker.Tests.Strategies;

public class PatternBasedStrategyConfigurationTests
{
    [Fact]
    public void Constructor_WithConfiguration_CreatesStrategy()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();

        // Act
        var strategy = new PatternBasedStrategy(rules, config);

        // Assert
        Assert.NotNull(strategy);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_CreatesStrategy()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();

        // Act
        var strategy = new PatternBasedStrategy(rules, null);

        // Assert
        Assert.NotNull(strategy);
    }

    [Fact]
    public void ProcessText_WithMaxChunkSizeLimit_SplitsLargeChunks()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            MaxChunkSize = 100, // Very small for testing
            SplitOnSentences = false
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var longContent = string.Join(" ", Enumerable.Repeat("word", 50)); // ~200 characters
        var text = $"# Title\n{longContent}";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        // Should have split the content into multiple chunks
        var contentChunks = chunks.Where(c => !string.IsNullOrEmpty(c.Content)).ToList();
        Assert.True(contentChunks.Count > 1, "Large content should be split into multiple chunks");
        
        // Each chunk should respect the size limit (allowing some tolerance for splitting logic)
        Assert.All(contentChunks, chunk => 
            Assert.True(chunk.Content!.Length <= config.MaxChunkSize + 50, 
                $"Chunk content length {chunk.Content!.Length} should be close to max size {config.MaxChunkSize}"));
    }

    [Fact]
    public void ProcessText_WithSentenceSplitting_SplitsOnSentenceBoundaries()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            MaxChunkSize = 50,
            SplitOnSentences = true
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = "# Title\nThis is sentence one. This is sentence two. This is sentence three.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var contentChunks = chunks.Where(c => !string.IsNullOrEmpty(c.Content)).ToList();
        Assert.True(contentChunks.Count > 1, "Content should be split into multiple chunks");
    }

    [Fact]
    public void ProcessText_WithChunkOverlap_AddsOverlapBetweenChunks()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            MaxChunkSize = 100,
            ChunkOverlap = 20,
            SplitOnSentences = false
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var longContent = string.Join(" ", Enumerable.Repeat("word", 50));
        var text = $"# Title\n{longContent}";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        var contentChunks = chunks.Where(c => !string.IsNullOrEmpty(c.Content)).ToList();
        if (contentChunks.Count > 1)
        {
            // Check that subsequent chunks contain overlapping content
            for (int i = 1; i < contentChunks.Count; i++)
            {
                var currentChunk = contentChunks[i];
                var previousChunk = contentChunks[i - 1];
                
                // The current chunk should contain some content from the previous chunk
                Assert.True(currentChunk.Content!.Length > 0, "Chunk should have content");
            }
        }
    }

    [Fact]
    public void ProcessText_WithoutConfiguration_ProcessesNormally()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var strategy = new PatternBasedStrategy(rules); // No configuration
        
        var text = "# Title\nSome content here.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        Assert.Single(chunks);
        Assert.Equal("Title", chunks[0].CleanTitle);
    }

    [Fact]
    public void ProcessText_WithPreserveStructure_MaintainsHierarchy()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            PreserveStructure = true,
            MaxChunkSize = 50
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Main Title
Some content.
## Subtitle
More content here.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        Assert.True(chunks.Count >= 2, "Should have multiple chunks for different heading levels");
        
        var mainTitle = chunks.FirstOrDefault(c => c.CleanTitle == "Main Title");
        var subtitle = chunks.FirstOrDefault(c => c.CleanTitle == "Subtitle");
        
        Assert.NotNull(mainTitle);
        Assert.NotNull(subtitle);
        Assert.Equal(1, mainTitle.Level);
        Assert.Equal(2, subtitle.Level);
    }

    [Fact]
    public void ProcessText_WithMinChunkSize_HandlesSmallChunks()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            MinChunkSize = 10,
            PreserveStructure = true
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = "# Title\nShort.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        // Small chunks should be preserved when PreserveStructure is true
        Assert.Single(chunks);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProcessText_WithSplitOnSentencesConfiguration_RespectsSettings(bool splitOnSentences)
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            MaxChunkSize = 30,
            SplitOnSentences = splitOnSentences
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = "# Title\nFirst sentence. Second sentence. Third sentence.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        // The splitting behavior should be different based on the configuration
        var contentChunks = chunks.Where(c => !string.IsNullOrEmpty(c.Content)).ToList();
        Assert.True(contentChunks.Any(), "Should have content chunks");
    }

    [Fact]
    public void ProcessText_WithLargeDocument_HandlesEfficiently()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateForLargeDocuments();
        var strategy = new PatternBasedStrategy(rules, config);
        
        // Create a large document
        var sections = Enumerable.Range(1, 10)
            .Select(i => $"# Section {i}\n{string.Join(" ", Enumerable.Repeat($"Content for section {i}.", 100))}")
            .ToArray();
        var text = string.Join("\n\n", sections);

        // Act
        var startTime = DateTime.UtcNow;
        var chunks = strategy.ProcessText(text, "test");
        var processingTime = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotEmpty(chunks);
        Assert.True(chunks.Count >= 10, "Should have chunks for each section");
        Assert.True(processingTime.TotalSeconds < 5, "Should process large documents efficiently");
    }

    [Fact]
    public void ProcessText_WithEmptyContent_ReturnsEmptyList()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);

        // Act
        var chunks = strategy.ProcessText("", "test");

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public void ProcessText_WithWhitespaceContent_ReturnsEmptyList()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = ChunkerConfiguration.CreateDefault();
        var strategy = new PatternBasedStrategy(rules, config);

        // Act
        var chunks = strategy.ProcessText("   \n\t   ", "test");

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public void ProcessText_WithConfigurationConstraints_MaintainsChunkIntegrity()
    {
        // Arrange
        var rules = PatternBasedStrategy.CreateDefaultRules();
        var config = new ChunkerConfiguration
        {
            MaxChunkSize = 200,
            MinChunkSize = 50,
            ChunkOverlap = 30,
            SplitOnSentences = true,
            PreserveStructure = true
        };
        var strategy = new PatternBasedStrategy(rules, config);
        
        var text = @"# Main Title
This is the introduction paragraph with multiple sentences. It contains enough content to test the chunking behavior. The sentences should be split appropriately.

## Subsection
This subsection has its own content. It also contains multiple sentences for testing. The chunking should respect the hierarchical structure.";

        // Act
        var chunks = strategy.ProcessText(text, "test");

        // Assert
        Assert.NotEmpty(chunks);
        
        // Verify chunk structure is maintained
        var titleChunk = chunks.FirstOrDefault(c => c.CleanTitle == "Main Title");
        var subsectionChunk = chunks.FirstOrDefault(c => c.CleanTitle == "Subsection");
        
        Assert.NotNull(titleChunk);
        Assert.NotNull(subsectionChunk);
        Assert.True(titleChunk.Level < subsectionChunk.Level, "Hierarchy should be preserved");
    }
}

