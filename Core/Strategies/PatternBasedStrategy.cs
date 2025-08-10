using MarkdownStructureChunker.Core.Interfaces;
using MarkdownStructureChunker.Core.Models;
using MarkdownStructureChunker.Core.Configuration;
using System.Text;

namespace MarkdownStructureChunker.Core.Strategies;

/// <summary>
/// Pattern-based chunking strategy that uses configurable regular expressions
/// to identify headings and structure in documents.
/// Enhanced version with ChunkerConfiguration support for size limits and overlap.
/// </summary>
public class PatternBasedStrategy : IChunkingStrategy
{
    private readonly List<ChunkingRule> _rules;
    private readonly ChunkerConfiguration? _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatternBasedStrategy"/> class with the specified chunking rules.
    /// </summary>
    /// <param name="rules">The collection of chunking rules to use for document processing, ordered by priority.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rules"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rules"/> is empty.</exception>
    public PatternBasedStrategy(IEnumerable<ChunkingRule> rules)
    {
        _rules = rules?.OrderBy(r => r.Priority).ToList() ?? throw new ArgumentNullException(nameof(rules));
        
        if (!_rules.Any())
            throw new ArgumentException("At least one chunking rule must be provided", nameof(rules));
        
        _configuration = null; // Legacy constructor without configuration
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PatternBasedStrategy"/> class with chunking rules and configuration.
    /// Enhanced constructor that supports ChunkerConfiguration for size limits and overlap.
    /// </summary>
    /// <param name="rules">The collection of chunking rules to use for document processing, ordered by priority.</param>
    /// <param name="configuration">Configuration settings for chunking behavior.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rules"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rules"/> is empty.</exception>
    public PatternBasedStrategy(IEnumerable<ChunkingRule> rules, ChunkerConfiguration? configuration)
    {
        _rules = rules?.OrderBy(r => r.Priority).ToList() ?? throw new ArgumentNullException(nameof(rules));
        
        if (!_rules.Any())
            throw new ArgumentException("At least one chunking rule must be provided", nameof(rules));
        
        _configuration = configuration;
    }

    /// <summary>
    /// Processes the input text and returns a list of structured chunks.
    /// Enhanced version that respects ChunkerConfiguration settings for size limits and overlap.
    /// Now includes offset calculation and original markdown preservation.
    /// </summary>
    /// <param name="text">The input document text</param>
    /// <param name="sourceId">Identifier for the source document</param>
    /// <returns>A list of chunk nodes representing the document structure</returns>
    public IReadOnlyList<ChunkNode> ProcessText(string text, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<ChunkNode>();

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var chunks = new List<ChunkNode>();
        var contextStack = new Stack<ChunkNode>();
        var currentContent = new StringBuilder();
        
        // Track character positions for offset calculation
        var currentOffset = 0;
        var lineOffsets = new List<int>();
        
        // Calculate line start positions
        lineOffsets.Add(0);
        for (int i = 0; i < lines.Length - 1; i++)
        {
            currentOffset += lines[i].Length + Environment.NewLine.Length;
            lineOffsets.Add(currentOffset);
        }

        // Create a root chunk to handle content before the first heading
        var rootChunk = new ChunkNode
        {
            Level = 0,
            ChunkType = "Root",
            RawTitle = "Document Root",
            CleanTitle = "Document Root",
            Content = string.Empty,
            StartOffset = 0,
            EndOffset = 0
        };
        contextStack.Push(rootChunk);

        var contentStartLine = 0;
        var headingHierarchy = new List<string>();

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var match = TryMatchLine(line);
            
            if (match != null)
            {
                // Finalize current content for the chunk at the top of the stack
                if (currentContent.Length > 0 && contextStack.Count > 0)
                {
                    var currentChunk = contextStack.Peek();
                    var contentToAdd = currentContent.ToString().Trim();
                    
                    if (!string.IsNullOrEmpty(contentToAdd))
                    {
                        // Calculate content offsets
                        var contentStartOffset = lineOffsets[contentStartLine];
                        var contentEndOffset = lineIndex > 0 ? lineOffsets[lineIndex] - Environment.NewLine.Length : lineOffsets[lineIndex];
                        
                        var updatedChunk = currentChunk with 
                        { 
                            Content = string.IsNullOrEmpty(currentChunk.Content) 
                                ? contentToAdd 
                                : currentChunk.Content + "\n\n" + contentToAdd,
                            EndOffset = contentEndOffset,
                            OriginalMarkdown = _configuration?.PreserveOriginalMarkdown == true 
                                ? ExtractOriginalMarkdown(text, currentChunk.StartOffset, contentEndOffset)
                                : null
                        };
                        
                        // Update the chunk in the results if it exists
                        var index = chunks.FindIndex(c => c.Id == currentChunk.Id);
                        if (index >= 0)
                        {
                            chunks[index] = updatedChunk;
                        }
                        
                        // Update the chunk in the stack
                        contextStack.Pop();
                        contextStack.Push(updatedChunk);
                    }
                    
                    currentContent.Clear();
                }

                // Create new chunk from the match with offset information
                var newChunk = CreateChunkFromMatchWithOffsets(match, contextStack, lineOffsets[lineIndex], text, headingHierarchy);
                
                // Update heading hierarchy
                UpdateHeadingHierarchy(headingHierarchy, newChunk);
                
                // Manage the context stack based on hierarchical levels
                AdjustContextStack(contextStack, newChunk.Level);
                
                // Set parent relationship
                var parent = contextStack.Count > 0 ? contextStack.Peek() : null;
                var chunkWithParent = newChunk with { ParentId = parent?.Id };
                
                // Add to results and push to stack
                chunks.Add(chunkWithParent);
                contextStack.Push(chunkWithParent);
                
                // Reset content tracking
                contentStartLine = lineIndex + 1;
            }
            else
            {
                // Accumulate content for the current chunk
                currentContent.AppendLine(line);
            }
        }

        // Finalize any remaining content
        if (currentContent.Length > 0 && contextStack.Count > 0)
        {
            var currentChunk = contextStack.Peek();
            var contentToAdd = currentContent.ToString().Trim();
            
            if (!string.IsNullOrEmpty(contentToAdd))
            {
                var contentEndOffset = text.Length;
                
                var updatedChunk = currentChunk with 
                { 
                    Content = string.IsNullOrEmpty(currentChunk.Content) 
                        ? contentToAdd 
                        : currentChunk.Content + "\n\n" + contentToAdd,
                    EndOffset = contentEndOffset,
                    OriginalMarkdown = _configuration?.PreserveOriginalMarkdown == true 
                        ? ExtractOriginalMarkdown(text, currentChunk.StartOffset, contentEndOffset)
                        : null
                };
                
                // Update the chunk in the results if it exists
                var index = chunks.FindIndex(c => c.Id == currentChunk.Id);
                if (index >= 0)
                {
                    chunks[index] = updatedChunk;
                }
            }
        }

        // Remove the root chunk if it has no content and get actual document chunks
        var documentChunks = chunks.Where(c => c.ChunkType != "Root").ToList();
        
        // Apply configuration-based post-processing
        if (_configuration != null)
        {
            documentChunks = ApplyConfigurationConstraints(documentChunks);
        }

        // Build parent-child relationships
        documentChunks = BuildParentChildRelationships(documentChunks);

        return documentChunks;
    }

