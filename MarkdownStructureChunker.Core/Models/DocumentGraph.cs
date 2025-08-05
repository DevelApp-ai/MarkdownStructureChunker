namespace MarkdownStructureChunker.Core.Models;

/// <summary>
/// Represents the entire document as a graph of nodes.
/// </summary>
public record DocumentGraph
{
    public string SourceId { get; init; } = string.Empty;
    public IReadOnlyList<ChunkNode> Chunks { get; init; } = new List<ChunkNode>();
}

