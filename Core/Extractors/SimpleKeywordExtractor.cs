using MarkdownStructureChunker.Core.Interfaces;

namespace MarkdownStructureChunker.Core.Extractors;

/// <summary>
/// A simple keyword extractor that uses basic text processing techniques.
/// This serves as a placeholder until the ML.NET implementation is complete.
/// </summary>
public class SimpleKeywordExtractor : IKeywordExtractor
{
    /// <summary>
    /// Extracts keywords from the given text content using simple frequency analysis.
    /// </summary>
    /// <param name="content">The text content to analyze</param>
    /// <param name="maxKeywords">Maximum number of keywords to extract</param>
    /// <returns>A list of extracted keywords</returns>
    public Task<IReadOnlyList<string>> ExtractKeywordsAsync(string content, int maxKeywords = 10)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult<IReadOnlyList<string>>(new List<string>());

        var keywords = KeywordFrequencyAnalyzer.ExtractTopKeywords(content, maxKeywords);
        return Task.FromResult(keywords);
    }
}

