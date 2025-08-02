namespace MarkdownStructureChunker.Core.Models;

/// <summary>
/// Represents a single structured chunk of a document.
/// </summary>
public record ChunkNode
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ParentId { get; init; }
    public int Level { get; init; }
    
    /// <summary>
    /// The type of heading used to create this chunk (e.g., "Markdown", "Numeric", "Legal").
    /// </summary>
    public string ChunkType { get; init; } = string.Empty;
    
    /// <summary>
    /// The original, unprocessed heading text (e.g., "1.2.4" or "## My Title").
    /// </summary>
    public string RawTitle { get; init; } = string.Empty;

    /// <summary>
    /// The cleaned-up title of the chunk.
    /// </summary>
    public string? CleanTitle { get; init; }
    
    /// <summary>
    /// The concatenated text content under this heading.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Keywords extracted by the ML.NET pipeline.
    /// </summary>
    public IReadOnlyList<string> Keywords { get; init; } = new List<string>();
}

