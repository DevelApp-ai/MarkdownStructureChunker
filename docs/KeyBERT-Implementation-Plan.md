# Implementation Plan: KeyBERT Integration for Enhanced Knowledge Extraction

This document outlines the implementation plan for integrating KeyBERT methodology into the MarkdownStructureChunker to align with GraphRAG optimization strategies.

## Overview

The KeyBERT methodology represents a significant advancement over traditional frequency-based keyword extraction by leveraging semantic similarity between document embeddings and candidate phrase embeddings. This implementation will enhance the existing keyword extraction capabilities while maintaining backward compatibility.

## Architecture Design

### New Components

1. **KeyBertExtractor**: Implementation of IKeywordExtractor using semantic similarity
2. **CandidateGenerator**: N-gram generation and filtering
3. **SimilarityCalculator**: Cosine similarity computation utilities
4. **HybridKeywordExtractor**: Combines KeyBERT with LLM abstraction

### Enhanced Components

1. **OnnxVectorizer**: Enhanced post-processing with proper attention masking
2. **ChunkerConfiguration**: New configuration options for KeyBERT
3. **StructureChunker**: Integration of hybrid extraction strategy

## Implementation Details

### Phase 1: KeyBERT Core Implementation

#### 1.1 Candidate Generation

```csharp
namespace MarkdownStructureChunker.Core.Extractors;

public class CandidateGenerator
{
    private readonly CandidateGenerationOptions _options;
    
    public CandidateGenerator(CandidateGenerationOptions options)
    {
        _options = options;
    }
    
    public IEnumerable<string> GenerateCandidates(string text)
    {
        // Clean and preprocess text
        var cleanText = CleanText(text);
        var sentences = SplitIntoSentences(cleanText);
        
        var candidates = new HashSet<string>();
        
        // Generate n-grams
        foreach (var sentence in sentences)
        {
            var words = TokenizeWords(sentence);
            
            // Generate 1-grams to n-grams
            for (int n = _options.MinNGramSize; n <= _options.MaxNGramSize; n++)
            {
                candidates.UnionWith(GenerateNGrams(words, n));
            }
        }
        
        // Filter candidates
        return FilterCandidates(candidates);
    }
    
    private IEnumerable<string> GenerateNGrams(string[] words, int n)
    {
        for (int i = 0; i <= words.Length - n; i++)
        {
            var ngram = string.Join(" ", words.Skip(i).Take(n));
            if (IsValidCandidate(ngram))
            {
                yield return ngram;
            }
        }
    }
    
    private bool IsValidCandidate(string candidate)
    {
        // Filter out candidates that are too short/long
        if (candidate.Length < _options.MinCandidateLength || 
            candidate.Length > _options.MaxCandidateLength)
            return false;
            
        // Filter out candidates with only stopwords
        if (_options.StopWords.Any() && IsOnlyStopWords(candidate))
            return false;
            
        // Filter out candidates with special characters only
        if (Regex.IsMatch(candidate, @"^[^a-zA-Z0-9\s]+$"))
            return false;
            
        return true;
    }
}

public class CandidateGenerationOptions
{
    public int MinNGramSize { get; set; } = 1;
    public int MaxNGramSize { get; set; } = 3;
    public int MinCandidateLength { get; set; } = 3;
    public int MaxCandidateLength { get; set; } = 100;
    public IReadOnlySet<string> StopWords { get; set; } = new HashSet<string>();
    public bool UsePosTags { get; set; } = false;
}
```

#### 1.2 KeyBERT Extractor Implementation

