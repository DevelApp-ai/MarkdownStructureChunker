using MarkdownStructureChunker.Core.Models;

namespace MarkdownStructureChunker.Core.Extensions;

/// <summary>
/// Extension methods for ChunkNode to support keyword-based operations.
/// </summary>
public static class ChunkNodeExtensions
{
    /// <summary>
    /// Finds chunks that share keywords with the current chunk.
    /// Useful for cross-document mapping and content similarity.
    /// </summary>
    /// <param name="chunk">The source chunk</param>
    /// <param name="otherChunks">Collection of chunks to search</param>
    /// <param name="minimumSharedKeywords">Minimum number of shared keywords required</param>
    /// <returns>Chunks that share keywords with the source chunk</returns>
    public static IEnumerable<ChunkNode> FindRelatedByKeywords(
        this ChunkNode chunk, 
        IEnumerable<ChunkNode> otherChunks, 
        int minimumSharedKeywords = 1)
    {
        if (chunk.Keywords == null || !chunk.Keywords.Any())
            return Enumerable.Empty<ChunkNode>();

        var sourceKeywords = new HashSet<string>(chunk.Keywords, StringComparer.OrdinalIgnoreCase);

        return otherChunks.Where(other =>
        {
            if (other.Id == chunk.Id || other.Keywords == null)
                return false;

            var sharedCount = other.Keywords.Count(k => sourceKeywords.Contains(k));
            return sharedCount >= minimumSharedKeywords;
        });
    }

    /// <summary>
    /// Calculates keyword similarity score between two chunks.
    /// </summary>
    /// <param name="chunk">The first chunk</param>
    /// <param name="other">The second chunk</param>
    /// <returns>Similarity score between 0.0 and 1.0</returns>
    public static double CalculateKeywordSimilarity(this ChunkNode chunk, ChunkNode other)
    {
        if (chunk.Keywords == null || other.Keywords == null || 
            !chunk.Keywords.Any() || !other.Keywords.Any())
            return 0.0;

        var keywords1 = new HashSet<string>(chunk.Keywords, StringComparer.OrdinalIgnoreCase);
        var keywords2 = new HashSet<string>(other.Keywords, StringComparer.OrdinalIgnoreCase);

        var intersection = keywords1.Intersect(keywords2, StringComparer.OrdinalIgnoreCase).Count();
        var union = keywords1.Union(keywords2, StringComparer.OrdinalIgnoreCase).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    /// <summary>
    /// Checks if the chunk contains any of the specified keywords.
    /// </summary>
    /// <param name="chunk">The chunk to check</param>
    /// <param name="keywords">Keywords to search for</param>
    /// <returns>True if the chunk contains any of the keywords</returns>
    public static bool ContainsAnyKeyword(this ChunkNode chunk, IEnumerable<string> keywords)
    {
        if (chunk.Keywords == null || !chunk.Keywords.Any() || keywords == null)
            return false;

        var chunkKeywords = new HashSet<string>(chunk.Keywords, StringComparer.OrdinalIgnoreCase);
        return keywords.Any(k => chunkKeywords.Contains(k));
    }

    /// <summary>
    /// Checks if the chunk contains all of the specified keywords.
    /// </summary>
    /// <param name="chunk">The chunk to check</param>
    /// <param name="keywords">Keywords to search for</param>
    /// <returns>True if the chunk contains all of the keywords</returns>
    public static bool ContainsAllKeywords(this ChunkNode chunk, IEnumerable<string> keywords)
    {
        if (chunk.Keywords == null || !chunk.Keywords.Any() || keywords == null)
            return false;

        var chunkKeywords = new HashSet<string>(chunk.Keywords, StringComparer.OrdinalIgnoreCase);
        return keywords.All(k => chunkKeywords.Contains(k));
    }

    /// <summary>
    /// Gets all unique keywords from a collection of chunks.
    /// </summary>
    /// <param name="chunks">The chunks to extract keywords from</param>
    /// <returns>A distinct list of all keywords</returns>
    public static IReadOnlyList<string> GetAllKeywords(this IEnumerable<ChunkNode> chunks)
    {
        return chunks
            .Where(c => c.Keywords != null)
            .SelectMany(c => c.Keywords)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k)
            .ToList();
    }

    /// <summary>
    /// Groups chunks by shared keywords.
    /// </summary>
    /// <param name="chunks">The chunks to group</param>
    /// <param name="minimumSharedKeywords">Minimum number of shared keywords for grouping</param>
    /// <returns>Groups of chunks that share keywords</returns>
    public static IEnumerable<IGrouping<string, ChunkNode>> GroupBySharedKeywords(
        this IEnumerable<ChunkNode> chunks, 
        int minimumSharedKeywords = 2)
    {
        var chunkList = chunks.ToList();
        var groups = new Dictionary<string, List<ChunkNode>>();

        foreach (var chunk in chunkList)
        {
            if (chunk.Keywords == null || chunk.Keywords.Count < minimumSharedKeywords)
                continue;

            // Create a signature from sorted keywords
            var signature = string.Join("|", chunk.Keywords.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
            
            if (!groups.ContainsKey(signature))
                groups[signature] = new List<ChunkNode>();
            
            groups[signature].Add(chunk);
        }

        return groups
            .Where(g => g.Value.Count > 1) // Only return groups with multiple chunks
            .Select(g => new Grouping<string, ChunkNode>(g.Key, g.Value));
    }

    /// <summary>
    /// Finds the most common keywords across a collection of chunks.
    /// </summary>
    /// <param name="chunks">The chunks to analyze</param>
    /// <param name="topCount">Number of top keywords to return</param>
    /// <returns>The most frequently occurring keywords</returns>
    public static IReadOnlyList<(string Keyword, int Frequency)> GetTopKeywords(
        this IEnumerable<ChunkNode> chunks, 
        int topCount = 10)
    {
        return chunks
            .Where(c => c.Keywords != null)
            .SelectMany(c => c.Keywords)
            .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Take(topCount)
            .Select(g => (g.Key, g.Count()))
            .ToList();
    }

    /// <summary>
    /// Creates a keyword-based index for fast chunk lookup.
    /// </summary>
    /// <param name="chunks">The chunks to index</param>
    /// <returns>A dictionary mapping keywords to chunks that contain them</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<ChunkNode>> CreateKeywordIndex(
        this IEnumerable<ChunkNode> chunks)
    {
        var index = new Dictionary<string, List<ChunkNode>>(StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in chunks)
        {
            if (chunk.Keywords == null)
                continue;

            foreach (var keyword in chunk.Keywords)
            {
                if (!index.ContainsKey(keyword))
                    index[keyword] = new List<ChunkNode>();
                
                index[keyword].Add(chunk);
            }
        }

        return index.ToDictionary(
            kvp => kvp.Key, 
            kvp => (IReadOnlyList<ChunkNode>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Helper class for creating groupings.
/// </summary>
internal class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
{
    private readonly IEnumerable<TElement> _elements;

    public Grouping(TKey key, IEnumerable<TElement> elements)
    {
        Key = key;
        _elements = elements;
    }

    public TKey Key { get; }

    public IEnumerator<TElement> GetEnumerator() => _elements.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

