using MarkdownStructureChunker.Core.Interfaces;
using MarkdownStructureChunker.Core.Models;

namespace MarkdownStructureChunker.Core;

/// <summary>
/// The main entry point for the MarkdownStructureChunker library.
/// Orchestrates the parsing, chunking, and keyword extraction processes.
/// </summary>
public class StructureChunker
{
    private readonly IChunkingStrategy _chunkingStrategy;
    private readonly IKeywordExtractor _keywordExtractor;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructureChunker"/> class with the specified strategy and extractor.
    /// </summary>
    /// <param name="chunkingStrategy">The chunking strategy to use for document processing.</param>
    /// <param name="keywordExtractor">The keyword extractor to use for content analysis.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="chunkingStrategy"/> or <paramref name="keywordExtractor"/> is null.</exception>
    public StructureChunker(IChunkingStrategy chunkingStrategy, IKeywordExtractor keywordExtractor)
    {
        _chunkingStrategy = chunkingStrategy ?? throw new ArgumentNullException(nameof(chunkingStrategy));
        _keywordExtractor = keywordExtractor ?? throw new ArgumentNullException(nameof(keywordExtractor));
    }

    /// <summary>
    /// Processes a document and returns a structured graph of chunks with extracted keywords.
    /// </summary>
    /// <param name="documentText">The input document text</param>
    /// <param name="sourceId">Identifier for the source document</param>
    /// <returns>A DocumentGraph containing all processed chunks</returns>
    public async Task<DocumentGraph> ProcessAsync(string documentText, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(documentText))
            throw new ArgumentException("Document text cannot be null or empty", nameof(documentText));

        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Source ID cannot be null or empty", nameof(sourceId));

        // Step 1: Parse and chunk the document
        var chunks = _chunkingStrategy.ProcessText(documentText, sourceId);

        // Step 2: Extract keywords for each chunk
        var enrichedChunks = new List<ChunkNode>();
        foreach (var chunk in chunks)
        {
            var keywords = await _keywordExtractor.ExtractKeywordsAsync(chunk.Content);
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
    /// Synchronous version of ProcessAsync for backward compatibility.
    /// </summary>
    /// <param name="documentText">The input document text</param>
    /// <param name="sourceId">Identifier for the source document</param>
    /// <returns>A DocumentGraph containing all processed chunks</returns>
    public DocumentGraph Process(string documentText, string sourceId)
    {
        return ProcessAsync(documentText, sourceId).GetAwaiter().GetResult();
    }
}