```csharp
namespace MarkdownStructureChunker.Core.Extractors;

public class KeyBertExtractor : IKeywordExtractor, IDisposable
{
    private readonly ILocalVectorizer _vectorizer;
    private readonly CandidateGenerator _candidateGenerator;
    private readonly KeyBertOptions _options;
    private bool _disposed = false;
    
    public KeyBertExtractor(
        ILocalVectorizer vectorizer,
        CandidateGenerator candidateGenerator,
        KeyBertOptions options)
    {
        _vectorizer = vectorizer ?? throw new ArgumentNullException(nameof(vectorizer));
        _candidateGenerator = candidateGenerator ?? throw new ArgumentNullException(nameof(candidateGenerator));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    public async Task<IReadOnlyList<string>> ExtractKeywordsAsync(
        string text, 
        int maxKeywords = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();
            
        try
        {
            // Step 1: Generate document embedding
            var documentEmbedding = await _vectorizer.VectorizeAsync(text, isQuery: false);
            
            // Step 2: Generate candidate phrases
            var candidates = _candidateGenerator.GenerateCandidates(text).ToList();
            
            if (!candidates.Any())
                return Array.Empty<string>();
            
            // Step 3: Generate embeddings for all candidates
            var candidateEmbeddings = await VectorizeCandidatesAsync(candidates, cancellationToken);
            
            // Step 4: Calculate similarities
            var similarities = CalculateSimilarities(documentEmbedding, candidateEmbeddings, candidates);
            
            // Step 5: Apply diversity and selection
            var selectedKeywords = SelectDiverseKeywords(similarities, maxKeywords);
            
            return selectedKeywords;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in KeyBERT extraction: {ex.Message}");
            // Fallback to simple extraction
            return await FallbackExtraction(text, maxKeywords, cancellationToken);
        }
    }
    
    private async Task<float[][]> VectorizeCandidatesAsync(
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken)
    {
        var embeddings = new float[candidates.Count][];
        
        // Process in batches to avoid memory issues
        const int batchSize = 50;
        
        for (int i = 0; i < candidates.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var batch = candidates.Skip(i).Take(batchSize);
            var batchTasks = batch.Select(candidate => 
                _vectorizer.VectorizeAsync(candidate, isQuery: true));
                
            var batchResults = await Task.WhenAll(batchTasks);
            
            for (int j = 0; j < batchResults.Length; j++)
            {
                embeddings[i + j] = batchResults[j];
            }
        }
        
        return embeddings;
    }
    
    private IEnumerable<KeywordSimilarity> CalculateSimilarities(
        float[] documentEmbedding,
        float[][] candidateEmbeddings,
        IReadOnlyList<string> candidates)
    {
        var similarities = new List<KeywordSimilarity>();
        
        for (int i = 0; i < candidates.Count; i++)
        {
            var similarity = CosineSimilarity(documentEmbedding, candidateEmbeddings[i]);
            
            // Only include candidates above threshold
            if (similarity >= _options.MinSimilarityThreshold)
            {
                similarities.Add(new KeywordSimilarity
                {
                    Keyword = candidates[i],
                    Similarity = similarity,
                    Length = candidates[i].Length,
                    WordCount = candidates[i].Split(' ').Length
                });
            }
        }
        
        return similarities.OrderByDescending(s => s.Similarity);
    }
    
    private IReadOnlyList<string> SelectDiverseKeywords(
        IEnumerable<KeywordSimilarity> similarities,
        int maxKeywords)
    {
        var selected = new List<KeywordSimilarity>();
        var remaining = similarities.ToList();
        
        // Always include the top similarity candidate
        if (remaining.Any())
        {
            selected.Add(remaining.First());
            remaining.RemoveAt(0);
        }
        
        // Apply diversity selection
        while (selected.Count < maxKeywords && remaining.Any())
        {
            var nextCandidate = FindMostDiverseCandidate(selected, remaining);
            if (nextCandidate != null)
            {
                selected.Add(nextCandidate);
                remaining.Remove(nextCandidate);
            }
            else
            {
                break;
            }
        }
        
        return selected.Select(s => s.Keyword).ToList();
    }
    
    private KeywordSimilarity? FindMostDiverseCandidate(
        IReadOnlyList<KeywordSimilarity> selected,
        IReadOnlyList<KeywordSimilarity> candidates)
    {
        KeywordSimilarity? bestCandidate = null;
        double bestScore = double.MinValue;
        
        foreach (var candidate in candidates)
        {
            // Calculate diversity score
            var diversityScore = CalculateDiversityScore(candidate, selected);
            
            // Combine similarity and diversity
            var finalScore = (_options.SimilarityWeight * candidate.Similarity) +
                           (_options.DiversityWeight * diversityScore);
            
            if (finalScore > bestScore)
            {
                bestScore = finalScore;
                bestCandidate = candidate;
            }
        }
        
        return bestCandidate;
    }
    
    private double CalculateDiversityScore(
        KeywordSimilarity candidate,
        IReadOnlyList<KeywordSimilarity> selected)
    {
        if (!selected.Any())
            return 1.0;
        
        // Calculate minimum textual similarity to existing keywords
        var minTextSimilarity = selected
            .Select(s => CalculateTextualSimilarity(candidate.Keyword, s.Keyword))
            .Min();
        
        // Diversity score is inverse of similarity (higher diversity = lower similarity to existing)
        return 1.0 - minTextSimilarity;
    }
    
    private double CalculateTextualSimilarity(string text1, string text2)
    {
        // Simple Jaccard similarity based on word overlap
        var words1 = text1.ToLowerInvariant().Split(' ').ToHashSet();
        var words2 = text2.ToLowerInvariant().Split(' ').ToHashSet();
        
        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();
        
        return union > 0 ? (double)intersection / union : 0.0;
    }
    
    private static float CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            throw new ArgumentException("Vectors must have the same length");
        
        float dotProduct = 0f;
        float normA = 0f;
        float normB = 0f;
        
        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }
        
        var magnitude = (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        return magnitude > 0 ? dotProduct / magnitude : 0f;
    }
    
    private async Task<IReadOnlyList<string>> FallbackExtraction(
        string text,
        int maxKeywords,
        CancellationToken cancellationToken)
    {
        // Fallback to simple frequency-based extraction
        var simpleExtractor = new SimpleKeywordExtractor();
        return await simpleExtractor.ExtractKeywordsAsync(text, maxKeywords, cancellationToken);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            // No resources to dispose currently
            _disposed = true;
        }
    }
}

public class KeywordSimilarity
{
    public string Keyword { get; set; } = string.Empty;
    public float Similarity { get; set; }
    public int Length { get; set; }
    public int WordCount { get; set; }
}

public class KeyBertOptions
{
    public float MinSimilarityThreshold { get; set; } = 0.1f;
    public double SimilarityWeight { get; set; } = 0.7;
    public double DiversityWeight { get; set; } = 0.3;
    public bool EnableDiversitySelection { get; set; } = true;
    public int MaxCandidatesPerBatch { get; set; } = 50;
}
```

