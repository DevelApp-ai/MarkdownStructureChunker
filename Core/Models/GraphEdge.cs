namespace MarkdownStructureChunker.Core.Models;

/// <summary>
/// Represents a relationship between structural elements in the document graph.
/// This enables the structure-first approach by capturing hierarchical relationships from the AST.
/// </summary>
public record GraphEdge
{
    /// <summary>
    /// Gets or sets the unique identifier for this edge.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Gets or sets the identifier of the source structural element.
    /// </summary>
    public Guid SourceElementId { get; init; }
    
    /// <summary>
    /// Gets or sets the identifier of the target structural element.
    /// </summary>
    public Guid TargetElementId { get; init; }
    
    /// <summary>
    /// Gets or sets the type of relationship between the elements.
    /// Common types: 'HAS_SUBSECTION', 'CONTAINS', 'FOLLOWS', 'PRECEDES', 'LINKS_TO'
    /// </summary>
    public string RelationshipType { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the weight or strength of this relationship (optional).
    /// </summary>
    public double Weight { get; init; } = 1.0;
    
    /// <summary>
    /// Gets or sets additional metadata for this relationship.
    /// For links, this might include the URL, link text, etc.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Gets or sets when this edge was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Common relationship types for structural graph edges.
/// </summary>
public static class RelationshipTypes
{
    /// <summary>
    /// Indicates a direct parent-child hierarchical relationship (e.g., h1 -> h2).
    /// Only used when the heading levels are sequential (h1->h2, h2->h3, etc.)
    /// </summary>
    public const string HAS_SUBSECTION = "HAS_SUBSECTION";
    
    /// <summary>
    /// Indicates containment relationship (e.g., section contains paragraph).
    /// </summary>
    public const string CONTAINS = "CONTAINS";
    
    /// <summary>
    /// Indicates sequential relationship (e.g., paragraph A follows paragraph B).
    /// </summary>
    public const string FOLLOWS = "FOLLOWS";
    
    /// <summary>
    /// Indicates reverse sequential relationship (e.g., paragraph A precedes paragraph B).
    /// </summary>
    public const string PRECEDES = "PRECEDES";
    
    /// <summary>
    /// Indicates sibling relationship at the same hierarchical level.
    /// </summary>
    public const string SIBLING = "SIBLING";
    
    /// <summary>
    /// Indicates parent relationship (inverse of HAS_SUBSECTION).
    /// </summary>
    public const string PARENT_OF = "PARENT_OF";
    
    /// <summary>
    /// Indicates a document link relationship (e.g., element links to another document).
    /// Metadata should contain link details like URL, link text, etc.
    /// </summary>
    public const string LINKS_TO = "LINKS_TO";
    
    /// <summary>
    /// Indicates a non-sequential hierarchical relationship (e.g., h1 -> h3 without h2).
    /// Used when heading levels skip intermediate levels.
    /// </summary>
    public const string HAS_NESTED_SECTION = "HAS_NESTED_SECTION";
}