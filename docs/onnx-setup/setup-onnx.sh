#!/bin/bash

# ONNX Model Setup Script for MarkdownStructureChunker
# Downloads and configures multilingual-e5-large ONNX model for CPU inference
# Compatible with Linux containers and development environments

set -euo pipefail

# Configuration
MODEL_NAME="multilingual-e5-large"
MODEL_DIR="${ONNX_MODEL_DIR:-./models/${MODEL_NAME}}"
HUGGINGFACE_REPO="intfloat/multilingual-e5-large"
TEMP_DIR="/tmp/onnx-setup-$$"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Install dependencies
install_dependencies() {
    log_info "Checking dependencies..."
    
    # Check for required commands
    local missing_deps=()
    
    if ! command_exists curl && ! command_exists wget; then
        missing_deps+=("curl or wget")
    fi
    
    if ! command_exists python3; then
        missing_deps+=("python3")
    fi
    
    if [ ${#missing_deps[@]} -ne 0 ]; then
        log_error "Missing dependencies: ${missing_deps[*]}"
        log_info "Installing dependencies..."
        
        # Detect package manager and install
        if command_exists apt-get; then
            sudo apt-get update
            sudo apt-get install -y curl wget python3 python3-pip
        elif command_exists yum; then
            sudo yum install -y curl wget python3 python3-pip
        elif command_exists apk; then
            sudo apk add --no-cache curl wget python3 py3-pip
        else
            log_error "Could not detect package manager. Please install: ${missing_deps[*]}"
            exit 1
        fi
    fi
    
    # Install huggingface_hub if not available
    if ! python3 -c "import huggingface_hub" 2>/dev/null; then
        log_info "Installing huggingface_hub..."
        python3 -m pip install --user huggingface_hub
    fi
    
    log_success "Dependencies ready"
}

# Download file with progress
download_file() {
    local url="$1"
    local output="$2"
    local description="$3"
    
    log_info "Downloading ${description}..."
    
    if command_exists curl; then
        curl -L --progress-bar -o "$output" "$url"
    elif command_exists wget; then
        wget --progress=bar:force -O "$output" "$url"
    else
        log_error "Neither curl nor wget available"
        exit 1
    fi
}

# Download model files using Hugging Face Hub
download_model_files() {
    log_info "Downloading ONNX model files from Hugging Face..."
    
    # Create temporary directory
    mkdir -p "$TEMP_DIR"
    cd "$TEMP_DIR"
    
    # Download using huggingface_hub
    python3 << EOF
import os
from huggingface_hub import hf_hub_download, snapshot_download
import sys

try:
    # Download the ONNX model and tokenizer files
    files_to_download = [
        "onnx/model.onnx",
        "tokenizer.json",
        "config.json",
        "special_tokens_map.json",
        "tokenizer_config.json"
    ]
    
    print("Downloading model files...")
    for file in files_to_download:
        try:
            print(f"Downloading {file}...")
            downloaded_path = hf_hub_download(
                repo_id="${HUGGINGFACE_REPO}",
                filename=file,
                cache_dir="./cache"
            )
            print(f"Downloaded: {downloaded_path}")
        except Exception as e:
            print(f"Warning: Could not download {file}: {e}")
            
    print("Download completed!")
    
except Exception as e:
    print(f"Error downloading model: {e}")
    sys.exit(1)
EOF
    
    if [ $? -ne 0 ]; then
        log_error "Failed to download model files"
        cleanup
        exit 1
    fi
}

# Organize downloaded files
organize_files() {
    log_info "Organizing model files..."
    
    # Create target directory
    mkdir -p "$MODEL_DIR"
    
    # Find and copy files from cache
    local cache_dir="$TEMP_DIR/cache"
    
    # Find the model files in the cache
    if [ -d "$cache_dir" ]; then
        # Copy ONNX model
        find "$cache_dir" -name "model.onnx" -exec cp {} "$MODEL_DIR/" \;
        
        # Copy tokenizer and config files
        find "$cache_dir" -name "tokenizer.json" -exec cp {} "$MODEL_DIR/" \;
        find "$cache_dir" -name "config.json" -exec cp {} "$MODEL_DIR/" \;
        find "$cache_dir" -name "special_tokens_map.json" -exec cp {} "$MODEL_DIR/" \;
        find "$cache_dir" -name "tokenizer_config.json" -exec cp {} "$MODEL_DIR/" \;
    fi
    
    # Verify files exist
    local required_files=("model.onnx" "tokenizer.json")
    for file in "${required_files[@]}"; do
        if [ ! -f "$MODEL_DIR/$file" ]; then
            log_error "Required file not found: $MODEL_DIR/$file"
            return 1
        fi
    done
    
    log_success "Model files organized in $MODEL_DIR"
}

# Create configuration file
create_config() {
    log_info "Creating configuration file..."
    
    cat > "$MODEL_DIR/onnx-config.json" << EOF
{
    "model_path": "$MODEL_DIR/model.onnx",
    "tokenizer_path": "$MODEL_DIR/tokenizer.json",
    "max_sequence_length": 512,
    "model_type": "multilingual-e5-large",
    "cpu_optimized": true,
    "created_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "setup_version": "1.0.0"
}
EOF
    
    log_success "Configuration created"
}

# Verify installation
verify_installation() {
    log_info "Verifying installation..."
    
    # Check file sizes
    local model_size=$(du -h "$MODEL_DIR/model.onnx" 2>/dev/null | cut -f1 || echo "unknown")
    local tokenizer_size=$(du -h "$MODEL_DIR/tokenizer.json" 2>/dev/null | cut -f1 || echo "unknown")
    
    log_info "Model file size: $model_size"
    log_info "Tokenizer file size: $tokenizer_size"
    
    # List all files
    log_info "Installed files:"
    ls -la "$MODEL_DIR" || true
    
    # Create test script
    cat > "$MODEL_DIR/test-setup.py" << 'EOF'
#!/usr/bin/env python3
import json
import os

def test_setup():
    model_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Check required files
    required_files = ["model.onnx", "tokenizer.json", "onnx-config.json"]
    missing_files = []
    
    for file in required_files:
        file_path = os.path.join(model_dir, file)
        if not os.path.exists(file_path):
            missing_files.append(file)
        else:
            size = os.path.getsize(file_path)
            print(f"✓ {file}: {size:,} bytes")
    
    if missing_files:
        print(f"✗ Missing files: {missing_files}")
        return False
    
    # Load and validate config
    try:
        with open(os.path.join(model_dir, "onnx-config.json"), "r") as f:
            config = json.load(f)
        print(f"✓ Configuration loaded: {config['model_type']}")
    except Exception as e:
        print(f"✗ Configuration error: {e}")
        return False
    
    print("✓ ONNX setup verification passed!")
    return True

if __name__ == "__main__":
    success = test_setup()
    exit(0 if success else 1)
EOF
    
    chmod +x "$MODEL_DIR/test-setup.py"
    
    # Run verification
    if python3 "$MODEL_DIR/test-setup.py"; then
        log_success "Installation verified successfully!"
    else
        log_warning "Installation verification failed, but files may still work"
    fi
}

# Cleanup function
cleanup() {
    if [ -d "$TEMP_DIR" ]; then
        log_info "Cleaning up temporary files..."
        rm -rf "$TEMP_DIR"
    fi
}

# Print usage information
print_usage() {
    log_info "ONNX Model Setup Complete!"
    echo
    echo "📁 Model Location: $MODEL_DIR"
    echo "🔧 Configuration: $MODEL_DIR/onnx-config.json"
    echo "🧪 Test Script: $MODEL_DIR/test-setup.py"
    echo
    echo "💻 Usage in C#:"
    echo "var vectorizer = OnnxVectorizer.CreateWithPaths("
    echo "    modelPath: \"$MODEL_DIR/model.onnx\","
    echo "    tokenizerPath: \"$MODEL_DIR/tokenizer.json\""
    echo ");"
    echo
    echo "🐳 For containers, copy the models directory to your image:"
    echo "COPY $MODEL_DIR /app/models/$MODEL_NAME"
    echo
    echo "🔗 Environment variables:"
    echo "export ONNX_MODEL_PATH=\"$MODEL_DIR/model.onnx\""
    echo "export ONNX_TOKENIZER_PATH=\"$MODEL_DIR/tokenizer.json\""
}

# Main execution
main() {
    log_info "Starting ONNX Model Setup for MarkdownStructureChunker"
    log_info "Target directory: $MODEL_DIR"
    
    # Set trap for cleanup
    trap cleanup EXIT
    
    # Check if already installed
    if [ -f "$MODEL_DIR/model.onnx" ] && [ -f "$MODEL_DIR/tokenizer.json" ]; then
        log_warning "ONNX model already exists at $MODEL_DIR"
        read -p "Do you want to reinstall? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            log_info "Skipping installation"
            exit 0
        fi
        rm -rf "$MODEL_DIR"
    fi
    
    # Execute setup steps
    install_dependencies
    download_model_files
    organize_files
    create_config
    verify_installation
    print_usage
    
    log_success "ONNX setup completed successfully!"
}

# Handle command line arguments
case "${1:-}" in
    --help|-h)
        echo "ONNX Model Setup Script"
        echo "Usage: $0 [options]"
        echo "Options:"
        echo "  --help, -h     Show this help message"
        echo "  --verify       Verify existing installation"
        echo "Environment variables:"
        echo "  ONNX_MODEL_DIR Directory to install models (default: ./models/multilingual-e5-large)"
        exit 0
        ;;
    --verify)
        if [ -f "$MODEL_DIR/test-setup.py" ]; then
            python3 "$MODEL_DIR/test-setup.py"
        else
            log_error "No installation found at $MODEL_DIR"
            exit 1
        fi
        exit 0
        ;;
    "")
        main
        ;;
    *)
        log_error "Unknown option: $1"
        echo "Use --help for usage information"
        exit 1
        ;;
esac