### Phase 2: Enhanced ONNX Post-Processing

#### 2.1 Improved Attention-Masked Mean Pooling

```csharp
// Enhancement to OnnxVectorizer.cs
private float[] ProcessModelOutputEnhanced(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
{
    try
    {
        // Extract tensors
        var lastHiddenState = ExtractTensor(outputs, "last_hidden_state");
        var attentionMask = ExtractTensor(outputs, "attention_mask");
        
        // Perform attention-masked mean pooling
        var pooledEmbedding = ComputeAttentionMaskedMeanPooling(lastHiddenState, attentionMask);
        
        // L2 normalize the result
        return NormalizeVector(pooledEmbedding);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in enhanced output processing: {ex.Message}");
        return new float[VectorDimension]; // Return zero vector as fallback
    }
}

private float[] ComputeAttentionMaskedMeanPooling(Tensor<float> lastHiddenState, Tensor<float> attentionMask)
{
    var dimensions = lastHiddenState.Dimensions.ToArray();
    var batchSize = dimensions[0];
    var sequenceLength = dimensions[1];
    var hiddenSize = dimensions[2];
    
    // Initialize result vector
    var result = new float[hiddenSize];
    
    // Step 1: Expand attention mask to match hidden state dimensions
    // attention_mask shape: [batch_size, sequence_length]
    // need to expand to: [batch_size, sequence_length, hidden_size]
    
    float totalTokens = 0f;
    
    // Step 2: Apply attention mask and sum
    for (int seqIdx = 0; seqIdx < sequenceLength; seqIdx++)
    {
        var maskValue = attentionMask[0, seqIdx]; // assuming batch_size = 1
        
        if (maskValue > 0) // Only process non-padded tokens
        {
            totalTokens += maskValue;
            
            // Add weighted hidden states
            for (int hiddenIdx = 0; hiddenIdx < hiddenSize; hiddenIdx++)
            {
                result[hiddenIdx] += lastHiddenState[0, seqIdx, hiddenIdx] * maskValue;
            }
        }
    }
    
    // Step 3: Divide by the number of actual tokens to get mean
    if (totalTokens > 0)
    {
        for (int i = 0; i < hiddenSize; i++)
        {
            result[i] /= totalTokens;
        }
    }
    
    return result;
}

private Tensor<float> ExtractTensor(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs, string tensorName)
{
    var output = outputs.FirstOrDefault(o => o.Name == tensorName);
    if (output == null)
    {
        throw new InvalidOperationException($"Required tensor '{tensorName}' not found in model outputs");
    }
    
    return output.AsTensor<float>();
}

private static float[] NormalizeVector(float[] vector)
{
    // L2 normalization
    var sumOfSquares = vector.Sum(x => x * x);
    var magnitude = (float)Math.Sqrt(sumOfSquares);
    
    if (magnitude > 0)
    {
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] /= magnitude;
        }
    }
    
    return vector;
}
```

