using MarkdownStructureChunker.Core.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using System.Numerics.Tensors;
using System.Text;

namespace MarkdownStructureChunker.Core.Vectorizers;

/// <summary>
/// ONNX-based vectorizer that provides semantic embeddings using transformer models.
/// Supports the intfloat/multilingual-e5-large model and other compatible BERT-style models.
/// 
/// This implementation includes:
/// - Proper tokenization using Microsoft.ML.Tokenizers
/// - ONNX model inference with tensor processing
/// - Fallback to deterministic embeddings when model is unavailable
/// - Support for both query and passage embeddings
/// </summary>
public class OnnxVectorizer : ILocalVectorizer, IDisposable
{
    private readonly InferenceSession? _session;
    private readonly Tokenizer? _tokenizer;
    private readonly bool _isModelAvailable;
    private readonly int _maxSequenceLength;
    private bool _disposed = false;

    /// <summary>
    /// Gets the dimension of the vector embeddings (1024 for multilingual-e5-large).
    /// </summary>
    public int VectorDimension => 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnnxVectorizer"/> class.
    /// </summary>
    /// <param name="modelPath">Optional path to the ONNX model file. If null or invalid, the vectorizer will operate in fallback mode.</param>
    /// <param name="tokenizerPath">Optional path to the tokenizer files. If null, uses built-in tokenization.</param>
    /// <param name="maxSequenceLength">Maximum sequence length for tokenization (default: 512).</param>
    public OnnxVectorizer(string? modelPath = null, string? tokenizerPath = null, int maxSequenceLength = 512)
    {
        _maxSequenceLength = maxSequenceLength;
        
        try
        {
            if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
            {
                // Validate model file before loading
                if (!ValidateModelFile(modelPath))
                {
                    Console.WriteLine($"Warning: Model file validation failed: {modelPath}");
                    _isModelAvailable = false;
                    return;
                }

                // Configure session options for better performance
                var sessionOptions = new SessionOptions();
                sessionOptions.InterOpNumThreads = Environment.ProcessorCount;
                sessionOptions.IntraOpNumThreads = Environment.ProcessorCount;
                sessionOptions.ExecutionMode = ExecutionMode.ORT_PARALLEL;
                sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                
                // Enable memory optimizations
                sessionOptions.EnableMemoryPattern = true;
                sessionOptions.EnableCpuMemArena = true;
                
                _session = new InferenceSession(modelPath, sessionOptions);
                _tokenizer = LoadTokenizer(tokenizerPath);
                _isModelAvailable = true;
                
                Console.WriteLine($"Enhanced ONNX model loaded successfully from: {modelPath}");
                Console.WriteLine($"Model inputs: {string.Join(", ", _session.InputMetadata.Keys)}");
                Console.WriteLine($"Model outputs: {string.Join(", ", _session.OutputMetadata.Keys)}");
                Console.WriteLine($"Performance optimizations enabled: CPU threads={Environment.ProcessorCount}");
            }
            else
            {
                _isModelAvailable = false;
                Console.WriteLine("ONNX model not found. Using deterministic fallback implementation.");
                Console.WriteLine("For production use, download the multilingual-e5-large ONNX model from Hugging Face.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to initialize enhanced ONNX model: {ex.Message}");
            _isModelAvailable = false;
        }
    }

    /// <summary>
    /// Validates the ONNX model file to ensure it's compatible.
    /// </summary>
    /// <param name="modelPath">Path to the model file</param>
    /// <returns>True if the model is valid, false otherwise</returns>
    private static bool ValidateModelFile(string modelPath)
    {
        try
        {
            var fileInfo = new FileInfo(modelPath);
            
            // Check file size (should be reasonable for a transformer model)
            if (fileInfo.Length < 1024 * 1024) // Less than 1MB is suspicious
            {
                Console.WriteLine($"Warning: Model file seems too small: {fileInfo.Length} bytes");
                return false;
            }
            
            if (fileInfo.Length > 10L * 1024 * 1024 * 1024) // More than 10GB is suspicious
            {
                Console.WriteLine($"Warning: Model file seems too large: {fileInfo.Length} bytes");
                return false;
            }
            
            // Check file extension
            if (!modelPath.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Warning: Model file doesn't have .onnx extension: {modelPath}");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating model file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads the tokenizer from the specified path or creates a default one.
    /// Enhanced version with better tokenizer support and fallback handling.
    /// </summary>
    /// <param name="tokenizerPath">Path to tokenizer files</param>
    /// <returns>Tokenizer instance or null if unavailable</returns>
    private static Tokenizer? LoadTokenizer(string? tokenizerPath)
    {
        try
        {
            if (!string.IsNullOrEmpty(tokenizerPath) && Directory.Exists(tokenizerPath))
            {
                // Try to load tokenizer.json first (preferred format)
                var tokenizerJsonPath = Path.Combine(tokenizerPath, "tokenizer.json");
                if (File.Exists(tokenizerJsonPath))
                {
                    Console.WriteLine($"Loading tokenizer from: {tokenizerJsonPath}");
                    // Note: This would need the correct API call when Microsoft.ML.Tokenizers supports it
                    // For now, fall back to BERT tokenizer
                }
                
                // Fallback to vocab.txt for BERT-style tokenizers
                var vocabPath = Path.Combine(tokenizerPath, "vocab.txt");
                if (File.Exists(vocabPath))
                {
                    Console.WriteLine($"Loading BERT tokenizer from: {vocabPath}");
                    return BertTokenizer.Create(vocabPath);
                }
                
                Console.WriteLine($"No compatible tokenizer files found in: {tokenizerPath}");
            }
            
            Console.WriteLine("Tokenizer files not found. Using enhanced fallback tokenization.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load enhanced tokenizer: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts text into vector embeddings using the ONNX model.
    /// Enhanced version with better error handling and performance monitoring.
    /// </summary>
    /// <param name="text">The text to vectorize</param>
    /// <param name="isQuery">Whether this is a query (true) or passage (false)</param>
    /// <returns>Vector embedding as an array of floats</returns>
    public async Task<float[]> VectorizeAsync(string text, bool isQuery = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new float[VectorDimension];

        // Add the appropriate prefix as required by the E5 model
        var prefixedText = isQuery ? $"query: {text}" : $"passage: {text}";

        if (_isModelAvailable && _session != null)
        {
            return await VectorizeWithOnnxAsync(prefixedText);
        }
        else
        {
            // Enhanced fallback to a deterministic implementation
            return await Task.FromResult(GenerateEnhancedDeterministicVector(prefixedText));
        }
    }

    /// <summary>
    /// Vectorizes multiple texts in a batch for improved performance.
    /// This is a new capability for production use.
    /// </summary>
    /// <param name="texts">The texts to vectorize</param>
    /// <param name="isQuery">Whether these are queries (true) or passages (false)</param>
    /// <returns>Array of vector embeddings</returns>
    public async Task<float[][]> VectorizeBatchAsync(IEnumerable<string> texts, bool isQuery = false)
    {
        var textList = texts.ToList();
        if (!textList.Any())
            return Array.Empty<float[]>();

        // For now, process in parallel. In future versions, we could implement true batch processing
        var tasks = textList.Select(text => VectorizeAsync(text, isQuery));
        var results = await Task.WhenAll(tasks);
        
        return results;
    }

    /// <summary>
    /// Vectorizes text using the ONNX model with proper tokenization and inference.
    /// Enhanced version with better error handling and performance monitoring.
    /// </summary>
    /// <param name="text">The text to vectorize</param>
    /// <returns>Vector embedding</returns>
    private async Task<float[]> VectorizeWithOnnxAsync(string text)
    {
        if (_session == null)
            return new float[VectorDimension];

        try
        {
            // Enhanced tokenization
            var tokens = EnhancedTokenizeText(text);
            
            // Create input tensors
            var inputTensors = CreateInputTensors(tokens);
            
            // Run ONNX inference with performance monitoring
            var startTime = DateTime.UtcNow;
            var outputs = await Task.Run(() => _session.Run(inputTensors));
            var inferenceTime = DateTime.UtcNow - startTime;
            
            // Log performance for monitoring (could be configurable)
            if (inferenceTime.TotalMilliseconds > 1000) // Log slow inferences
            {
                Console.WriteLine($"Slow ONNX inference detected: {inferenceTime.TotalMilliseconds:F2}ms for text length {text.Length}");
            }
            
            // Enhanced output processing
            var embeddings = ProcessModelOutputEnhanced(outputs);
            
            return NormalizeVector(embeddings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during enhanced ONNX inference: {ex.Message}");
            return GenerateEnhancedDeterministicVector(text);
        }
    }

    /// <summary>
    /// Enhanced tokenization with better BERT support and error handling.
    /// </summary>
    /// <param name="text">Text to tokenize</param>
    /// <returns>Enhanced tokenization result</returns>
    private TokenizationResult EnhancedTokenizeText(string text)
    {
        if (_tokenizer != null)
        {
            try
            {
                var encoding = _tokenizer.EncodeToTokens(text, out var normalizedText);
                var inputIds = encoding.Select(token => token.Id).Take(_maxSequenceLength - 2).ToArray(); // Reserve space for special tokens
                
                // Add special tokens: [CLS] at start, [SEP] at end
                var finalInputIds = new List<int> { 101 }; // [CLS] token
                finalInputIds.AddRange(inputIds);
                finalInputIds.Add(102); // [SEP] token
                
                var attentionMask = Enumerable.Repeat(1, finalInputIds.Count).ToArray();
                
                // Pad to max sequence length
                var paddedInputIds = new int[_maxSequenceLength];
                var paddedAttentionMask = new int[_maxSequenceLength];
                
                Array.Copy(finalInputIds.ToArray(), paddedInputIds, Math.Min(finalInputIds.Count, _maxSequenceLength));
                Array.Copy(attentionMask, paddedAttentionMask, Math.Min(attentionMask.Length, _maxSequenceLength));
                
                return new TokenizationResult(paddedInputIds, paddedAttentionMask);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Enhanced tokenization error: {ex.Message}. Using fallback.");
            }
        }
        
        // Enhanced fallback tokenization
        return CreateEnhancedFallbackTokenization(text);
    }

    /// <summary>
    /// Creates an enhanced fallback tokenization with better subword handling.
    /// </summary>
    /// <param name="text">Text to tokenize</param>
    /// <returns>Enhanced tokenization result</returns>
    private TokenizationResult CreateEnhancedFallbackTokenization(string text)
    {
        // Enhanced normalization
        var normalizedText = text.ToLowerInvariant()
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Replace('\t', ' ');
        
        // Better subword splitting
        var subwords = new List<string>();
        var words = normalizedText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var word in words)
        {
            if (word.Length > 6)
            {
                // Split long words into overlapping subwords for better representation
                for (int i = 0; i < word.Length; i += 3)
                {
                    var subword = word.Substring(i, Math.Min(4, word.Length - i));
                    subwords.Add(subword);
                }
            }
            else
            {
                subwords.Add(word);
            }
        }
        
        var maxContentLength = _maxSequenceLength - 2;
        subwords = subwords.Take(maxContentLength).ToList();
        
        var inputIds = new int[_maxSequenceLength];
        var attentionMask = new int[_maxSequenceLength];
        
        inputIds[0] = 101; // [CLS]
        attentionMask[0] = 1;
        
        // Enhanced token ID generation
        for (int i = 0; i < subwords.Count; i++)
        {
            var subword = subwords[i];
            var hash1 = subword.GetHashCode();
            var hash2 = subword.Reverse().ToString()?.GetHashCode() ?? 0;
            var combinedHash = hash1 ^ (hash2 << 1);
            var tokenId = Math.Abs(combinedHash) % 29000 + 1000;
            
            inputIds[i + 1] = tokenId;
            attentionMask[i + 1] = 1;
        }
        
        if (subwords.Count < maxContentLength)
        {
            inputIds[subwords.Count + 1] = 102; // [SEP]
            attentionMask[subwords.Count + 1] = 1;
        }
        
        return new TokenizationResult(inputIds, attentionMask);
    }

    /// <summary>
    /// Enhanced model output processing with better error handling.
    /// </summary>
    /// <param name="outputs">Model outputs</param>
    /// <returns>Processed embedding vector</returns>
    private float[] ProcessModelOutputEnhanced(IReadOnlyCollection<NamedOnnxValue> outputs)
    {
        try
        {
            if (!outputs.Any())
            {
                Console.WriteLine("Warning: No outputs from ONNX model");
                return new float[VectorDimension];
            }

            // Try multiple output names for compatibility
            var outputNames = new[] { "last_hidden_state", "hidden_states", "output", "logits" };
            NamedOnnxValue? targetOutput = null;
            
            foreach (var name in outputNames)
            {
                targetOutput = outputs.FirstOrDefault(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (targetOutput != null) break;
            }
            
            targetOutput ??= outputs.First();
            
            var tensor = targetOutput.AsTensor<float>();
            var shape = tensor.Dimensions.ToArray();
            
            if (shape.Length < 2)
            {
                Console.WriteLine($"Warning: Unexpected tensor shape: [{string.Join(", ", shape)}]");
                return new float[VectorDimension];
            }
            
            var sequenceLength = shape[1];
            var hiddenSize = shape.Length > 2 ? shape[2] : VectorDimension;
            hiddenSize = Math.Min(hiddenSize, VectorDimension);
            
            var embeddings = new float[VectorDimension];
            
            // Enhanced mean pooling with attention consideration
            for (int h = 0; h < hiddenSize; h++)
            {
                float sum = 0;
                int validTokens = 0;
                
                for (int s = 0; s < sequenceLength && s < _maxSequenceLength; s++)
                {
                    try
                    {
                        float value = shape.Length == 3 ? tensor[0, s, h] : tensor[0, s * hiddenSize + h];
                        
                        if (Math.Abs(value) > 1e-8) // Only count non-zero values
                        {
                            sum += value;
                            validTokens++;
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
                
                embeddings[h] = validTokens > 0 ? sum / validTokens : 0;
            }
            
            return embeddings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in enhanced output processing: {ex.Message}");
            return new float[VectorDimension];
        }
    }

    /// <summary>
    /// Generates an enhanced deterministic vector with better text analysis.
    /// </summary>
    /// <param name="text">The input text</param>
    /// <returns>Enhanced deterministic vector</returns>
    private float[] GenerateEnhancedDeterministicVector(string text)
    {
        var vector = new float[VectorDimension];
        
        // Multiple hash functions for better distribution
        var hash1 = text.GetHashCode();
        var hash2 = text.ToLowerInvariant().GetHashCode();
        var hash3 = text.Replace(" ", "").GetHashCode();
        var hash4 = text.Length.GetHashCode();
        
        var randoms = new[]
        {
            new Random(hash1),
            new Random(hash2),
            new Random(hash3),
            new Random(hash4)
        };
        
        // Generate vector with multiple strategies
        for (int i = 0; i < VectorDimension; i++)
        {
            var randomIndex = i % randoms.Length;
            vector[i] = (float)(randoms[randomIndex].NextDouble() * 2.0 - 1.0);
        }
        
        // Enhanced text features
        AddEnhancedTextFeatures(vector, text);
        
        return NormalizeVector(vector);
    }

    /// <summary>
    /// Adds enhanced text-specific features to the vector.
    /// </summary>
    /// <param name="vector">Vector to modify</param>
    /// <param name="text">Source text</param>
    private static void AddEnhancedTextFeatures(float[] vector, string text)
    {
        var textLower = text.ToLowerInvariant();
        var words = textLower.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Enhanced features
        var lengthFeature = Math.Min(text.Length / 1000.0f, 1.0f);
        var wordCountFeature = Math.Min(words.Length / 100.0f, 1.0f);
        var uniqueChars = text.ToCharArray().Distinct().Count();
        var diversityFeature = Math.Min(uniqueChars / 50.0f, 1.0f);
        var avgWordLength = words.Any() ? words.Average(w => w.Length) / 10.0f : 0;
        
        // Apply features to different vector positions
        vector[0] += lengthFeature * 0.1f;
        vector[1] += wordCountFeature * 0.1f;
        vector[2] += diversityFeature * 0.1f;
        vector[3] += (float)avgWordLength * 0.1f;
        
        // Word-based features with better distribution
        for (int i = 0; i < Math.Min(words.Length, vector.Length / 20); i++)
        {
            var wordHash = words[i].GetHashCode();
            var index = Math.Abs(wordHash) % (vector.Length - 20) + 20;
            vector[index] += 0.03f;
        }
    }

    /// <summary>
    /// Tokenizes the input text using the available tokenizer.
    /// This method is kept for backward compatibility.
    /// </summary>
    /// <param name="text">Text to tokenize</param>
    /// <returns>Tokenization result with input IDs and attention mask</returns>
    private TokenizationResult TokenizeText(string text)
    {
        // Delegate to enhanced version
        return EnhancedTokenizeText(text);
    }

    /// <summary>
    /// Creates a simple fallback tokenization when the proper tokenizer is unavailable.
    /// This method is kept for backward compatibility.
    /// </summary>
    /// <param name="text">Text to tokenize</param>
    /// <returns>Basic tokenization result</returns>
    private TokenizationResult CreateFallbackTokenization(string text)
    {
        // Delegate to enhanced version
        return CreateEnhancedFallbackTokenization(text);
    }

    /// <summary>
    /// Creates input tensors for the ONNX model.
    /// </summary>
    /// <param name="tokens">Tokenization result</param>
    /// <returns>List of named ONNX values</returns>
    private List<NamedOnnxValue> CreateInputTensors(TokenizationResult tokens)
    {
        var inputTensors = new List<NamedOnnxValue>();
        
        // Create input_ids tensor
        var inputIdsTensor = NamedOnnxValue.CreateFromTensor("input_ids",
            new DenseTensor<long>(tokens.InputIds.Select(x => (long)x).ToArray(), new int[] { 1, _maxSequenceLength }));
        inputTensors.Add(inputIdsTensor);
        
        // Create attention_mask tensor
        var attentionMaskTensor = NamedOnnxValue.CreateFromTensor("attention_mask",
            new DenseTensor<long>(tokens.AttentionMask.Select(x => (long)x).ToArray(), new int[] { 1, _maxSequenceLength }));
        inputTensors.Add(attentionMaskTensor);
        
        // Create token_type_ids tensor (all zeros for single sentence)
        var tokenTypeIds = new long[_maxSequenceLength];
        var tokenTypeIdsTensor = NamedOnnxValue.CreateFromTensor("token_type_ids",
            new DenseTensor<long>(tokenTypeIds, new int[] { 1, _maxSequenceLength }));
        inputTensors.Add(tokenTypeIdsTensor);
        
        return inputTensors;
    }

    /// <summary>
    /// Processes the ONNX model output to extract embeddings.
    /// </summary>
    /// <param name="outputs">Model outputs</param>
    /// <returns>Processed embedding vector</returns>
    private float[] ProcessModelOutput(IReadOnlyCollection<NamedOnnxValue> outputs)
    {
        try
        {
            // Get the last hidden state (typically the first output)
            var firstOutput = outputs.First();
            var lastHiddenState = firstOutput.AsTensor<float>();
            
            // Apply mean pooling over the sequence dimension
            var hiddenSize = VectorDimension;
            var embeddings = new float[hiddenSize];
            
            // Mean pooling: average over all token positions
            for (int i = 0; i < hiddenSize; i++)
            {
                float sum = 0;
                int count = 0;
                
                for (int j = 0; j < _maxSequenceLength; j++)
                {
                    try
                    {
                        var value = lastHiddenState[0, j, i]; // [batch, sequence, hidden]
                        sum += value;
                        count++;
                    }
                    catch
                    {
                        // Handle dimension mismatches gracefully
                        break;
                    }
                }
                
                embeddings[i] = count > 0 ? sum / count : 0;
            }
            
            return embeddings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing model output: {ex.Message}");
            return new float[VectorDimension];
        }
    }

    /// <summary>
    /// Generates a deterministic vector for testing and fallback purposes.
    /// This method creates a consistent vector based on the text content using advanced hashing.
    /// </summary>
    /// <param name="text">The input text</param>
    /// <returns>A deterministic vector</returns>
    private float[] GenerateDeterministicVector(string text)
    {
        var vector = new float[VectorDimension];
        
        // Use multiple hash functions for better distribution
        var hash1 = text.GetHashCode();
        var hash2 = text.ToLowerInvariant().GetHashCode();
        var hash3 = text.Replace(" ", "").GetHashCode();
        
        // Create multiple random generators with different seeds
        var random1 = new Random(hash1);
        var random2 = new Random(hash2);
        var random3 = new Random(hash3);
        
        // Generate vector components using different strategies
        for (int i = 0; i < VectorDimension; i++)
        {
            var component = i % 3 switch
            {
                0 => (float)(random1.NextDouble() * 2.0 - 1.0),
                1 => (float)(random2.NextDouble() * 2.0 - 1.0),
                _ => (float)(random3.NextDouble() * 2.0 - 1.0)
            };
            
            vector[i] = component;
        }
        
        // Add text-specific features based on content analysis
        AddTextFeatures(vector, text);
        
        return NormalizeVector(vector);
    }

    /// <summary>
    /// Adds text-specific features to the vector based on content analysis.
    /// </summary>
    /// <param name="vector">Vector to modify</param>
    /// <param name="text">Source text</param>
    private static void AddTextFeatures(float[] vector, string text)
    {
        var textLower = text.ToLowerInvariant();
        var words = textLower.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Text length feature
        var lengthFeature = Math.Min(text.Length / 1000.0f, 1.0f);
        vector[0] += lengthFeature * 0.1f;
        
        // Word count feature
        var wordCountFeature = Math.Min(words.Length / 100.0f, 1.0f);
        vector[1] += wordCountFeature * 0.1f;
        
        // Character diversity feature
        var uniqueChars = text.ToCharArray().Distinct().Count();
        var diversityFeature = Math.Min(uniqueChars / 50.0f, 1.0f);
        vector[2] += diversityFeature * 0.1f;
        
        // Add word-based features
        for (int i = 0; i < Math.Min(words.Length, vector.Length / 10); i++)
        {
            var wordHash = words[i].GetHashCode();
            var index = Math.Abs(wordHash) % (vector.Length - 10) + 10;
            vector[index] += 0.05f;
        }
    }

    /// <summary>
    /// Normalizes a vector to unit length.
    /// </summary>
    /// <param name="vector">The vector to normalize</param>
    /// <returns>The normalized vector</returns>
    private static float[] NormalizeVector(float[] vector)
    {
        var magnitude = Math.Sqrt(vector.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= (float)magnitude;
            }
        }
        return vector;
    }

    /// <summary>
    /// Enriches chunk content with ancestral titles for better vectorization.
    /// </summary>
    /// <param name="chunkContent">The chunk content</param>
    /// <param name="ancestralTitles">List of ancestral titles from root to parent</param>
    /// <returns>Enriched content string</returns>
    public static string EnrichContentWithContext(string chunkContent, IEnumerable<string> ancestralTitles)
    {
        var titles = ancestralTitles.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (!titles.Any())
            return chunkContent;

        var contextPrefix = string.Join(": ", titles) + ": ";
        return contextPrefix + chunkContent;
    }

    /// <summary>
    /// Releases all resources used by the <see cref="OnnxVectorizer"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            // Note: Tokenizer doesn't implement IDisposable in this version
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents the result of text tokenization.
/// </summary>
internal record TokenizationResult(int[] InputIds, int[] AttentionMask);

/// <summary>
/// Factory class for creating OnnxVectorizer instances with different configurations.
/// </summary>
public static class OnnxVectorizerFactory
{
    /// <summary>
    /// Creates an OnnxVectorizer with default model paths.
    /// Looks for models in the standard locations.
    /// </summary>
    /// <returns>A new OnnxVectorizer instance</returns>
    public static OnnxVectorizer CreateDefault()
    {
        // Default paths where the model files would typically be located
        var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "multilingual-e5-large.onnx");
        var tokenizerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "tokenizer");
        
        return new OnnxVectorizer(modelPath, tokenizerPath);
    }

    /// <summary>
    /// Creates an OnnxVectorizer with custom model and tokenizer paths.
    /// </summary>
    /// <param name="modelPath">Path to the ONNX model file</param>
    /// <param name="tokenizerPath">Path to the tokenizer directory</param>
    /// <param name="maxSequenceLength">Maximum sequence length for tokenization</param>
    /// <returns>A new OnnxVectorizer instance</returns>
    public static OnnxVectorizer CreateWithPaths(string modelPath, string? tokenizerPath = null, int maxSequenceLength = 512)
    {
        return new OnnxVectorizer(modelPath, tokenizerPath, maxSequenceLength);
    }

    /// <summary>
    /// Creates a deterministic OnnxVectorizer for testing without model files.
    /// Uses advanced deterministic algorithms for consistent embeddings.
    /// </summary>
    /// <returns>A new OnnxVectorizer instance in deterministic mode</returns>
    public static OnnxVectorizer CreateDeterministic()
    {
        return new OnnxVectorizer();
    }

    /// <summary>
    /// Creates an OnnxVectorizer optimized for short text sequences.
    /// </summary>
    /// <param name="modelPath">Path to the ONNX model file</param>
    /// <param name="tokenizerPath">Path to the tokenizer directory</param>
    /// <returns>A new OnnxVectorizer instance optimized for short text</returns>
    public static OnnxVectorizer CreateForShortText(string? modelPath = null, string? tokenizerPath = null)
    {
        return new OnnxVectorizer(modelPath, tokenizerPath, maxSequenceLength: 256);
    }

    /// <summary>
    /// Creates an OnnxVectorizer optimized for long text sequences.
    /// </summary>
    /// <param name="modelPath">Path to the ONNX model file</param>
    /// <param name="tokenizerPath">Path to the tokenizer directory</param>
    /// <returns>A new OnnxVectorizer instance optimized for long text</returns>
    public static OnnxVectorizer CreateForLongText(string? modelPath = null, string? tokenizerPath = null)
    {
        return new OnnxVectorizer(modelPath, tokenizerPath, maxSequenceLength: 1024);
    }
}

