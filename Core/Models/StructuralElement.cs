namespace MarkdownStructureChunker.Core.Models;

/// <summary>
/// Represents a structural element in the document graph that corresponds to AST nodes.
/// This serves as the foundation for the structure-first ingestion architecture.
/// </summary>
public record StructuralElement
{
    /// <summary>
    /// Gets or sets the unique identifier for this structural element.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Gets or sets the type of structural element (e.g., 'heading', 'paragraph', 'list', 'code_block').
    /// </summary>
    public string ElementType { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the raw text content of this structural element.
    /// </summary>
    public string Content { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the hierarchical level of this element (for headings).
    /// </summary>
    public int Level { get; init; }
    
    /// <summary>
    /// Gets or sets the character offset where this element starts in the original document.
    /// </summary>
    public int StartOffset { get; init; }
    
    /// <summary>
    /// Gets or sets the character offset where this element ends in the original document.
    /// </summary>
    public int EndOffset { get; init; }
    
    /// <summary>
    /// Gets or sets the line number where this element starts.
    /// </summary>
    public int StartLine { get; init; }
    
    /// <summary>
    /// Gets or sets the line number where this element ends.
    /// </summary>
    public int EndLine { get; init; }
    
    /// <summary>
    /// Gets or sets additional metadata specific to this element type.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Gets or sets the original markdown formatting for this element.
    /// </summary>
    public string OriginalMarkdown { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the source document identifier.
    /// </summary>
    public string SourceId { get; init; } = string.Empty;
}