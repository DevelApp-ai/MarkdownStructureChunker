namespace MarkdownStructureChunker.Core.Models;

/// <summary>
/// Represents the entire document as a graph of nodes.
/// </summary>
public record DocumentGraph
{
    /// <summary>
    /// Gets or sets the unique identifier for the source document.
    /// </summary>
    public string SourceId { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the collection of structured chunks that make up the document.
    /// </summary>
    public IReadOnlyList<ChunkNode> Chunks { get; init; } = new List<ChunkNode>();
}

