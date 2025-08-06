namespace MarkdownStructureChunker.Core.Models;

/// <summary>
/// Represents a single structured chunk of a document.
/// </summary>
public record ChunkNode
{
    /// <summary>
    /// Gets or sets the unique identifier for this chunk.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Gets or sets the unique identifier of the parent chunk, or null if this is a root-level chunk.
    /// </summary>
    public Guid? ParentId { get; init; }
    
    /// <summary>
    /// Gets or sets the hierarchical level of this chunk (1 = top level, 2 = second level, etc.).
    /// </summary>
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

