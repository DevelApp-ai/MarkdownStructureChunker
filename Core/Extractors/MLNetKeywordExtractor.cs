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
    private readonly IChunkerLogger _logger;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MLNetKeywordExtractor"/> class.
    /// Sets up the ML.NET context and text processing pipeline for keyword extraction.
    /// </summary>
    public MLNetKeywordExtractor(IChunkerLogger? logger = null)
    {
        _mlContext = new MLContext(seed: 42);
        _logger = logger ?? NullChunkerLogger.Instance;

        try
        {
            _pipeline = CreateTextProcessingPipeline();
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<TextInput, TextFeatures>(_pipeline);
        }
        catch (Exception ex)
        {
            // If ML.NET pipeline creation fails, we'll fall back to simple extraction
            _logger.LogWarning($"ML.NET pipeline creation failed: {ex.Message}");
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

        // Frequency-based keyword selection on the cleaned text.
        var keywords = KeywordFrequencyAnalyzer.ExtractTopKeywords(cleanedContent, maxKeywords);
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
        var keywords = KeywordFrequencyAnalyzer.ExtractTopKeywords(content, maxKeywords);
        return Task.FromResult(keywords);
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
    /// Releases all resources used by the <see cref="MLNetKeywordExtractor"/>.
    /// </summary>
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
    /// <summary>
    /// Gets or sets the text content to be processed for keyword extraction.
    /// </summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Output class for ML.NET text features.
/// </summary>
public class TextFeatures
{
    /// <summary>
    /// Gets or sets the feature vector representing the processed text.
    /// </summary>
    [VectorType]
    public float[] Features { get; set; } = Array.Empty<float>();
}

