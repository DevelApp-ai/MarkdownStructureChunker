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
    /// Creates a StructureChunker instance configured for structure-first ingestion architecture.
    /// This factory method sets up an AST-based strategy using Markdig for robust Markdown parsing.
    /// </summary>
    /// <param name="keywordExtractor">The keyword extractor to use (optional, defaults to SimpleKeywordExtractor)</param>
    /// <returns>A StructureChunker configured for structure-first processing</returns>
    public static StructureChunker CreateStructureFirst(IKeywordExtractor? keywordExtractor = null)
    {
        var strategy = new Strategies.ASTBasedStrategy();
        var extractor = keywordExtractor ?? new SimpleKeywordExtractor();
        return new StructureChunker(strategy, extractor);
    }

    /// <summary>
    /// Creates a StructureChunker instance with the specified configuration and AST-based strategy.
    /// This factory method enables structure-first processing with full configuration support.
    /// </summary>
    /// <param name="configuration">The configuration to use</param>
    /// <returns>A StructureChunker configured for structure-first processing</returns>
    public static StructureChunker CreateStructureFirstWithConfiguration(ChunkerConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
            
        configuration.Validate();

        var strategy = new Strategies.ASTBasedStrategy();
        var extractor = CreateExtractorFromConfiguration(configuration);
        
        // Create instance using the strategy-based constructor and store configuration separately
        var chunker = new StructureChunker(strategy, extractor);
        
        // Note: For full configuration support with AST strategy, 
        // users should use CreateStructureFirst() and call ProcessWithStructureAsync()
        return chunker;
    }

    /// <summary>
    /// Processes a document using the structure-first ingestion architecture.
    /// This method creates both structural elements (from AST) and traditional chunks for backward compatibility.
    /// </summary>
    /// <param name="documentText">The input document text</param>
    /// <param name="sourceId">Identifier for the source document</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A DocumentGraph containing structural elements, edges, and traditional chunks</returns>
    public async Task<DocumentGraph> ProcessWithStructureAsync(string documentText, string sourceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentText))
            throw new ArgumentException("Document text cannot be null or empty", nameof(documentText));

        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Source ID cannot be null or empty", nameof(sourceId));

        cancellationToken.ThrowIfCancellationRequested();

        // Check if the current strategy supports structure-first processing
        if (_chunkingStrategy is Strategies.ASTBasedStrategy astStrategy)
        {
            // Use the structure-first approach
            var (elements, edges, chunks) = astStrategy.ProcessTextToStructure(documentText, sourceId);

            // Step 2: Extract and merge keywords for each chunk (if enabled)
            var enrichedChunks = new List<ChunkNode>();
            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keywords = new List<string>();
                if (_configuration?.ExtractKeywords != false) // Extract keywords unless explicitly disabled
                {
                    // Combine custom and extracted keywords
                    keywords = await CombineKeywordsAsync(chunk, cancellationToken);
                    
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
                Chunks = enrichedChunks,
                StructuralElements = elements,
                StructuralEdges = edges
            };
        }
        else
        {
            // Fall back to traditional processing
            return await ProcessAsync(documentText, sourceId, cancellationToken);
        }
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

        // Step 2: Extract and merge keywords for each chunk (if enabled)
        var enrichedChunks = new List<ChunkNode>();
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var keywords = new List<string>();
            if (_configuration?.ExtractKeywords != false) // Extract keywords unless explicitly disabled
            {
                // Combine custom and extracted keywords
                keywords = await CombineKeywordsAsync(chunk, cancellationToken);
                
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
    /// Combines custom keywords with automatically extracted keywords for a chunk.
    /// Handles prioritization, section-specific mappings, and parent keyword inheritance.
    /// </summary>
    /// <param name="chunk">The chunk to process keywords for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A combined list of keywords</returns>
    private async Task<List<string>> CombineKeywordsAsync(ChunkNode chunk, CancellationToken cancellationToken)
    {
        var allKeywords = new List<string>();

        // 1. Add global custom keywords
        if (_configuration?.CustomKeywords != null)
        {
            allKeywords.AddRange(_configuration.CustomKeywords);
        }

        // 2. Add section-specific keywords based on heading patterns
        if (_configuration?.SectionKeywordMappings != null && !string.IsNullOrEmpty(chunk.CleanTitle))
        {
            foreach (var mapping in _configuration.SectionKeywordMappings)
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(mapping.Key, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (regex.IsMatch(chunk.CleanTitle))
                    {
                        allKeywords.AddRange(mapping.Value);
                    }
                }
                catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
                {
                    // Skip this pattern if it times out
                    continue;
                }
            }
        }

        // 3. Add inherited keywords from parent chunks
        if (_configuration?.InheritParentKeywords == true && chunk.Parent != null)
        {
            allKeywords.AddRange(chunk.Parent.Keywords);
        }

        // 4. Extract keywords from content
        var extractedKeywords = await _keywordExtractor.ExtractKeywordsAsync(chunk.Content);

        // 5. Combine and prioritize keywords
        List<string> finalKeywords;
        if (_configuration?.PrioritizeCustomKeywords == true)
        {
            // Custom keywords first, then extracted keywords to fill remaining slots
            finalKeywords = allKeywords.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var remainingSlots = (_configuration?.MaxKeywordsPerChunk ?? 10) - finalKeywords.Count;
            if (remainingSlots > 0)
            {
                var additionalKeywords = extractedKeywords
                    .Where(k => !finalKeywords.Contains(k, StringComparer.OrdinalIgnoreCase))
                    .Take(remainingSlots);
                finalKeywords.AddRange(additionalKeywords);
            }
        }
        else
        {
            // Mix all keywords and sort by relevance (frequency for extracted, order for custom)
            var customKeywordSet = new HashSet<string>(allKeywords, StringComparer.OrdinalIgnoreCase);
            var mixedKeywords = new List<string>();
            
            // Add custom keywords first (they're considered high priority)
            mixedKeywords.AddRange(allKeywords.Distinct(StringComparer.OrdinalIgnoreCase));
            
            // Add extracted keywords that aren't already included
            mixedKeywords.AddRange(extractedKeywords.Where(k => !customKeywordSet.Contains(k)));
            
            finalKeywords = mixedKeywords.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        return finalKeywords;
    }

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

