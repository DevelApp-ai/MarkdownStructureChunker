using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdownStructureChunker.Core.Interfaces;
using MarkdownStructureChunker.Core.Models;

namespace MarkdownStructureChunker.Core.Strategies;

/// <summary>
/// AST-based chunking strategy that uses Markdig to parse Markdown documents into their Abstract Syntax Tree (AST).
/// This strategy implements the structure-first ingestion architecture by creating structural elements and relationships.
/// </summary>
public class ASTBasedStrategy : IChunkingStrategy
{
    private readonly MarkdownPipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the ASTBasedStrategy with a default Markdig pipeline.
    /// </summary>
    public ASTBasedStrategy()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <summary>
    /// Initializes a new instance of the ASTBasedStrategy with a custom Markdig pipeline.
    /// </summary>
    /// <param name="pipeline">The Markdig pipeline to use for parsing</param>
    public ASTBasedStrategy(MarkdownPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    /// <summary>
    /// Processes the input text using Markdig AST parsing and returns structured chunks.
    /// This method maintains compatibility with the existing IChunkingStrategy interface
    /// while internally building structural elements.
    /// </summary>
    /// <param name="text">The input document text</param>
    /// <param name="sourceId">Identifier for the source document</param>
    /// <returns>A list of chunk nodes representing the document structure</returns>
    public IReadOnlyList<ChunkNode> ProcessText(string text, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<ChunkNode>();

        var document = Markdown.Parse(text, _pipeline);
        var chunks = new List<ChunkNode>();
        var chunkId = 0;

        ProcessBlock(document, text, sourceId, chunks, ref chunkId, null, 0);

        return chunks;
    }

    /// <summary>
    /// Processes the input text and returns both structural elements and chunks.
    /// This method provides the full structure-first architecture capabilities.
    /// </summary>
    /// <param name="text">The input document text</param>
    /// <param name="sourceId">Identifier for the source document</param>
    /// <returns>A tuple containing structural elements, edges, and traditional chunks</returns>
    public (IReadOnlyList<StructuralElement> Elements, IReadOnlyList<GraphEdge> Edges, IReadOnlyList<ChunkNode> Chunks) ProcessTextToStructure(string text, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (new List<StructuralElement>(), new List<GraphEdge>(), new List<ChunkNode>());

        var document = Markdown.Parse(text, _pipeline);
        var elements = new List<StructuralElement>();
        var edges = new List<GraphEdge>();
        var chunks = new List<ChunkNode>();
        var chunkId = 0;

        // Process blocks to create structural elements and edges
        ProcessBlockToStructure(document, text, sourceId, elements, edges, null);

        // Also create traditional chunks for backward compatibility
        ProcessBlock(document, text, sourceId, chunks, ref chunkId, null, 0);

        return (elements, edges, chunks);
    }

    /// <summary>
    /// Recursively processes Markdig blocks to create traditional ChunkNode objects.
    /// </summary>
    private void ProcessBlock(Block block, string originalText, string sourceId, List<ChunkNode> chunks, ref int chunkId, Guid? parentId, int level)
    {
        switch (block)
        {
            case HeadingBlock heading:
                ProcessHeading(heading, originalText, sourceId, chunks, ref chunkId, parentId, level);
                break;

            case ParagraphBlock paragraph:
                ProcessParagraph(paragraph, originalText, sourceId, chunks, ref chunkId, parentId, level);
                break;

            case ListBlock list:
                ProcessList(list, originalText, sourceId, chunks, ref chunkId, parentId, level);
                break;

            case CodeBlock code:
                ProcessCodeBlock(code, originalText, sourceId, chunks, ref chunkId, parentId, level);
                break;

            case ContainerBlock container:
                // Process all child blocks
                foreach (var childBlock in container)
                {
                    ProcessBlock(childBlock, originalText, sourceId, chunks, ref chunkId, parentId, level);
                }
                break;
        }
    }

    /// <summary>
    /// Recursively processes Markdig blocks to create StructuralElement objects and relationships.
    /// </summary>
    private void ProcessBlockToStructure(Block block, string originalText, string sourceId, List<StructuralElement> elements, List<GraphEdge> edges, StructuralElement? parent)
    {
        if (block is not ContainerBlock containerBlock)
        {
            // Process single block
            var element = CreateStructuralElementFromBlock(block, originalText, sourceId);
            if (element != null)
            {
                elements.Add(element);
                
                if (parent != null)
                {
                    var relationshipType = DetermineRelationshipType(parent, element);
                    edges.Add(new GraphEdge
                    {
                        SourceElementId = parent.Id,
                        TargetElementId = element.Id,
                        RelationshipType = relationshipType
                    });
                }
            }
            return;
        }

        // For container blocks, process all children and establish more precise hierarchical relationships
        StructuralElement? currentHeading = null;
        var headingStack = new Stack<StructuralElement>();
        if (parent?.ElementType == "heading")
        {
            headingStack.Push(parent);
        }

        foreach (var childBlock in containerBlock)
        {
            var element = CreateStructuralElementFromBlock(childBlock, originalText, sourceId);
            if (element == null) continue;

            elements.Add(element);

            if (element.ElementType == "heading")
            {
                // More precise hierarchical relationship detection
                StructuralElement? parentHeading = null;
                
                // Pop headings from stack until we find one with lower level (higher importance)
                while (headingStack.Count > 0 && headingStack.Peek().Level >= element.Level)
                {
                    headingStack.Pop();
                }
                
                if (headingStack.Count > 0)
                {
                    parentHeading = headingStack.Peek();
                    
                    // Determine the appropriate relationship type based on level difference
                    var relationshipType = DetermineHeadingRelationshipType(parentHeading, element);
                    edges.Add(new GraphEdge
                    {
                        SourceElementId = parentHeading.Id,
                        TargetElementId = element.Id,
                        RelationshipType = relationshipType
                    });
                }

                // Push current heading to stack
                headingStack.Push(element);
                currentHeading = element;
            }
            else
            {
                // Non-heading elements belong to the current heading
                if (currentHeading != null)
                {
                    edges.Add(new GraphEdge
                    {
                        SourceElementId = currentHeading.Id,
                        TargetElementId = element.Id,
                        RelationshipType = RelationshipTypes.CONTAINS
                    });
                }
                else if (parent != null)
                {
                    // No current heading, attach to parent
                    var relationshipType = DetermineRelationshipType(parent, element);
                    edges.Add(new GraphEdge
                    {
                        SourceElementId = parent.Id,
                        TargetElementId = element.Id,
                        RelationshipType = relationshipType
                    });
                }
                
                // Extract and create link relationships if this element contains links
                CreateLinkRelationships(element, elements, edges);
            }
        }
    }

    /// <summary>
    /// Determines the relationship type between two headings based on their levels.
    /// </summary>
    private string DetermineHeadingRelationshipType(StructuralElement parent, StructuralElement child)
    {
        var levelDifference = child.Level - parent.Level;
        
        if (levelDifference == 1)
        {
            // Sequential levels (h1->h2, h2->h3) - direct subsection
            return RelationshipTypes.HAS_SUBSECTION;
        }
        else if (levelDifference > 1)
        {
            // Non-sequential levels (h1->h3, h2->h4) - nested section
            return RelationshipTypes.HAS_NESTED_SECTION;
        }
        else
        {
            // This shouldn't happen due to stack logic, but fallback to sibling
            return RelationshipTypes.SIBLING;
        }
    }

    /// <summary>
    /// Creates link relationships for elements that contain document links.
    /// </summary>
    private void CreateLinkRelationships(StructuralElement element, List<StructuralElement> elements, List<GraphEdge> edges)
    {
        if (element.Links.Any())
        {
            foreach (var link in element.Links.Where(l => l.Type == LinkType.Internal))
            {
                // Create a virtual link element for internal document links
                var linkElement = new StructuralElement
                {
                    ElementType = "link",
                    Content = link.Text,
                    SourceId = element.SourceId,
                    Metadata = new Dictionary<string, object>
                    {
                        ["LinkUrl"] = link.Url,
                        ["LinkType"] = link.Type.ToString(),
                        ["LinkTitle"] = link.Title ?? ""
                    }
                };
                
                elements.Add(linkElement);
                
                edges.Add(new GraphEdge
                {
                    SourceElementId = element.Id,
                    TargetElementId = linkElement.Id,
                    RelationshipType = RelationshipTypes.LINKS_TO,
                    Metadata = new Dictionary<string, object>
                    {
                        ["LinkUrl"] = link.Url,
                        ["LinkText"] = link.Text,
                        ["LinkType"] = link.Type.ToString()
                    }
                });
            }
        }
    }

    /// <summary>
    /// Creates a StructuralElement from any type of Markdig block.
    /// </summary>
    private StructuralElement? CreateStructuralElementFromBlock(Block block, string originalText, string sourceId)
    {
        switch (block)
        {
            case HeadingBlock heading:
                return CreateStructuralElement(heading, originalText, sourceId, "heading");

            case ParagraphBlock paragraph:
                return CreateStructuralElement(paragraph, originalText, sourceId, "paragraph");

            case ListBlock list:
                return CreateStructuralElement(list, originalText, sourceId, "list");

            case CodeBlock code:
                return CreateStructuralElement(code, originalText, sourceId, "code_block");

            default:
                return null;
        }
    }

    /// <summary>
    /// Creates a StructuralElement from a Markdig block.
    /// </summary>
    private StructuralElement CreateStructuralElement(Block block, string originalText, string sourceId, string elementType)
    {
        var content = ExtractTextContent(block);
        var originalMarkdown = ExtractOriginalMarkdown(originalText, block.Span.Start, block.Span.End);
        var level = block is HeadingBlock heading ? heading.Level : 0;
        var links = ExtractLinksFromBlock(block);

        return new StructuralElement
        {
            ElementType = elementType,
            Content = content,
            Level = level,
            StartOffset = block.Span.Start,
            EndOffset = block.Span.End,
            StartLine = block.Line,
            EndLine = block.Line, // Markdig doesn't provide end line directly
            OriginalMarkdown = originalMarkdown,
            SourceId = sourceId,
            Links = links,
            Metadata = new Dictionary<string, object>
            {
                ["BlockType"] = block.GetType().Name,
                ["HasChildren"] = block is ContainerBlock container && container.Any(),
                ["LinkCount"] = links.Count
            }
        };
    }

    /// <summary>
    /// Extracts document links from a Markdig block.
    /// </summary>
    private List<DocumentLink> ExtractLinksFromBlock(Block block)
    {
        var links = new List<DocumentLink>();
        
        switch (block)
        {
            case HeadingBlock heading:
                links.AddRange(ExtractLinksFromInline(heading.Inline));
                break;

            case ParagraphBlock paragraph:
                links.AddRange(ExtractLinksFromInline(paragraph.Inline));
                break;

            case ListBlock list:
                foreach (var item in list.OfType<ListItemBlock>())
                {
                    foreach (var itemChild in item)
                    {
                        if (itemChild is ParagraphBlock itemParagraph)
                        {
                            links.AddRange(ExtractLinksFromInline(itemParagraph.Inline));
                        }
                    }
                }
                break;
        }
        
        return links;
    }

    /// <summary>
    /// Extracts document links from Markdig inline elements.
    /// </summary>
    private List<DocumentLink> ExtractLinksFromInline(ContainerInline? inline)
    {
        var links = new List<DocumentLink>();
        if (inline == null) return links;

        foreach (var child in inline)
        {
            if (child is LinkInline link)
            {
                var documentLink = new DocumentLink
                {
                    Url = link.Url ?? "",
                    Text = ExtractInlineText(link),
                    Title = link.Title,
                    Type = DetermineLinkType(link.Url ?? "")
                };
                links.Add(documentLink);
            }
            else if (child is ContainerInline container)
            {
                links.AddRange(ExtractLinksFromInline(container));
            }
        }
        
        return links;
    }

    /// <summary>
    /// Determines the type of link based on its URL.
    /// </summary>
    private LinkType DetermineLinkType(string url)
    {
        if (string.IsNullOrEmpty(url))
            return LinkType.Other;
            
        if (url.StartsWith("http://") || url.StartsWith("https://"))
            return LinkType.External;
            
        if (url.StartsWith("mailto:"))
            return LinkType.Email;
            
        if (url.StartsWith("#"))
            return LinkType.Anchor;
            
        if (IsInternalDocumentLink(url))
            return LinkType.Internal;
            
        return LinkType.Other;
    }

    /// <summary>
    /// Determines if the given URL is an internal document link.
    /// </summary>
    private bool IsInternalDocumentLink(string url)
    {
        return url.StartsWith("./") ||
               url.StartsWith("../") ||
               (!url.Contains("://") &&
                (url.EndsWith(".md") || url.EndsWith(".html") || url.Contains("/")));
    }

    /// <summary>
    /// Determines the appropriate relationship type between two structural elements.
    /// </summary>
    private string DetermineRelationshipType(StructuralElement parent, StructuralElement child)
    {
        // If parent is a heading and child is a heading with higher level number (lower importance)
        if (parent.ElementType == "heading" && child.ElementType == "heading" && child.Level > parent.Level)
        {
            return RelationshipTypes.HAS_SUBSECTION;
        }

        // If parent is a heading and child is not a heading
        if (parent.ElementType == "heading" && child.ElementType != "heading")
        {
            return RelationshipTypes.CONTAINS;
        }

        // Default containment relationship
        return RelationshipTypes.CONTAINS;
    }

    /// <summary>
    /// Processes a heading block into a ChunkNode.
    /// </summary>
    private void ProcessHeading(HeadingBlock heading, string originalText, string sourceId, List<ChunkNode> chunks, ref int chunkId, Guid? parentId, int level)
    {
        var headingText = ExtractTextContent(heading);
        var originalMarkdown = ExtractOriginalMarkdown(originalText, heading.Span.Start, heading.Span.End);

        var chunk = new ChunkNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Level = heading.Level,
            ChunkType = "Markdown",
            RawTitle = originalMarkdown,
            CleanTitle = headingText,
            Content = headingText,
            StartOffset = heading.Span.Start,
            EndOffset = heading.Span.End,
            OriginalMarkdown = originalMarkdown,
            IsHeading = true,
            SectionLevel = heading.Level,
            ChunkTypeEnum = Models.ChunkType.Header
        };

        chunks.Add(chunk);
        chunkId++;
    }

