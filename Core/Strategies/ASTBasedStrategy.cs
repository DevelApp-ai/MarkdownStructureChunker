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

        // For container blocks, process all children and establish hierarchical relationships
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
                // Find the appropriate parent heading based on level
                StructuralElement? parentHeading = null;
                
                // Pop headings from stack until we find one with lower level (higher importance)
                while (headingStack.Count > 0 && headingStack.Peek().Level >= element.Level)
                {
                    headingStack.Pop();
                }
                
                if (headingStack.Count > 0)
                {
                    parentHeading = headingStack.Peek();
                }

                // Create relationship with parent heading
                if (parentHeading != null)
                {
                    edges.Add(new GraphEdge
                    {
                        SourceElementId = parentHeading.Id,
                        TargetElementId = element.Id,
                        RelationshipType = RelationshipTypes.HAS_SUBSECTION
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
            Metadata = new Dictionary<string, object>
            {
                ["BlockType"] = block.GetType().Name,
                ["HasChildren"] = block is ContainerBlock container && container.Any()
            }
        };
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