### Phase 3: Hybrid Extraction Strategy

#### 3.1 Hybrid Keyword Extractor

```csharp
namespace MarkdownStructureChunker.Core.Extractors;

public class HybridKeywordExtractor : IKeywordExtractor, IDisposable
{
    private readonly KeyBertExtractor _keyBertExtractor;
    private readonly IKeywordExtractor _fallbackExtractor;
    private readonly HybridExtractionOptions _options;
    private bool _disposed = false;
    
    public HybridKeywordExtractor(
        KeyBertExtractor keyBertExtractor,
        IKeywordExtractor fallbackExtractor,
        HybridExtractionOptions options)
    {
        _keyBertExtractor = keyBertExtractor ?? throw new ArgumentNullException(nameof(keyBertExtractor));
        _fallbackExtractor = fallbackExtractor ?? throw new ArgumentNullException(nameof(fallbackExtractor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    public async Task<IReadOnlyList<string>> ExtractKeywordsAsync(
        string text,
        int maxKeywords = 10,
        CancellationToken cancellationToken = default)
    {
        var allKeywords = new List<ExtractedKeyword>();
        
        // Pass 1: KeyBERT extraction (high-precision, verbatim)
        try
        {
            var keyBertKeywords = await _keyBertExtractor.ExtractKeywordsAsync(
                text, 
                _options.MaxKeyBertKeywords, 
                cancellationToken);
                
            allKeywords.AddRange(keyBertKeywords.Select(k => new ExtractedKeyword
            {
                Text = k,
                Source = ExtractionSource.KeyBERT,
                Priority = 1
            }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"KeyBERT extraction failed: {ex.Message}");
        }
        
        // Pass 2: Fallback extraction if needed
        if (allKeywords.Count < _options.MinRequiredKeywords)
        {
            try
            {
                var fallbackKeywords = await _fallbackExtractor.ExtractKeywordsAsync(
                    text,
                    maxKeywords - allKeywords.Count,
                    cancellationToken);
                    
                // Add fallback keywords that don't duplicate KeyBERT results
                var existingKeywords = new HashSet<string>(
                    allKeywords.Select(k => k.Text.ToLowerInvariant()));
                    
                foreach (var keyword in fallbackKeywords)
                {
                    if (!existingKeywords.Contains(keyword.ToLowerInvariant()))
                    {
                        allKeywords.Add(new ExtractedKeyword
                        {
                            Text = keyword,
                            Source = ExtractionSource.Fallback,
                            Priority = 2
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fallback extraction failed: {ex.Message}");
            }
        }
        
        // Sort and return top keywords
        return allKeywords
            .OrderBy(k => k.Priority)
            .ThenByDescending(k => k.Text.Length) // Prefer longer phrases
            .Take(maxKeywords)
            .Select(k => k.Text)
            .ToList();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _keyBertExtractor?.Dispose();
            _disposed = true;
        }
    }
}

public class ExtractedKeyword
{
    public string Text { get; set; } = string.Empty;
    public ExtractionSource Source { get; set; }
    public int Priority { get; set; }
}

public enum ExtractionSource
{
    KeyBERT,
    Fallback,
    LLM
}

public class HybridExtractionOptions
{
    public int MaxKeyBertKeywords { get; set; } = 8;
    public int MinRequiredKeywords { get; set; } = 5;
    public bool PreferLongerPhrases { get; set; } = true;
}
```

### Phase 4: Configuration Updates

#### 4.1 Enhanced Configuration Options