    /// <summary>
    /// Attempts to match a line against all configured rules.
    /// </summary>
    /// <param name="line">The line to test</param>
    /// <returns>The first successful match, or null if no rules match</returns>
    private ChunkingMatch? TryMatchLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        foreach (var rule in _rules)
        {
            var match = rule.TryMatch(line);
            if (match != null)
                return match;
        }

        return null;
    }

    /// <summary>
    /// Creates a ChunkNode from a successful pattern match.
    /// </summary>
    /// <param name="match">The pattern match result</param>
    /// <param name="contextStack">Current context stack for generating unique IDs</param>
    /// <returns>A new ChunkNode</returns>
    private static ChunkNode CreateChunkFromMatch(ChunkingMatch match, Stack<ChunkNode> contextStack)
    {
        return new ChunkNode
        {
            Id = Guid.NewGuid(),
            Level = match.Level,
            ChunkType = match.Type,
            RawTitle = match.RawTitle,
            CleanTitle = match.CleanTitle,
            Content = string.Empty
        };
    }

    /// <summary>
    /// Creates a chunk from a match with offset information and heading hierarchy.
    /// Enhanced version that includes StartOffset, EndOffset, HeadingHierarchy, and other metadata.
    /// </summary>
    /// <param name="match">The chunking match</param>
    /// <param name="contextStack">The current context stack</param>
    /// <param name="startOffset">The character offset where this chunk starts</param>
    /// <param name="originalText">The original document text</param>
    /// <param name="headingHierarchy">The current heading hierarchy</param>
    /// <returns>A new chunk node with complete metadata</returns>
    private ChunkNode CreateChunkFromMatchWithOffsets(ChunkingMatch match, Stack<ChunkNode> contextStack, 
        int startOffset, string originalText, List<string> headingHierarchy)
    {
        // Determine if this is a heading chunk
        var isHeading = match.Type?.Contains("Heading") == true || 
                       match.Type?.Contains("Markdown") == true ||
                       !string.IsNullOrEmpty(match.RawTitle);

        // Get parent heading for context
        var parentHeading = contextStack.Count > 0 && contextStack.Peek().Level < match.Level && contextStack.Peek().ChunkType != "Root"
            ? contextStack.Peek().CleanTitle 
            : null;

        // Determine chunk type enum
        var chunkTypeEnum = DetermineChunkType(match, isHeading);

        return new ChunkNode
        {
            Id = Guid.NewGuid(),
            Level = match.Level,
            ChunkType = match.Type,
            RawTitle = match.RawTitle,
            CleanTitle = match.CleanTitle,
            Content = string.Empty,
            StartOffset = startOffset,
            EndOffset = startOffset, // Will be updated when content is added
            HeadingHierarchy = new List<string>(headingHierarchy),
            SectionLevel = match.Level,
            IsHeading = isHeading,
            ParentHeading = parentHeading,
            ChunkTypeEnum = chunkTypeEnum,
            OriginalMarkdown = _configuration?.PreserveOriginalMarkdown == true 
                ? ExtractOriginalMarkdown(originalText, startOffset, startOffset + match.RawTitle?.Length ?? 0)
                : null
        };
    }

    /// <summary>
    /// Updates the heading hierarchy based on the new chunk.
    /// </summary>
    /// <param name="headingHierarchy">The current heading hierarchy to update</param>
    /// <param name="newChunk">The new chunk being added</param>
    private static void UpdateHeadingHierarchy(List<string> headingHierarchy, ChunkNode newChunk)
    {
        if (!newChunk.IsHeading || string.IsNullOrEmpty(newChunk.CleanTitle))
            return;

        // Adjust hierarchy based on level - remove deeper levels
        while (headingHierarchy.Count >= newChunk.Level)
        {
            headingHierarchy.RemoveAt(headingHierarchy.Count - 1);
        }

        // Add the new heading at the correct level
        headingHierarchy.Add(newChunk.CleanTitle);
    }

    /// <summary>
    /// Determines the ChunkType enumeration value based on the match and context.
    /// </summary>
    /// <param name="match">The chunking match</param>
    /// <param name="isHeading">Whether this chunk represents a heading</param>
    /// <returns>The appropriate ChunkType enum value</returns>
    private static ChunkType DetermineChunkType(ChunkingMatch match, bool isHeading)
    {
        if (isHeading)
        {
            return ChunkType.Header;
        }

        return match.Type?.ToLowerInvariant() switch
        {
            var t when t?.Contains("list") == true => ChunkType.ListItem,
            var t when t?.Contains("table") == true => ChunkType.Table,
            var t when t?.Contains("code") == true => ChunkType.CodeBlock,
            var t when t?.Contains("quote") == true => ChunkType.Quote,
            var t when t?.Contains("legal") == true => ChunkType.Legal,
            var t when t?.Contains("numeric") == true => ChunkType.Numeric,
            var t when t?.Contains("section") == true => ChunkType.Section,
            var t when t?.Contains("appendix") == true => ChunkType.Appendix,
            _ => ChunkType.Content
        };
    }

    /// <summary>
    /// Extracts the original markdown content for a given range.
    /// </summary>
    /// <param name="originalText">The original document text</param>
    /// <param name="startOffset">The start character offset</param>
    /// <param name="endOffset">The end character offset</param>
    /// <returns>The original markdown content</returns>
    private static string ExtractOriginalMarkdown(string originalText, int startOffset, int endOffset)
    {
        if (string.IsNullOrEmpty(originalText) || startOffset < 0 || endOffset <= startOffset || endOffset > originalText.Length)
            return string.Empty;

        return originalText.Substring(startOffset, endOffset - startOffset);
    }

    /// <summary>
    /// Adjusts the context stack based on the hierarchical level of a new chunk.
    /// Pops chunks from the stack until the correct parent level is found.
    /// </summary>
    /// <param name="contextStack">The current context stack</param>
    /// <param name="newLevel">The level of the new chunk being added</param>
    private static void AdjustContextStack(Stack<ChunkNode> contextStack, int newLevel)
    {
        // Pop chunks from the stack until we find the appropriate parent level
        while (contextStack.Count > 0 && contextStack.Peek().Level >= newLevel)
        {
            contextStack.Pop();
        }
    }

    /// <summary>
    /// Applies configuration constraints to the chunks, including size limits and overlap.
    /// </summary>
    /// <param name="chunks">The original chunks to process</param>
    /// <returns>Processed chunks that respect configuration constraints</returns>
    private List<ChunkNode> ApplyConfigurationConstraints(List<ChunkNode> chunks)
    {
        if (_configuration == null)
            return chunks;

        var processedChunks = new List<ChunkNode>();

        foreach (var chunk in chunks)
        {
            var chunkContent = chunk.Content ?? string.Empty;
            
            // Check if chunk exceeds maximum size
            if (chunkContent.Length > _configuration.MaxChunkSize)
            {
                // Split large chunks while respecting configuration
                var splitChunks = SplitLargeChunk(chunk, chunkContent);
                processedChunks.AddRange(splitChunks);
            }
            else if (chunkContent.Length < _configuration.MinChunkSize && 
                     _configuration.PreserveStructure && 
                     !IsHeadingChunk(chunk))
            {
                // Try to merge small chunks with next chunk if possible
                // For now, keep small chunks as-is to preserve structure
                processedChunks.Add(chunk);
            }
            else
            {
                processedChunks.Add(chunk);
            }
        }

        // Apply overlap if configured
        if (_configuration.ChunkOverlap > 0)
        {
            processedChunks = ApplyChunkOverlap(processedChunks);
        }

        return processedChunks;
    }

    /// <summary>
    /// Splits a large chunk into smaller chunks while respecting configuration settings.
    /// </summary>
    /// <param name="originalChunk">The chunk to split</param>
    /// <param name="content">The content to split</param>
    /// <returns>List of smaller chunks</returns>
    private List<ChunkNode> SplitLargeChunk(ChunkNode originalChunk, string content)
    {
        if (_configuration == null)
            return new List<ChunkNode> { originalChunk };

        var chunks = new List<ChunkNode>();
        var maxSize = _configuration.MaxChunkSize;
        var splitOnSentences = _configuration.SplitOnSentences;
        
        if (splitOnSentences)
        {
            // Split on sentence boundaries
            var sentences = SplitIntoSentences(content);
            var currentChunk = new StringBuilder();
            var chunkIndex = 0;

            foreach (var sentence in sentences)
            {
                if (currentChunk.Length + sentence.Length > maxSize && currentChunk.Length > 0)
                {
                    // Create chunk from current content
                    var newChunk = CreateSplitChunk(originalChunk, currentChunk.ToString().Trim(), chunkIndex);
                    chunks.Add(newChunk);
                    
                    currentChunk.Clear();
                    chunkIndex++;
                }
                
                currentChunk.Append(sentence);
                if (!sentence.EndsWith(" "))
                    currentChunk.Append(" ");
            }

            // Add remaining content
            if (currentChunk.Length > 0)
            {
                var finalChunk = CreateSplitChunk(originalChunk, currentChunk.ToString().Trim(), chunkIndex);
                chunks.Add(finalChunk);
            }
        }
        else
        {
            // Split on word boundaries
            var words = content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = new StringBuilder();
            var chunkIndex = 0;

            foreach (var word in words)
            {
                if (currentChunk.Length + word.Length + 1 > maxSize && currentChunk.Length > 0)
                {
                    // Create chunk from current content
                    var newChunk = CreateSplitChunk(originalChunk, currentChunk.ToString().Trim(), chunkIndex);
                    chunks.Add(newChunk);
                    
                    currentChunk.Clear();
                    chunkIndex++;
                }
                
                if (currentChunk.Length > 0)
                    currentChunk.Append(" ");
                currentChunk.Append(word);
            }

            // Add remaining content
            if (currentChunk.Length > 0)
            {
                var finalChunk = CreateSplitChunk(originalChunk, currentChunk.ToString().Trim(), chunkIndex);
                chunks.Add(finalChunk);
            }
        }

        return chunks.Any() ? chunks : new List<ChunkNode> { originalChunk };
    }

    /// <summary>
    /// Creates a split chunk from the original chunk with new content.
    /// </summary>
    /// <param name="originalChunk">The original chunk</param>
    /// <param name="content">The new content</param>
    /// <param name="index">The split index</param>
    /// <returns>A new chunk node</returns>
    private static ChunkNode CreateSplitChunk(ChunkNode originalChunk, string content, int index)
    {
        var suffix = index > 0 ? $" (Part {index + 1})" : "";
        
        return originalChunk with
        {
            Id = Guid.NewGuid(),
            Content = content,
            RawTitle = originalChunk.RawTitle + suffix,
            CleanTitle = originalChunk.CleanTitle + suffix
        };
    }

    /// <summary>
    /// Splits content into sentences for sentence-boundary splitting.
    /// </summary>
    /// <param name="content">The content to split</param>
    /// <returns>List of sentences</returns>
    private static List<string> SplitIntoSentences(string content)
    {
        // Simple sentence splitting - could be enhanced with more sophisticated NLP
        var sentences = new List<string>();
        var sentenceEnders = new[] { '.', '!', '?' };
        var currentSentence = new StringBuilder();

        for (int i = 0; i < content.Length; i++)
        {
            var ch = content[i];
            currentSentence.Append(ch);

            if (sentenceEnders.Contains(ch))
            {
                // Check if this is really the end of a sentence
                if (i == content.Length - 1 || 
                    (i < content.Length - 1 && char.IsWhiteSpace(content[i + 1])))
                {
                    sentences.Add(currentSentence.ToString());
                    currentSentence.Clear();
                }
            }
        }

        // Add any remaining content
        if (currentSentence.Length > 0)
        {
            sentences.Add(currentSentence.ToString());
        }

        return sentences.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }

    /// <summary>
    /// Applies chunk overlap by adding overlapping content between adjacent chunks.
    /// </summary>
    /// <param name="chunks">The chunks to process</param>
    /// <returns>Chunks with overlap applied</returns>
    private List<ChunkNode> ApplyChunkOverlap(List<ChunkNode> chunks)
    {
        if (_configuration == null || _configuration.ChunkOverlap <= 0 || chunks.Count <= 1)
            return chunks;

        var overlappedChunks = new List<ChunkNode>();
        
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var content = chunk.Content ?? string.Empty;

            // Add overlap from previous chunk
            if (i > 0)
            {
                var previousChunk = chunks[i - 1];
                var previousContent = previousChunk.Content ?? string.Empty;
                
                if (previousContent.Length > _configuration.ChunkOverlap)
                {
                    var overlapContent = previousContent.Substring(previousContent.Length - _configuration.ChunkOverlap);
                    content = overlapContent + "\n\n" + content;
                }
            }

            var updatedChunk = chunk with { Content = content };
            overlappedChunks.Add(updatedChunk);
        }

        return overlappedChunks;
    }

    /// <summary>
    /// Determines if a chunk represents a heading.
    /// </summary>
    /// <param name="chunk">The chunk to check</param>
    /// <returns>True if the chunk is a heading</returns>
    private static bool IsHeadingChunk(ChunkNode chunk)
    {
        return chunk.ChunkType?.Contains("Heading") == true ||
               chunk.ChunkType?.Contains("Header") == true ||
               !string.IsNullOrEmpty(chunk.RawTitle);
    }

    /// <summary>
    /// Builds parent-child relationships between chunks based on their hierarchical structure.
    /// </summary>
    /// <param name="chunks">The chunks to process</param>
    /// <returns>Chunks with populated Parent and Children properties</returns>
    private static List<ChunkNode> BuildParentChildRelationships(List<ChunkNode> chunks)
    {
        if (!chunks.Any())
            return chunks;

        var chunkMap = new Dictionary<Guid, ChunkNode>();
        var childrenMap = new Dictionary<Guid, List<ChunkNode>>();
        var updatedChunks = new List<ChunkNode>();

        // First pass: Create a map of all chunks and initialize children collections
        foreach (var chunk in chunks)
        {
            chunkMap[chunk.Id] = chunk;
            childrenMap[chunk.Id] = new List<ChunkNode>();
        }

        // Second pass: Build parent-child relationships
        foreach (var chunk in chunks)
        {
            ChunkNode? parentChunk = null;
            List<ChunkNode> children = new List<ChunkNode>();

            // Find parent based on ParentId
            if (chunk.ParentId.HasValue && chunkMap.TryGetValue(chunk.ParentId.Value, out var parent))
            {
                parentChunk = parent;
                childrenMap[chunk.ParentId.Value].Add(chunk);
            }

            // Find children based on ParentId references
            children = chunks.Where(c => c.ParentId == chunk.Id).ToList();

            // Create updated chunk with parent and children references
            var updatedChunk = chunk with
            {
                Parent = parentChunk,
                Children = children.AsReadOnly()
            };

            updatedChunks.Add(updatedChunk);
        }

        // Third pass: Update all chunks with their final children collections
        var finalChunks = new List<ChunkNode>();
        foreach (var chunk in updatedChunks)
        {
            var finalChildren = updatedChunks.Where(c => c.ParentId == chunk.Id).ToList();
            var finalChunk = chunk with
            {
                Children = finalChildren.AsReadOnly()
            };
            finalChunks.Add(finalChunk);
        }

        return finalChunks;
    }

    /// <summary>
    /// Creates default chunking rules for common document patterns.
    /// </summary>
    /// <returns>A collection of default chunking rules</returns>
    public static IEnumerable<ChunkingRule> CreateDefaultRules()
    {
        return new[]
        {
            // Markdown headings (highest priority)
            new ChunkingRule("MarkdownH1", @"^#\s+(.*)", level: 1, priority: 1),
            new ChunkingRule("MarkdownH2", @"^##\s+(.*)", level: 2, priority: 2),
            new ChunkingRule("MarkdownH3", @"^###\s+(.*)", level: 3, priority: 3),
            new ChunkingRule("MarkdownH4", @"^####\s+(.*)", level: 4, priority: 4),
            new ChunkingRule("MarkdownH5", @"^#####\s+(.*)", level: 5, priority: 5),
            new ChunkingRule("MarkdownH6", @"^######\s+(.*)", level: 6, priority: 6),
            
            // Numeric outlines (dynamic level calculation)
            new ChunkingRule("Numeric", @"^(\d+(?:\.\d+)*\.?)\s+(.*)", priority: 10),
            
            // Legal section references
            new ChunkingRule("Legal", @"^(§\s*\d+)\s+(.*)", priority: 20),
            
            // Appendices
            new ChunkingRule("Appendix", @"^Appendix\s+([A-Z])[\.:\-\s]+(.*)", priority: 30),
            
            // Roman numerals
            new ChunkingRule("Roman", @"^([IVX]+)\.\s+(.*)", priority: 40),
            
            // Lettered sections
            new ChunkingRule("Letter", @"^([A-Z])\.\s+(.*)", priority: 50)
        };
    }
}

