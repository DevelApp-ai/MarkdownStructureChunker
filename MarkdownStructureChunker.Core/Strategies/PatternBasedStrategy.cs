using MarkdownStructureChunker.Core.Interfaces;
using MarkdownStructureChunker.Core.Models;
using System.Text;

namespace MarkdownStructureChunker.Core.Strategies;

/// <summary>
/// Pattern-based chunking strategy that uses configurable regular expressions
/// to identify headings and structure in documents.
/// </summary>
public class PatternBasedStrategy : IChunkingStrategy
{
    private readonly List<ChunkingRule> _rules;

    public PatternBasedStrategy(IEnumerable<ChunkingRule> rules)
    {
        _rules = rules?.OrderBy(r => r.Priority).ToList() ?? throw new ArgumentNullException(nameof(rules));
        
        if (!_rules.Any())
            throw new ArgumentException("At least one chunking rule must be provided", nameof(rules));
    }

    /// <summary>
    /// Processes the input text and returns a list of structured chunks.
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

        // Create a root chunk to handle content before the first heading
        var rootChunk = new ChunkNode
        {
            Level = 0,
            ChunkType = "Root",
            RawTitle = "Document Root",
            CleanTitle = "Document Root",
            Content = string.Empty
        };
        contextStack.Push(rootChunk);

        foreach (var line in lines)
        {
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
                        var updatedChunk = currentChunk with 
                        { 
                            Content = string.IsNullOrEmpty(currentChunk.Content) 
                                ? contentToAdd 
                                : currentChunk.Content + "\n\n" + contentToAdd
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

                // Create new chunk from the match
                var newChunk = CreateChunkFromMatch(match, contextStack);
                
                // Manage the context stack based on hierarchical levels
                AdjustContextStack(contextStack, newChunk.Level);
                
                // Set parent relationship
                var parent = contextStack.Count > 0 ? contextStack.Peek() : null;
                var chunkWithParent = newChunk with { ParentId = parent?.Id };
                
                // Add to results and push to stack
                chunks.Add(chunkWithParent);
                contextStack.Push(chunkWithParent);
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
                var updatedChunk = currentChunk with 
                { 
                    Content = string.IsNullOrEmpty(currentChunk.Content) 
                        ? contentToAdd 
                        : currentChunk.Content + "\n\n" + contentToAdd
                };
                
                // Update the chunk in the results if it exists
                var index = chunks.FindIndex(c => c.Id == currentChunk.Id);
                if (index >= 0)
                {
                    chunks[index] = updatedChunk;
                }
            }
        }

        // Remove the root chunk if it has no content and return only actual document chunks
        return chunks.Where(c => c.ChunkType != "Root").ToList();
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
    /// Creates a default set of chunking rules for common document patterns.
    /// </summary>
    /// <returns>A list of default chunking rules</returns>
    public static List<ChunkingRule> CreateDefaultRules()
    {
        return new List<ChunkingRule>
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

