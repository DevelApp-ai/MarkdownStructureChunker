# ONNX Vectorizer Setup Guide

This guide shows how to set up the ONNX vectorizer with real semantic embeddings using the multilingual-e5-large model. The setup works on **CPU-only Linux containers** - no GPU required.

## 🚀 Quick Start

### Option 1: Automated Script (Recommended)
```bash
# Download and run the setup script
curl -sSL https://raw.githubusercontent.com/DevelApp-ai/MarkdownStructureChunker/main/docs/onnx-setup/setup-onnx.sh | bash

# Or clone and run locally
git clone https://github.com/DevelApp-ai/MarkdownStructureChunker.git
cd MarkdownStructureChunker/docs/onnx-setup
chmod +x setup-onnx.sh
./setup-onnx.sh
```

### Option 2: Manual Setup
Follow the [Manual Setup Guide](./manual-setup.md) for step-by-step instructions.

## 📋 What You Get

After setup, you'll have:
- ✅ **multilingual-e5-large ONNX model** (~1.1GB)
- ✅ **Tokenizer configuration** files
- ✅ **Ready-to-use directory structure**
- ✅ **CPU-optimized configuration**

## 🐳 Container Usage

### Docker Example
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install dependencies for ONNX model download
RUN apt-get update && apt-get install -y curl wget python3 python3-pip git-lfs

# Download ONNX models during build
COPY docs/onnx-setup/setup-onnx.sh /tmp/
RUN chmod +x /tmp/setup-onnx.sh && /tmp/setup-onnx.sh

# Copy your application
COPY . /app
WORKDIR /app

# Your app will now have access to ONNX models
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

### Kubernetes ConfigMap
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: onnx-models
data:
  model-path: "/app/models/multilingual-e5-large"
  tokenizer-path: "/app/models/multilingual-e5-large/tokenizer.json"
```

## 💻 Usage in Code

### Basic Usage
```csharp
using MarkdownStructureChunker.Core.Vectorizers;

// Automatic model detection (looks in standard paths)
using var vectorizer = OnnxVectorizer.CreateDefault();
var vector = await vectorizer.VectorizeAsync("Your text here");
```

### Custom Paths
```csharp
// Specify custom model paths
var vectorizer = OnnxVectorizer.CreateWithPaths(
    modelPath: "/app/models/multilingual-e5-large/model.onnx",
    tokenizerPath: "/app/models/multilingual-e5-large/tokenizer.json"
);
var vector = await vectorizer.VectorizeAsync("Your text here");
```

### Query vs Passage Mode
```csharp
// For search queries
var queryVector = await vectorizer.VectorizeAsync("What is machine learning?", isQuery: true);

// For documents/passages
var docVector = await vectorizer.VectorizeAsync("Machine learning is...", isQuery: false);
```

## 🔧 Configuration Options

### Environment Variables
```bash
# Override default model paths
export ONNX_MODEL_PATH="/custom/path/to/model.onnx"
export ONNX_TOKENIZER_PATH="/custom/path/to/tokenizer.json"

# Performance tuning
export ONNX_INTER_OP_THREADS=4
export ONNX_INTRA_OP_THREADS=4
```

### Factory Methods
```csharp
// For short text (faster, 256 tokens max)
var shortTextVectorizer = OnnxVectorizer.CreateForShortText();

// For long text (slower, 1024 tokens max)
var longTextVectorizer = OnnxVectorizer.CreateForLongText();

// Deterministic mode (no model files needed)
var deterministicVectorizer = OnnxVectorizer.CreateDeterministic();
```

## 📊 Performance Characteristics

### CPU Performance (Linux Container)
- **Model Loading**: ~2-3 seconds (one-time)
- **Short Text (< 100 words)**: ~50-100ms per text
- **Long Text (< 500 words)**: ~200-500ms per text
- **Memory Usage**: ~2GB RAM (model + runtime)

### Optimization Tips
1. **Reuse vectorizer instances** - model loading is expensive
2. **Batch processing** - process multiple texts in sequence
3. **Container warm-up** - load model during container startup
4. **Memory limits** - allocate at least 3GB RAM for containers

## 🐛 Troubleshooting

### Common Issues

**Model not found:**
```
ONNX model not found. Using deterministic fallback implementation.
```
**Solution:** Run the setup script or verify model paths.

**Out of memory:**
```
System.OutOfMemoryException during model loading
```
**Solution:** Increase container memory limit to 3GB+.

**Slow performance:**
```
Vectorization taking > 1 second per text
```
**Solution:** Check CPU allocation and consider using short text mode.

### Debug Mode
```csharp
// Enable detailed logging
var vectorizer = OnnxVectorizer.CreateWithPaths(
    modelPath: "path/to/model.onnx",
    tokenizerPath: "path/to/tokenizer.json",
    enableLogging: true
);
```

## 📁 Directory Structure

After setup:
```
models/
└── multilingual-e5-large/
    ├── model.onnx              # Main ONNX model (~1.1GB)
    ├── tokenizer.json          # Tokenizer configuration
    ├── config.json             # Model configuration
    ├── special_tokens_map.json # Special tokens
    └── tokenizer_config.json   # Tokenizer settings
```

## 🔗 Related Links

- [Hugging Face Model Page](https://huggingface.co/intfloat/multilingual-e5-large)
- [ONNX Runtime Documentation](https://onnxruntime.ai/docs/)
- [Microsoft.ML.Tokenizers](https://www.nuget.org/packages/Microsoft.ML.Tokenizers/)
- [Container Deployment Examples](./container-examples.md)

## 📝 License

The multilingual-e5-large model is licensed under MIT License. See the [model page](https://huggingface.co/intfloat/multilingual-e5-large) for details.

