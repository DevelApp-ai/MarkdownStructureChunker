using System.Text.RegularExpressions;

namespace MarkdownStructureChunker.Core.Extractors;

/// <summary>
/// Shared frequency-based keyword analysis used by both
/// <see cref="SimpleKeywordExtractor"/> and <see cref="MLNetKeywordExtractor"/>.
/// Centralizes word tokenization, stop-word filtering, and frequency ranking so
/// the behavior is consistent and the compiled regex is allocated once.
/// </summary>
internal static class KeywordFrequencyAnalyzer
{
    private static readonly Regex WordPattern = new(@"\b[a-zA-Z]+\b", RegexOptions.Compiled);

    /// <summary>
    /// Extracts the top <paramref name="maxKeywords"/> keywords from
    /// <paramref name="content"/> using frequency analysis, after filtering
    /// stop words and words shorter than 3 characters. Results are returned
    /// lower-cased and ordered by descending frequency then alphabetically.
    /// </summary>
    /// <param name="content">The text to analyze.</param>
    /// <param name="maxKeywords">Maximum number of keywords to return.</param>
    /// <returns>The top keywords, or an empty list for null/blank input.</returns>
    public static IReadOnlyList<string> ExtractTopKeywords(string? content, int maxKeywords)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<string>();

        var words = WordPattern.Matches(content);
        var filtered = new List<string>();
        foreach (Match m in words)
        {
            var w = m.Value;
            if (w.Length >= 3 && !StopWords.IsStopWord(w))
                filtered.Add(w);
        }

        var frequencies = filtered
            .GroupBy(word => word, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        return frequencies
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .Take(maxKeywords)
            .Select(kvp => kvp.Key.ToLowerInvariant())
            .ToList();
    }
}
