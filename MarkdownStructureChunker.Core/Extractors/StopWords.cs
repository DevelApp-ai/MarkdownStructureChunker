namespace MarkdownStructureChunker.Core.Extractors;

/// <summary>
/// Internal class containing common stop words used by keyword extractors.
/// </summary>
internal static class StopWords
{
    /// <summary>
    /// Common English stop words that should be filtered out during keyword extraction.
    /// </summary>
    public static readonly HashSet<string> CommonEnglishStopWords = new(StringComparer.OrdinalIgnoreCase)
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
    /// Checks if a word is a stop word.
    /// </summary>
    /// <param name="word">The word to check</param>
    /// <returns>True if the word is a stop word, false otherwise</returns>
    public static bool IsStopWord(string word)
    {
        return CommonEnglishStopWords.Contains(word);
    }
}

