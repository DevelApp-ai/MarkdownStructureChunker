using MarkdownStructureChunker.Core.Interfaces;
using System.Text.RegularExpressions;

namespace MarkdownStructureChunker.Core.Extractors;

/// <summary>
/// A simple keyword extractor that uses basic text processing techniques.
/// This serves as a placeholder until the ML.NET implementation is complete.
/// </summary>
public class SimpleKeywordExtractor : IKeywordExtractor
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "has", "he", "in", "is", "it",
        "its", "of", "on", "that", "the", "to", "was", "will", "with", "would", "could", "should",
        "this", "these", "those", "they", "them", "their", "there", "where", "when", "what", "who",
        "how", "why", "which", "can", "may", "might", "must", "shall", "have", "had", "do", "does",
        "did", "been", "being", "am", "were", "but", "or", "not", "no", "yes", "if", "then", "else",
        "than", "more", "most", "less", "least", "very", "much", "many", "some", "any", "all", "each",
        "every", "both", "either", "neither", "one", "two", "three", "first", "second", "third",
        "last", "next", "previous", "before", "after", "during", "while", "until", "since", "because",
        "so", "therefore", "however", "although", "though", "unless", "except", "instead", "rather",
        "quite", "just", "only", "also", "too", "even", "still", "yet", "already", "again", "once",
        "twice", "here", "there", "everywhere", "anywhere", "somewhere", "nowhere", "up", "down",
        "left", "right", "above", "below", "over", "under", "through", "across", "around", "between",
        "among", "within", "without", "inside", "outside", "near", "far", "close", "away", "back",
        "forward", "toward", "against", "along", "beside", "behind", "beyond", "beneath", "above"
    };

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
            .Where(word => word.Length >= 3 && !StopWords.Contains(word))
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

