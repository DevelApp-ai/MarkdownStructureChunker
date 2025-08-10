namespace MarkdownStructureChunker.Core.Configuration;

/// <summary>
/// Configuration class for controlling chunking behavior and parameters.
/// </summary>
public class ChunkerConfiguration
{
    /// <summary>
    /// Gets or sets the maximum size of a chunk in characters.
    /// Default is 1000 characters.
    /// </summary>
    public int MaxChunkSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the overlap between chunks in characters.
    /// Default is 200 characters.
    /// </summary>
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>
    /// Gets or sets whether to preserve document structure when chunking.
    /// When true, chunks will respect section boundaries and hierarchical structure.
    /// Default is true.
    /// </summary>
    public bool PreserveStructure { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum size of a chunk in characters.
    /// Chunks smaller than this will be merged with adjacent chunks when possible.
    /// Default is 100 characters.
    /// </summary>
    public int MinChunkSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to split content on sentence boundaries.
    /// When true, chunks will prefer to break at sentence endings.
    /// Default is true.
    /// </summary>
    public bool SplitOnSentences { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to respect section boundaries when chunking.
    /// When true, chunks will not cross major section boundaries.
    /// Default is true.
    /// </summary>
    public bool RespectSectionBoundaries { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include heading hierarchy in chunk metadata.
    /// When true, each chunk will include its full heading path.
    /// Default is true.
    /// </summary>
    public bool IncludeHeadingHierarchy { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to extract keywords from chunk content.
    /// Default is true.
    /// </summary>
    public bool ExtractKeywords { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of keywords to extract per chunk.
    /// Default is 10.
    /// </summary>
    public int MaxKeywordsPerChunk { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to calculate precise character offsets for chunks.
    /// When true, StartOffset and EndOffset will be accurately calculated.
    /// Default is true.
    /// </summary>
    public bool CalculateOffsets { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to preserve original markdown formatting in chunks.
    /// When true, OriginalMarkdown property will contain the raw markdown.
    /// Default is false to save memory.
    /// </summary>
    public bool PreserveOriginalMarkdown { get; set; } = false;

    /// <summary>
    /// Validates the configuration and throws an exception if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration values are invalid.</exception>
    public void Validate()
    {
        if (MaxChunkSize <= 0)
            throw new ArgumentException("MaxChunkSize must be greater than 0.", nameof(MaxChunkSize));

        if (MinChunkSize <= 0)
            throw new ArgumentException("MinChunkSize must be greater than 0.", nameof(MinChunkSize));

        if (MinChunkSize >= MaxChunkSize)
            throw new ArgumentException("MinChunkSize must be less than MaxChunkSize.", nameof(MinChunkSize));

        if (ChunkOverlap < 0)
            throw new ArgumentException("ChunkOverlap cannot be negative.", nameof(ChunkOverlap));

        if (ChunkOverlap >= MaxChunkSize)
            throw new ArgumentException("ChunkOverlap must be less than MaxChunkSize.", nameof(ChunkOverlap));

        if (MaxKeywordsPerChunk <= 0)
            throw new ArgumentException("MaxKeywordsPerChunk must be greater than 0.", nameof(MaxKeywordsPerChunk));
    }

    /// <summary>
    /// Creates a default configuration optimized for general document processing.
    /// </summary>
    /// <returns>A new ChunkerConfiguration with default settings.</returns>
    public static ChunkerConfiguration CreateDefault()
    {
        return new ChunkerConfiguration();
    }

    /// <summary>
    /// Creates a configuration optimized for large documents.
    /// </summary>
    /// <returns>A new ChunkerConfiguration optimized for large documents.</returns>
    public static ChunkerConfiguration CreateForLargeDocuments()
    {
        return new ChunkerConfiguration
        {
            MaxChunkSize = 2000,
            ChunkOverlap = 400,
            MinChunkSize = 200,
            PreserveStructure = true,
            SplitOnSentences = true,
            RespectSectionBoundaries = true
        };
    }

    /// <summary>
    /// Creates a configuration optimized for small documents or precise chunking.
    /// </summary>
    /// <returns>A new ChunkerConfiguration optimized for small documents.</returns>
    public static ChunkerConfiguration CreateForSmallDocuments()
    {
        return new ChunkerConfiguration
        {
            MaxChunkSize = 500,
            ChunkOverlap = 100,
            MinChunkSize = 50,
            PreserveStructure = true,
            SplitOnSentences = true,
            RespectSectionBoundaries = true
        };
    }

    /// <summary>
    /// Creates a configuration optimized for performance with minimal metadata.
    /// </summary>
    /// <returns>A new ChunkerConfiguration optimized for performance.</returns>
    public static ChunkerConfiguration CreateForPerformance()
    {
        return new ChunkerConfiguration
        {
            MaxChunkSize = 1500,
            ChunkOverlap = 150,
            MinChunkSize = 150,
            PreserveStructure = false,
            SplitOnSentences = false,
            RespectSectionBoundaries = false,
            IncludeHeadingHierarchy = false,
            ExtractKeywords = false,
            CalculateOffsets = false,
            PreserveOriginalMarkdown = false
        };
    }
}

