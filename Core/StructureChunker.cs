using MarkdownStructureChunker.Core.Interfaces;
using MarkdownStructureChunker.Core.Models;
using MarkdownStructureChunker.Core.Configuration;
using MarkdownStructureChunker.Core.Strategies;
using MarkdownStructureChunker.Core.Extractors;

namespace MarkdownStructureChunker.Core;

/// <summary>
/// The main entry point for the MarkdownStructureChunker library.
/// Orchestrates the parsing, chunking, and keyword extraction processes.
/// Supports both configuration-based and strategy-based initialization.
/// </summary>
public class StructureChunker : IDisposable
{
    private readonly IChunkingStrategy _chunkingStrategy;
    private readonly IKeywordExtractor _keywordExtractor;
    private readonly ChunkerConfiguration? _configuration;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructureChunker"/> class with the specified strategy and extractor.
    /// This constructor maintains backward compatibility with the existing API.
    /// </summary>
    /// <param name="chunkingStrategy">The chunking strategy to use for document processing.</param>
    /// <param name="keywordExtractor">The keyword extractor to use for content analysis.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="chunkingStrategy"/> or <paramref name="keywordExtractor"/> is null.</exception>
    public StructureChunker(IChunkingStrategy chunkingStrategy, IKeywordExtractor keywordExtractor)
    {
        _chunkingStrategy = chunkingStrategy ?? throw new ArgumentNullException(nameof(chunkingStrategy));
        _keywordExtractor = keywordExtractor ?? throw new ArgumentNullException(nameof(keywordExtractor));
        _configuration = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StructureChunker"/> class with the specified configuration.
    /// This constructor provides the new configuration-based API requested by customers.
    /// </summary>
    /// <param name="configuration">The configuration to use for chunking behavior.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    public StructureChunker(ChunkerConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _configuration.Validate();

        // Create strategy and extractor based on configuration
        _chunkingStrategy = CreateStrategyFromConfiguration(_configuration);
        _keywordExtractor = CreateExtractorFromConfiguration(_configuration);
    }

    /// <summary>
    /// Creates a chunking strategy based on the provided configuration.
    /// </summary>
    /// <param name="config">The configuration to use</param>
    /// <returns>An appropriate chunking strategy</returns>
    private static IChunkingStrategy CreateStrategyFromConfiguration(ChunkerConfiguration config)
    {
        var rules = PatternBasedStrategy.CreateDefaultRules();
        
        // TODO: In future versions, we could create different strategies based on config
        // For now, we use the PatternBasedStrategy with default rules
        return new PatternBasedStrategy(rules);
    }

    /// <summary>
    /// Creates a keyword extractor based on the provided configuration.
    /// </summary>
    /// <param name="config">The configuration to use</param>
    /// <returns>An appropriate keyword extractor</returns>
    private static IKeywordExtractor CreateExtractorFromConfiguration(ChunkerConfiguration config)
    {
        if (!config.ExtractKeywords)
        {
            // Return a no-op extractor if keywords are disabled
            return new SimpleKeywordExtractor(); // We'll enhance this to respect MaxKeywordsPerChunk
        }

        // For now, use SimpleKeywordExtractor
        // TODO: In future versions, we could use MLNetKeywordExtractor based on config
        return new SimpleKeywordExtractor();
    }

    /// <summary>
    /// Processes a document and returns a structured graph of chunks with extracted keywords.
    /// This method maintains backward compatibility with the existing API.
    /// </summary>
    /// <param name="documentText">The input document text</param>
    /// <param name="sourceId">Identifier for the source document</param>
    /// <returns>A DocumentGraph containing all processed chunks</returns>
    public async Task<DocumentGraph> ProcessAsync(string documentText, string sourceId)
    {
        return await ProcessAsync(documentText, sourceId, CancellationToken.None);
    }

    /// <summary>
    /// Processes a document and returns a structured graph of chunks with extracted keywords.
    /// This overload supports cancellation tokens as requested by customers.
    /// </summary>
    /// <param name="documentText">The input document text</param>
    /// <param name="sourceId">Identifier for the source document</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A DocumentGraph containing all processed chunks</returns>
    public async Task<DocumentGraph> ProcessAsync(string documentText, string sourceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentText))
            throw new ArgumentException("Document text cannot be null or empty", nameof(documentText));

        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Source ID cannot be null or empty", nameof(sourceId));

        cancellationToken.ThrowIfCancellationRequested();

        // Step 1: Parse and chunk the document
        var chunks = _chunkingStrategy.ProcessText(documentText, sourceId);

        // Step 2: Extract keywords for each chunk (if enabled)
        var enrichedChunks = new List<ChunkNode>();
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var keywords = new List<string>();
            if (_configuration?.ExtractKeywords != false) // Extract keywords unless explicitly disabled
            {
                var extractedKeywords = await _keywordExtractor.ExtractKeywordsAsync(chunk.Content);
                keywords = extractedKeywords.ToList();
                
                // Respect MaxKeywordsPerChunk if configuration is available
                if (_configuration != null && keywords.Count > _configuration.MaxKeywordsPerChunk)
                {
                    keywords = keywords.Take(_configuration.MaxKeywordsPerChunk).ToList();
                }
            }

            var enrichedChunk = chunk with { Keywords = keywords };
            enrichedChunks.Add(enrichedChunk);
        }

        return new DocumentGraph
        {
            SourceId = sourceId,
            Chunks = enrichedChunks
        };
    }

    /// <summary>
    /// Processes a document and returns individual chunks as requested by customers.
    /// This method provides the ChunkAsync API that customers expect.
    /// </summary>
    /// <param name="content">The input document content</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>An enumerable of ChunkNode objects</returns>
    public async Task<IEnumerable<ChunkNode>> ChunkAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Enumerable.Empty<ChunkNode>();

        // Use a default source ID for this API
        var sourceId = Guid.NewGuid().ToString();
        var documentGraph = await ProcessAsync(content, sourceId, cancellationToken);
        
        return documentGraph.Chunks;
    }

    /// <summary>
    /// Synchronous version of ProcessAsync for backward compatibility.
    /// </summary>
    /// <param name="documentText">The input document text</param>
    /// <param name="sourceId">Identifier for the source document</param>
    /// <returns>A DocumentGraph containing all processed chunks</returns>
    public DocumentGraph Process(string documentText, string sourceId)
    {
        return ProcessAsync(documentText, sourceId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the current configuration used by this chunker instance.
    /// Returns null if the chunker was created with the legacy constructor.
    /// </summary>
    public ChunkerConfiguration? Configuration => _configuration;

    /// <summary>
    /// Releases all resources used by the <see cref="StructureChunker"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // Dispose of any disposable resources
            if (_keywordExtractor is IDisposable disposableExtractor)
            {
                disposableExtractor.Dispose();
            }

            if (_chunkingStrategy is IDisposable disposableStrategy)
            {
                disposableStrategy.Dispose();
            }

            _disposed = true;
        }
    }
}