    /// <summary>
    /// Processes a paragraph block into a ChunkNode.
    /// </summary>
    private void ProcessParagraph(ParagraphBlock paragraph, string originalText, string sourceId, List<ChunkNode> chunks, ref int chunkId, Guid? parentId, int level)
    {
        var content = ExtractTextContent(paragraph);
        var originalMarkdown = ExtractOriginalMarkdown(originalText, paragraph.Span.Start, paragraph.Span.End);

        var chunk = new ChunkNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Level = level,
            ChunkType = "Paragraph",
            CleanTitle = $"Paragraph {chunkId + 1}",
            Content = content,
            StartOffset = paragraph.Span.Start,
            EndOffset = paragraph.Span.End,
            OriginalMarkdown = originalMarkdown,
            IsHeading = false,
            SectionLevel = level,
            ChunkTypeEnum = Models.ChunkType.Content
        };

        chunks.Add(chunk);
        chunkId++;
    }

    /// <summary>
    /// Processes a list block into a ChunkNode.
    /// </summary>
    private void ProcessList(ListBlock list, string originalText, string sourceId, List<ChunkNode> chunks, ref int chunkId, Guid? parentId, int level)
    {
        var content = ExtractTextContent(list);
        var originalMarkdown = ExtractOriginalMarkdown(originalText, list.Span.Start, list.Span.End);

        var chunk = new ChunkNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Level = level,
            ChunkType = "List",
            CleanTitle = $"List {chunkId + 1}",
            Content = content,
            StartOffset = list.Span.Start,
            EndOffset = list.Span.End,
            OriginalMarkdown = originalMarkdown,
            IsHeading = false,
            SectionLevel = level,
            ChunkTypeEnum = Models.ChunkType.Content
        };

        chunks.Add(chunk);
        chunkId++;
    }

    /// <summary>
    /// Processes a code block into a ChunkNode.
    /// </summary>
    private void ProcessCodeBlock(CodeBlock code, string originalText, string sourceId, List<ChunkNode> chunks, ref int chunkId, Guid? parentId, int level)
    {
        var content = code is FencedCodeBlock fenced ? fenced.Lines.ToString() : ExtractTextContent(code);
        var originalMarkdown = ExtractOriginalMarkdown(originalText, code.Span.Start, code.Span.End);

        var chunk = new ChunkNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Level = level,
            ChunkType = "Code",
            CleanTitle = $"Code Block {chunkId + 1}",
            Content = content,
            StartOffset = code.Span.Start,
            EndOffset = code.Span.End,
            OriginalMarkdown = originalMarkdown,
            IsHeading = false,
            SectionLevel = level,
            ChunkTypeEnum = Models.ChunkType.Content
        };

        chunks.Add(chunk);
        chunkId++;
    }

    /// <summary>
    /// Extracts plain text content from a Markdig block.
    /// </summary>
    private string ExtractTextContent(Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                return ExtractInlineText(heading.Inline);

            case ParagraphBlock paragraph:
                return ExtractInlineText(paragraph.Inline);

            case ListBlock list:
                var listText = new List<string>();
                foreach (var item in list.OfType<ListItemBlock>())
                {
                    foreach (var itemChild in item)
                    {
                        if (itemChild is ParagraphBlock itemParagraph)
                        {
                            listText.Add("• " + ExtractInlineText(itemParagraph.Inline));
                        }
                    }
                }
                return string.Join("\n", listText);

            case FencedCodeBlock fenced:
                return fenced.Lines.ToString();

            case CodeBlock code:
                return code.Lines.ToString();

            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// Extracts plain text from Markdig inline elements.
    /// </summary>
    private string ExtractInlineText(ContainerInline? inline)
    {
        if (inline == null) return string.Empty;

        var text = new List<string>();
        foreach (var child in inline)
        {
            switch (child)
            {
                case LiteralInline literal:
                    text.Add(literal.Content.ToString());
                    break;
                case LineBreakInline:
                    text.Add(" ");
                    break;
                case EmphasisInline emphasis:
                    text.Add(ExtractInlineText(emphasis));
                    break;
                case LinkInline link:
                    text.Add(ExtractInlineText(link));
                    break;
                case CodeInline code:
                    text.Add(code.Content);
                    break;
            }
        }
        return string.Join("", text);
    }

    /// <summary>
    /// Extracts the original markdown content for a given range.
    /// </summary>
    private string ExtractOriginalMarkdown(string originalText, int startOffset, int endOffset)
    {
        if (startOffset < 0 || endOffset > originalText.Length || startOffset >= endOffset)
            return string.Empty;

        // Markdig spans may not include the full line, so we need to adjust to get complete markdown
        // Find the start of the line containing startOffset
        int lineStart = startOffset;
        while (lineStart > 0 && originalText[lineStart - 1] != '\n')
        {
            lineStart--;
        }

        // Find the end of the line containing endOffset
        int lineEnd = endOffset;
        while (lineEnd < originalText.Length && originalText[lineEnd] != '\n')
        {
            lineEnd++;
        }

        return originalText.Substring(lineStart, lineEnd - lineStart);
    }
}