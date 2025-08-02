using MarkdownStructureChunker.Core.Models;

namespace MarkdownStructureChunker.Core.Interfaces;

/// <summary>
/// Defines the contract for chunking logic.
/// </summary>
public interface IChunkingStrategy
{
    /// <summary>
    /// Processes the input text and returns a list of structured chunks.
    /// </summary>
    /// <param name="text">The input document text</param>
    /// <param name="sourceId">Identifier for the source document</param>
    /// <returns>A list of chunk nodes representing the document structure</returns>
    IReadOnlyList<ChunkNode> ProcessText(string text, string sourceId);
}

