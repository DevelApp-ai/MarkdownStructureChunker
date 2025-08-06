using MarkdownStructureChunker.Core.Interfaces;
using System.Text.RegularExpressions;

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

        // Normalize and tokenize the text
        var words = ExtractWords(content);
        
        // Filter out stop words and short words
        var filteredWords = words
            .Where(word => word.Length >= 3 && !StopWords.IsStopWord(word))
            .ToList();

        // Count word frequencies
        var wordFrequencies = filteredWords
            .GroupBy(word => word, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        // Select top keywords by frequency
        var keywords = wordFrequencies
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key) // Secondary sort for consistency
            .Take(maxKeywords)
            .Select(kvp => kvp.Key.ToLowerInvariant())
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(keywords);
    }

    /// <summary>
    /// Extracts words from text using regex pattern matching.
    /// </summary>
    /// <param name="text">The input text</param>
    /// <returns>A list of extracted words</returns>
    private static List<string> ExtractWords(string text)
    {
        // Use regex to extract words (sequences of letters)
        var wordPattern = new Regex(@"\b[a-zA-Z]+\b", RegexOptions.Compiled);
        var matches = wordPattern.Matches(text);
        
        return matches.Cast<Match>()
            .Select(m => m.Value)
            .ToList();
    }
}

