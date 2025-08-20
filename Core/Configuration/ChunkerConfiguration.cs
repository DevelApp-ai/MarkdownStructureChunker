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

    // CUSTOM KEYWORD SUPPORT

    /// <summary>
    /// Gets or sets custom keywords to add to all chunks.
    /// These keywords will be combined with automatically extracted keywords.
    /// Useful for adding domain-specific terms, project tags, or classification labels.
    /// Default is an empty list.
    /// </summary>
    public IReadOnlyList<string> CustomKeywords { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets document-specific keyword mappings based on heading patterns.
    /// Key: Regular expression pattern to match against heading text.
    /// Value: List of keywords to add to chunks under matching headings.
    /// This enables targeted keyword assignment for specific document sections.
    /// Default is an empty dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> SectionKeywordMappings { get; set; } 
        = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>
    /// Gets or sets whether custom keywords should be prioritized over extracted keywords.
    /// When true, custom keywords are added first and extracted keywords fill remaining slots.
    /// When false, all keywords are mixed and sorted by relevance.
    /// Default is true.
    /// </summary>
    public bool PrioritizeCustomKeywords { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to inherit custom keywords from parent chunks.
    /// When true, child chunks will include keywords from their parent sections.
    /// This enables hierarchical keyword propagation through the document structure.
    /// Default is false.
    /// </summary>
    public bool InheritParentKeywords { get; set; } = false;

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

        // Validate custom keywords
        if (CustomKeywords.Any(k => string.IsNullOrWhiteSpace(k)))
            throw new ArgumentException("CustomKeywords cannot contain null or empty values.", nameof(CustomKeywords));

        // Validate section keyword mappings
        foreach (var mapping in SectionKeywordMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.Key))
                throw new ArgumentException("SectionKeywordMappings cannot contain null or empty pattern keys.", nameof(SectionKeywordMappings));

            if (mapping.Value.Any(k => string.IsNullOrWhiteSpace(k)))
                throw new ArgumentException($"SectionKeywordMappings pattern '{mapping.Key}' contains null or empty keyword values.", nameof(SectionKeywordMappings));

            // Validate regex pattern
            try
            {
                _ = new System.Text.RegularExpressions.Regex(mapping.Key);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"SectionKeywordMappings contains invalid regex pattern '{mapping.Key}': {ex.Message}", nameof(SectionKeywordMappings));
            }
        }
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

    /// <summary>
    /// Creates a configuration with custom keywords for document tagging and cross-referencing.
    /// </summary>
    /// <param name="customKeywords">Global keywords to add to all chunks</param>
    /// <param name="sectionMappings">Section-specific keyword mappings (optional)</param>
    /// <param name="inheritParentKeywords">Whether to inherit keywords from parent chunks</param>
    /// <returns>A new ChunkerConfiguration with custom keyword support enabled.</returns>
    public static ChunkerConfiguration CreateWithCustomKeywords(
        IReadOnlyList<string> customKeywords,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? sectionMappings = null,
        bool inheritParentKeywords = false)
    {
        return new ChunkerConfiguration
        {
            CustomKeywords = customKeywords ?? throw new ArgumentNullException(nameof(customKeywords)),
            SectionKeywordMappings = sectionMappings ?? new Dictionary<string, IReadOnlyList<string>>(),
            InheritParentKeywords = inheritParentKeywords,
            PrioritizeCustomKeywords = true,
            ExtractKeywords = true,
            MaxKeywordsPerChunk = Math.Max(10, customKeywords.Count + 5) // Ensure room for both custom and extracted
        };
    }

    /// <summary>
    /// Creates a configuration optimized for document cross-referencing and mapping.
    /// Enables all features needed for linking related content across documents.
    /// </summary>
    /// <param name="projectKeywords">Project-specific keywords for cross-document mapping</param>
    /// <returns>A new ChunkerConfiguration optimized for document mapping.</returns>
    public static ChunkerConfiguration CreateForDocumentMapping(IReadOnlyList<string> projectKeywords)
    {
        return new ChunkerConfiguration
        {
            CustomKeywords = projectKeywords ?? throw new ArgumentNullException(nameof(projectKeywords)),
            InheritParentKeywords = true,
            PrioritizeCustomKeywords = true,
            ExtractKeywords = true,
            MaxKeywordsPerChunk = 15,
            IncludeHeadingHierarchy = true,
            PreserveStructure = true,
            CalculateOffsets = true
        };
    }
}

