# Manual ONNX Setup Guide

This guide provides step-by-step instructions for manually setting up the ONNX vectorizer with the multilingual-e5-large model.

## 📋 Prerequisites

### System Requirements
- **Operating System**: Linux, macOS, or Windows
- **CPU**: x64 architecture (ARM64 supported with limitations)
- **Memory**: Minimum 3GB RAM available
- **Storage**: 2GB free space for model files
- **Network**: Internet connection for downloading models

### Software Dependencies
- **Python 3.7+** with pip
- **curl** or **wget** for downloading
- **.NET 8.0** runtime (for your application)

## 🔧 Step 1: Install Dependencies

### Ubuntu/Debian
```bash
sudo apt-get update
sudo apt-get install -y curl wget python3 python3-pip
```

### CentOS/RHEL/Fedora
```bash
sudo yum install -y curl wget python3 python3-pip
# or for newer versions:
sudo dnf install -y curl wget python3 python3-pip
```

### Alpine Linux
```bash
sudo apk add --no-cache curl wget python3 py3-pip
```

### macOS
```bash
# Using Homebrew
brew install python3 curl wget

# Using MacPorts
sudo port install python39 curl wget
```

### Windows
```powershell
# Using Chocolatey
choco install python3 curl wget

# Or download Python from python.org
# and install curl/wget manually
```

## 📦 Step 2: Install Python Dependencies

```bash
# Install Hugging Face Hub library
python3 -m pip install --user huggingface_hub

# Verify installation
python3 -c "import huggingface_hub; print('✓ huggingface_hub installed')"
```

## 📁 Step 3: Create Directory Structure

```bash
# Create models directory
mkdir -p models/multilingual-e5-large
cd models/multilingual-e5-large

# Set permissions (Linux/macOS)
chmod 755 .
```

## ⬇️ Step 4: Download Model Files

### Method 1: Using Hugging Face Hub (Recommended)

```python
# Create download script: download_model.py
cat > download_model.py << 'EOF'
#!/usr/bin/env python3
import os
from huggingface_hub import hf_hub_download

def download_model():
    repo_id = "intfloat/multilingual-e5-large"
    
    files_to_download = [
        "onnx/model.onnx",
        "tokenizer.json",
        "config.json",
        "special_tokens_map.json",
        "tokenizer_config.json"
    ]
    
    print("Downloading multilingual-e5-large ONNX model...")
    
    for file in files_to_download:
        try:
            print(f"Downloading {file}...")
            local_path = hf_hub_download(
                repo_id=repo_id,
                filename=file,
                local_dir=".",
                local_dir_use_symlinks=False
            )
            print(f"✓ Downloaded: {local_path}")
        except Exception as e:
            print(f"✗ Failed to download {file}: {e}")
            if file == "onnx/model.onnx":
                print("Model file is required!")
                return False
    
    # Move ONNX model to root level
    if os.path.exists("onnx/model.onnx"):
        os.rename("onnx/model.onnx", "model.onnx")
        os.rmdir("onnx")
    
    print("✓ Download completed successfully!")
    return True

if __name__ == "__main__":
    success = download_model()
    exit(0 if success else 1)
EOF

# Run the download script
python3 download_model.py
```

### Method 2: Direct Download (Alternative)

```bash
# Download model files directly
echo "Downloading ONNX model..."
curl -L -o model.onnx "https://huggingface.co/intfloat/multilingual-e5-large/resolve/main/onnx/model.onnx"

echo "Downloading tokenizer..."
curl -L -o tokenizer.json "https://huggingface.co/intfloat/multilingual-e5-large/resolve/main/tokenizer.json"

echo "Downloading config files..."
curl -L -o config.json "https://huggingface.co/intfloat/multilingual-e5-large/resolve/main/config.json"
curl -L -o special_tokens_map.json "https://huggingface.co/intfloat/multilingual-e5-large/resolve/main/special_tokens_map.json"
curl -L -o tokenizer_config.json "https://huggingface.co/intfloat/multilingual-e5-large/resolve/main/tokenizer_config.json"
```

### Method 3: Using Git LFS

```bash
# Install git-lfs if not available
git lfs install

# Clone the repository
git clone https://huggingface.co/intfloat/multilingual-e5-large temp_repo

# Copy required files
cp temp_repo/onnx/model.onnx .
cp temp_repo/tokenizer.json .
cp temp_repo/config.json .
cp temp_repo/special_tokens_map.json .
cp temp_repo/tokenizer_config.json .

# Clean up
rm -rf temp_repo
```

## ✅ Step 5: Verify Download

```bash
# Check file sizes and existence
echo "Verifying downloaded files..."

# Required files
required_files=("model.onnx" "tokenizer.json")
optional_files=("config.json" "special_tokens_map.json" "tokenizer_config.json")

for file in "${required_files[@]}"; do
    if [ -f "$file" ]; then
        size=$(du -h "$file" | cut -f1)
        echo "✓ $file: $size"
    else
        echo "✗ Missing required file: $file"
        exit 1
    fi
done

for file in "${optional_files[@]}"; do
    if [ -f "$file" ]; then
        size=$(du -h "$file" | cut -f1)
        echo "✓ $file: $size"
    else
        echo "⚠ Optional file missing: $file"
    fi
done

echo "✓ File verification completed"
```

## 🔧 Step 6: Create Configuration

```bash
# Create configuration file
cat > onnx-config.json << EOF
{
    "model_path": "$(pwd)/model.onnx",
    "tokenizer_path": "$(pwd)/tokenizer.json",
    "max_sequence_length": 512,
    "model_type": "multilingual-e5-large",
    "cpu_optimized": true,
    "created_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "setup_version": "1.0.0"
}
EOF

echo "✓ Configuration file created"
```

