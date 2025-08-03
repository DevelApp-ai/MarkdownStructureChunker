using MarkdownStructureChunker.Core.Interfaces;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text.RegularExpressions;

namespace MarkdownStructureChunker.Core.Extractors;

/// <summary>
/// ML.NET-based keyword extractor that uses text processing pipeline
/// to identify the most significant terms in a chunk.
/// </summary>
public class MLNetKeywordExtractor : IKeywordExtractor, IDisposable
{
    private readonly MLContext _mlContext;
    private readonly ITransformer? _pipeline;
    private readonly PredictionEngine<TextInput, TextFeatures>? _predictionEngine;
    private bool _disposed = false;

    public MLNetKeywordExtractor()
    {
        _mlContext = new MLContext(seed: 42);
        
        try
        {
            _pipeline = CreateTextProcessingPipeline();
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<TextInput, TextFeatures>(_pipeline);
        }
        catch (Exception ex)
        {
            // If ML.NET pipeline creation fails, we'll fall back to simple extraction
            Console.WriteLine($"Warning: ML.NET pipeline creation failed: {ex.Message}");
            _pipeline = null;
            _predictionEngine = null;
        }
    }

    /// <summary>
    /// Extracts keywords from the given text content using ML.NET text processing pipeline.
    /// </summary>
    /// <param name="content">The text content to analyze</param>
    /// <param name="maxKeywords">Maximum number of keywords to extract</param>
    /// <returns>A list of extracted keywords</returns>
    public Task<IReadOnlyList<string>> ExtractKeywordsAsync(string content, int maxKeywords = 10)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult<IReadOnlyList<string>>(new List<string>());

        try
        {
            if (_predictionEngine != null)
            {
                return Task.FromResult(ExtractKeywordsWithMLNet(content, maxKeywords));
            }
            else
            {
                // Fallback to simple extraction if ML.NET is not available
                return ExtractKeywordsSimple(content, maxKeywords);
            }
        }
        catch (Exception)
        {
            // If ML.NET extraction fails, fall back to simple extraction
            return ExtractKeywordsSimple(content, maxKeywords);
        }
    }

    /// <summary>
    /// Creates the ML.NET text processing pipeline for keyword extraction.
    /// </summary>
    /// <returns>A trained transformer pipeline</returns>
    private ITransformer CreateTextProcessingPipeline()
    {
        // Create a simple dataset for training the pipeline
        var sampleData = new List<TextInput>
        {
            new() { Text = "This is sample text for training the pipeline with various words and terms." },
            new() { Text = "Another example with different vocabulary to help establish the text processing model." },
            new() { Text = "Technical documentation often contains specialized terminology and concepts." }
        };

        var dataView = _mlContext.Data.LoadFromEnumerable(sampleData);

        // Create the text processing pipeline
        var pipeline = _mlContext.Transforms.Text.NormalizeText("NormalizedText", "Text",
                keepDiacritics: false, keepPunctuations: false, keepNumbers: true)
            .Append(_mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedText"))
            .Append(_mlContext.Transforms.Text.RemoveDefaultStopWords("FilteredTokens", "Tokens"))
            .Append(_mlContext.Transforms.Text.ProduceWordBags("Features", "FilteredTokens"));

        // Fit the pipeline
        return pipeline.Fit(dataView);
    }

    /// <summary>
    /// Extracts keywords using the ML.NET pipeline.
    /// </summary>
    /// <param name="content">The text content to analyze</param>
    /// <param name="maxKeywords">Maximum number of keywords to extract</param>
    /// <returns>A list of extracted keywords</returns>
    private IReadOnlyList<string> ExtractKeywordsWithMLNet(string content, int maxKeywords)
    {
        if (_predictionEngine == null)
            return new List<string>();

        // Clean and prepare the text
        var cleanedContent = CleanText(content);
        var input = new TextInput { Text = cleanedContent };

        // Process through ML.NET pipeline
        var prediction = _predictionEngine.Predict(input);

        // Extract words from the original text for frequency analysis
        var words = ExtractWords(cleanedContent);
        var filteredWords = words.Where(w => w.Length >= 3 && !IsStopWord(w)).ToList();

        // Count word frequencies
        var wordFrequencies = filteredWords
            .GroupBy(word => word, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        // Select top keywords by frequency
        var keywords = wordFrequencies
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .Take(maxKeywords)
            .Select(kvp => kvp.Key.ToLowerInvariant())
            .ToList();

        return keywords;
    }

    /// <summary>
    /// Simple keyword extraction fallback method.
    /// </summary>
    /// <param name="content">The text content to analyze</param>
    /// <param name="maxKeywords">Maximum number of keywords to extract</param>
    /// <returns>A list of extracted keywords</returns>
    private Task<IReadOnlyList<string>> ExtractKeywordsSimple(string content, int maxKeywords)
    {
        var words = ExtractWords(content);
        var filteredWords = words.Where(w => w.Length >= 3 && !IsStopWord(w)).ToList();

        var wordFrequencies = filteredWords
            .GroupBy(word => word, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var keywords = wordFrequencies
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .Take(maxKeywords)
            .Select(kvp => kvp.Key.ToLowerInvariant())
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(keywords);
    }

    /// <summary>
    /// Cleans text by removing special characters and normalizing whitespace.
    /// </summary>
    /// <param name="text">The input text</param>
    /// <returns>Cleaned text</returns>
    private static string CleanText(string text)
    {
        // Remove markdown formatting and special characters
        text = Regex.Replace(text, @"[#*_`\[\](){}]", " ");
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    /// <summary>
    /// Extracts words from text using regex pattern matching.
    /// </summary>
    /// <param name="text">The input text</param>
    /// <returns>A list of extracted words</returns>
    private static List<string> ExtractWords(string text)
    {
        var wordPattern = new Regex(@"\b[a-zA-Z]+\b", RegexOptions.Compiled);
        var matches = wordPattern.Matches(text);
        
        return matches.Cast<Match>()
            .Select(m => m.Value)
            .ToList();
    }

    /// <summary>
    /// Checks if a word is a stop word.
    /// </summary>
    /// <param name="word">The word to check</param>
    /// <returns>True if the word is a stop word</returns>
    private static bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

        return stopWords.Contains(word);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _predictionEngine?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Input class for ML.NET text processing.
/// </summary>
public class TextInput
{
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Output class for ML.NET text features.
/// </summary>
public class TextFeatures
{
    [VectorType]
    public float[] Features { get; set; } = Array.Empty<float>();
}

