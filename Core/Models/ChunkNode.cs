namespace MarkdownStructureChunker.Core.Models;

/// <summary>
/// Represents a single structured chunk of a document with comprehensive metadata.
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
    /// This property is maintained for backward compatibility.
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

    // NEW PROPERTIES REQUESTED BY CUSTOMERS

    /// <summary>
    /// Gets or sets the original markdown content for this chunk.
    /// Contains the raw markdown formatting before processing.
    /// </summary>
    public string OriginalMarkdown { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the starting character offset of this chunk in the original document.
    /// </summary>
    public int StartOffset { get; init; }

    /// <summary>
    /// Gets or sets the ending character offset of this chunk in the original document.
    /// </summary>
    public int EndOffset { get; init; }

    /// <summary>
    /// Gets or sets the full heading hierarchy path from root to this chunk.
    /// For example: ["Chapter 1", "Section 1.1", "Subsection 1.1.1"]
    /// </summary>
    public IEnumerable<string> HeadingHierarchy { get; init; } = new List<string>();

    /// <summary>
    /// Gets or sets the section level of this chunk in the document structure.
    /// This may differ from Level for complex document structures.
    /// </summary>
    public int SectionLevel { get; init; }

    /// <summary>
    /// Gets or sets whether this chunk represents a heading/title rather than content.
    /// </summary>
    public bool IsHeading { get; init; }

    /// <summary>
    /// Gets or sets the title of the immediate parent heading.
    /// </summary>
    public string ParentHeading { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the type classification of this chunk using the ChunkType enumeration.
    /// </summary>
    public ChunkType? ChunkTypeEnum { get; init; }

    /// <summary>
    /// Gets or sets a reference to the parent ChunkNode for navigation.
    /// This enables traversing the document hierarchy.
    /// </summary>
    public ChunkNode? Parent { get; init; }

    /// <summary>
    /// Gets or sets the child chunks of this node.
    /// This enables traversing the document hierarchy downward.
    /// </summary>
    public IReadOnlyList<ChunkNode> Children { get; init; } = new List<ChunkNode>();

    /// <summary>
    /// Gets or sets additional metadata properties for extensibility.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets the length of the content in characters.
    /// </summary>
    public int ContentLength => Content?.Length ?? 0;

    /// <summary>
    /// Gets the length of the chunk including both title and content.
    /// </summary>
    public int TotalLength => (CleanTitle?.Length ?? 0) + ContentLength;

    /// <summary>
    /// Gets whether this chunk has any child chunks.
    /// </summary>
    public bool HasChildren => Children.Any();

    /// <summary>
    /// Gets whether this chunk is a root-level chunk (no parent).
    /// </summary>
    public bool IsRoot => ParentId == null && Parent == null;

    /// <summary>
    /// Gets the full path of this chunk as a concatenated string.
    /// </summary>
    public string FullPath => string.Join(" > ", HeadingHierarchy);

    /// <summary>
    /// Creates a copy of this ChunkNode with updated properties.
    /// This method helps with immutable updates while preserving the record semantics.
    /// </summary>
    /// <param name="content">New content (optional)</param>
    /// <param name="keywords">New keywords (optional)</param>
    /// <param name="parent">New parent reference (optional)</param>
    /// <param name="children">New children list (optional)</param>
    /// <returns>A new ChunkNode with updated properties</returns>
    public ChunkNode WithUpdates(
        string? content = null,
        IReadOnlyList<string>? keywords = null,
        ChunkNode? parent = null,
        IReadOnlyList<ChunkNode>? children = null)
    {
        return this with
        {
            Content = content ?? Content,
            Keywords = keywords ?? Keywords,
            Parent = parent ?? Parent,
            Children = children ?? Children
        };
    }

    /// <summary>
    /// Gets all descendant chunks (children, grandchildren, etc.) in a flat list.
    /// </summary>
    /// <returns>An enumerable of all descendant ChunkNode objects</returns>
    public IEnumerable<ChunkNode> GetAllDescendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetAllDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Gets all ancestor chunks (parent, grandparent, etc.) in order from immediate parent to root.
    /// </summary>
    /// <returns>An enumerable of ancestor ChunkNode objects</returns>
    public IEnumerable<ChunkNode> GetAncestors()
    {
        var current = Parent;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    /// <summary>
    /// Finds a child chunk by its ID.
    /// </summary>
    /// <param name="id">The ID to search for</param>
    /// <returns>The child chunk if found, null otherwise</returns>
    public ChunkNode? FindChild(Guid id)
    {
        return Children.FirstOrDefault(c => c.Id == id);
    }

    /// <summary>
    /// Finds a descendant chunk by its ID (searches recursively).
    /// </summary>
    /// <param name="id">The ID to search for</param>
    /// <returns>The descendant chunk if found, null otherwise</returns>
    public ChunkNode? FindDescendant(Guid id)
    {
        return GetAllDescendants().FirstOrDefault(c => c.Id == id);
    }
}