## 🧪 Step 7: Test Installation

```python
# Create test script: test_installation.py
cat > test_installation.py << 'EOF'
#!/usr/bin/env python3
import os
import json

def test_installation():
    print("Testing ONNX installation...")
    
    # Check required files
    required_files = {
        "model.onnx": 1000000000,  # ~1GB minimum
        "tokenizer.json": 1000,    # ~1KB minimum
    }
    
    for file, min_size in required_files.items():
        if not os.path.exists(file):
            print(f"✗ Missing file: {file}")
            return False
        
        size = os.path.getsize(file)
        if size < min_size:
            print(f"✗ File too small: {file} ({size} bytes)")
            return False
        
        print(f"✓ {file}: {size:,} bytes")
    
    # Test configuration
    if os.path.exists("onnx-config.json"):
        try:
            with open("onnx-config.json", "r") as f:
                config = json.load(f)
            print(f"✓ Configuration: {config['model_type']}")
        except Exception as e:
            print(f"⚠ Configuration warning: {e}")
    
    print("✓ Installation test passed!")
    return True

if __name__ == "__main__":
    success = test_installation()
    exit(0 if success else 1)
EOF

# Run the test
python3 test_installation.py
```

## 📝 Step 8: Set Environment Variables

```bash
# Add to your shell profile (.bashrc, .zshrc, etc.)
export ONNX_MODEL_PATH="$(pwd)/model.onnx"
export ONNX_TOKENIZER_PATH="$(pwd)/tokenizer.json"

# For current session
echo "export ONNX_MODEL_PATH=\"$(pwd)/model.onnx\"" >> ~/.bashrc
echo "export ONNX_TOKENIZER_PATH=\"$(pwd)/tokenizer.json\"" >> ~/.bashrc

# Reload shell configuration
source ~/.bashrc

# Verify environment variables
echo "Model path: $ONNX_MODEL_PATH"
echo "Tokenizer path: $ONNX_TOKENIZER_PATH"
```

## 💻 Step 9: Usage in Your Application

### Basic Usage
```csharp
using MarkdownStructureChunker.Core.Vectorizers;

// Using environment variables
var vectorizer = OnnxVectorizer.CreateDefault();

// Or specify paths directly
var vectorizer = OnnxVectorizer.CreateWithPaths(
    modelPath: "/path/to/models/multilingual-e5-large/model.onnx",
    tokenizerPath: "/path/to/models/multilingual-e5-large/tokenizer.json"
);

// Vectorize text
var vector = await vectorizer.VectorizeAsync("Your text here");
Console.WriteLine($"Vector dimension: {vector.Length}");
```

### Configuration in appsettings.json
```json
{
  "OnnxVectorizer": {
    "ModelPath": "/path/to/models/multilingual-e5-large/model.onnx",
    "TokenizerPath": "/path/to/models/multilingual-e5-large/tokenizer.json",
    "MaxSequenceLength": 512,
    "InterOpThreads": 4,
    "IntraOpThreads": 4
  }
}
```

## 🔧 Step 10: Performance Optimization

### System Optimization
```bash
# Set CPU affinity (Linux)
export OMP_NUM_THREADS=4
export ONNX_INTER_OP_THREADS=4
export ONNX_INTRA_OP_THREADS=4

# Memory optimization
export ONNX_ENABLE_MEMORY_PATTERN=1
export ONNX_ENABLE_CPU_MEM_ARENA=1

# For production systems
echo 'vm.swappiness=10' | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

### Application Configuration
```csharp
// Optimize for your use case
var vectorizer = OnnxVectorizer.CreateWithPaths(
    modelPath: modelPath,
    tokenizerPath: tokenizerPath,
    maxSequenceLength: 256,  // Shorter for better performance
    interOpThreads: 4,       // Match your CPU cores
    intraOpThreads: 4        // Match your CPU cores
);
```

## 🐛 Troubleshooting

### Common Issues

**File not found errors:**
```bash
# Check file permissions
ls -la model.onnx tokenizer.json

# Fix permissions if needed
chmod 644 model.onnx tokenizer.json
```

**Download failures:**
```bash
# Check internet connectivity
curl -I https://huggingface.co

# Try alternative download method
wget --spider https://huggingface.co/intfloat/multilingual-e5-large/resolve/main/onnx/model.onnx
```

**Memory issues:**
```bash
# Check available memory
free -h

# Monitor memory usage during model loading
top -p $(pgrep -f "your-app")
```

**Performance issues:**
```bash
# Check CPU usage
htop

# Verify thread settings
echo "OMP_NUM_THREADS: $OMP_NUM_THREADS"
echo "ONNX_INTER_OP_THREADS: $ONNX_INTER_OP_THREADS"
```

## 📁 Final Directory Structure

After successful setup:
```
models/multilingual-e5-large/
├── model.onnx                 # Main ONNX model (~1.1GB)
├── tokenizer.json             # Tokenizer configuration
├── config.json                # Model configuration
├── special_tokens_map.json    # Special tokens mapping
├── tokenizer_config.json      # Tokenizer settings
├── onnx-config.json          # Setup configuration
├── download_model.py         # Download script
└── test_installation.py      # Test script
```

## 🔗 Next Steps

1. **Test your application** with the ONNX vectorizer
2. **Monitor performance** and adjust thread settings
3. **Set up monitoring** for production deployments
4. **Consider containerization** for easier deployment

For container deployment, see the [Container Examples](./container-examples.md) guide.

