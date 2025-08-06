namespace MarkdownStructureChunker.Core.Interfaces;

/// <summary>
/// A pluggable component for keyword extraction.
/// </summary>
public interface IKeywordExtractor
{
    /// <summary>
    /// Extracts keywords from the given text content.
    /// </summary>
    /// <param name="content">The text content to analyze</param>
    /// <param name="maxKeywords">Maximum number of keywords to extract</param>
    /// <returns>A list of extracted keywords</returns>
    Task<IReadOnlyList<string>> ExtractKeywordsAsync(string content, int maxKeywords = 10);
}