```csharp
// Addition to ChunkerConfiguration.cs
public class ChunkerConfiguration
{
    // ... existing properties ...
    
    /// <summary>
    /// Gets or sets the keyword extraction strategy to use.
    /// </summary>
    public KeywordExtractionStrategy ExtractionStrategy { get; set; } = KeywordExtractionStrategy.Hybrid;
    
    /// <summary>
    /// Gets or sets options for KeyBERT extraction.
    /// </summary>
    public KeyBertOptions KeyBertOptions { get; set; } = new KeyBertOptions();
    
    /// <summary>
    /// Gets or sets options for hybrid extraction strategy.
    /// </summary>
    public HybridExtractionOptions HybridOptions { get; set; } = new HybridExtractionOptions();
    
    /// <summary>
    /// Gets or sets options for candidate generation.
    /// </summary>
    public CandidateGenerationOptions CandidateOptions { get; set; } = new CandidateGenerationOptions();
    
    /// <summary>
    /// Creates a configuration optimized for semantic keyword extraction using KeyBERT.
    /// </summary>
    public static ChunkerConfiguration CreateForSemanticExtraction()
    {
        return new ChunkerConfiguration
        {
            ExtractionStrategy = KeywordExtractionStrategy.KeyBERT,
            ExtractKeywords = true,
            MaxKeywordsPerChunk = 12,
            KeyBertOptions = new KeyBertOptions
            {
                MinSimilarityThreshold = 0.3f,
                SimilarityWeight = 0.8,
                DiversityWeight = 0.2,
                EnableDiversitySelection = true
            },
            CandidateOptions = new CandidateGenerationOptions
            {
                MinNGramSize = 1,
                MaxNGramSize = 4,
                MinCandidateLength = 3,
                MaxCandidateLength = 50
            }
        };
    }
}

public enum KeywordExtractionStrategy
{
    Simple,
    MLNet,
    KeyBERT,
    Hybrid
}
```

## Testing Strategy

### Unit Tests

1. **CandidateGenerator Tests**
   - N-gram generation accuracy
   - Filtering logic validation
   - Edge cases (empty text, special characters)

2. **KeyBertExtractor Tests**
   - Similarity calculation accuracy
   - Diversity selection logic
   - Error handling and fallback behavior

3. **HybridExtractor Tests**
   - Strategy selection logic
   - Keyword deduplication
   - Priority-based sorting

### Integration Tests

1. **End-to-End Extraction Pipeline**
   - Full document processing with KeyBERT
   - Performance benchmarking
   - Quality assessment vs. baseline

2. **ONNX Enhancement Tests**
   - Attention masking accuracy
   - L2 normalization validation
   - Performance impact measurement

### Performance Tests

1. **Throughput Testing**
   - Documents per second with KeyBERT
   - Memory usage profiling
   - Scalability assessment

2. **Quality Metrics**
   - Keyword relevance scoring
   - Diversity measurement
   - Semantic coherence evaluation

## Migration Path

### Backward Compatibility

1. **Configuration Migration**
   ```csharp
   // Automatic migration for existing configurations
   public static ChunkerConfiguration MigrateToSemanticExtraction(ChunkerConfiguration existing)
   {
       return existing with
       {
           ExtractionStrategy = KeywordExtractionStrategy.Hybrid,
           KeyBertOptions = new KeyBertOptions(),
           HybridOptions = new HybridExtractionOptions
           {
               MaxKeyBertKeywords = Math.Max(6, existing.MaxKeywordsPerChunk - 2)
           }
       };
   }
   ```

2. **Factory Method Updates**
   ```csharp
   // Updated factory in StructureChunker
   public static StructureChunker CreateWithSemanticExtraction(
       ChunkerConfiguration configuration,
       string? onnxModelPath = null)
   {
       var vectorizer = new OnnxVectorizer(onnxModelPath);
       var candidateGenerator = new CandidateGenerator(configuration.CandidateOptions);
       var keyBertExtractor = new KeyBertExtractor(vectorizer, candidateGenerator, configuration.KeyBertOptions);
       var hybridExtractor = new HybridKeywordExtractor(
           keyBertExtractor, 
           new SimpleKeywordExtractor(), 
           configuration.HybridOptions);
           
       var strategy = new PatternBasedStrategy(
           PatternBasedStrategy.CreateDefaultRules(), 
           configuration);
           
       return new StructureChunker(strategy, hybridExtractor, configuration);
   }
   ```

## Performance Expectations

### KeyBERT Implementation

- **Throughput**: 50-100 documents/second (depending on document size)
- **Memory**: 500MB-1GB additional for ONNX model and embeddings
- **Latency**: 100-500ms per document (depending on candidate count)

### Quality Improvements

- **Relevance**: 20-30% improvement in keyword semantic relevance
- **Diversity**: Better keyword diversity through similarity-based selection
- **Consistency**: More consistent results across different document types

## Conclusion

This implementation plan provides a comprehensive roadmap for integrating KeyBERT methodology into the MarkdownStructureChunker. The phased approach ensures:

1. **High-impact improvements** through semantic-based keyword extraction
2. **Backward compatibility** with existing configurations and APIs
3. **Extensibility** for future enhancements like LLM integration
4. **Production readiness** with comprehensive testing and error handling

The resulting system will be significantly more aligned with advanced GraphRAG optimization strategies while maintaining the robust, configurable architecture that makes MarkdownStructureChunker valuable for diverse use cases.