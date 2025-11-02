namespace MarkdownStructureChunker.Core.Models;

/// <summary>
/// Represents the entire document as a graph of nodes.
/// Enhanced to support structure-first ingestion architecture with AST-derived elements.
/// </summary>
public record DocumentGraph
{
    /// <summary>
    /// Gets or sets the unique identifier for the source document.
    /// </summary>
    public string SourceId { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the collection of structured chunks that make up the document.
    /// This maintains backward compatibility with the original API.
    /// </summary>
    public IReadOnlyList<ChunkNode> Chunks { get; init; } = new List<ChunkNode>();
    
    /// <summary>
    /// Gets or sets the collection of structural elements derived from the document's AST.
    /// These represent the deterministic structural backbone of the document.
    /// </summary>
    public IReadOnlyList<StructuralElement> StructuralElements { get; init; } = new List<StructuralElement>();
    
    /// <summary>
    /// Gets or sets the collection of relationships between structural elements.
    /// These edges capture the hierarchical and sequential relationships from the AST.
    /// </summary>
    public IReadOnlyList<GraphEdge> StructuralEdges { get; init; } = new List<GraphEdge>();
    
    /// <summary>
    /// Gets whether this document graph contains structural information.
    /// </summary>
    public bool HasStructuralGraph => StructuralElements.Any();
    
    /// <summary>
    /// Gets the root structural elements (elements with no parent).
    /// </summary>
    public IEnumerable<StructuralElement> RootElements 
    {
        get
        {
            var childElementIds = StructuralEdges
                .Where(e => e.RelationshipType == RelationshipTypes.HAS_SUBSECTION || e.RelationshipType == RelationshipTypes.CONTAINS)
                .Select(e => e.TargetElementId)
                .ToHashSet();
                
            return StructuralElements.Where(e => !childElementIds.Contains(e.Id));
        }
    }
    
    /// <summary>
    /// Gets the children of a specific structural element.
    /// </summary>
    /// <param name="elementId">The ID of the parent element</param>
    /// <returns>Child elements</returns>
    public IEnumerable<StructuralElement> GetChildElements(Guid elementId)
    {
        var childElementIds = StructuralEdges
            .Where(e => e.SourceElementId == elementId && 
                       (e.RelationshipType == RelationshipTypes.HAS_SUBSECTION || e.RelationshipType == RelationshipTypes.CONTAINS))
            .Select(e => e.TargetElementId)
            .ToHashSet();
            
        return StructuralElements.Where(e => childElementIds.Contains(e.Id));
    }
    
    /// <summary>
    /// Gets the parent of a specific structural element.
    /// </summary>
    /// <param name="elementId">The ID of the child element</param>
    /// <returns>Parent element, or null if not found</returns>
    public StructuralElement? GetParentElement(Guid elementId)
    {
        var parentElementId = StructuralEdges
            .Where(e => e.TargetElementId == elementId && 
                       (e.RelationshipType == RelationshipTypes.HAS_SUBSECTION || e.RelationshipType == RelationshipTypes.CONTAINS))
            .Select(e => e.SourceElementId)
            .FirstOrDefault();
            
        return StructuralElements.FirstOrDefault(e => e.Id == parentElementId);
    }
}

