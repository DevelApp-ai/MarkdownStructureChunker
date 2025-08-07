# Podman Examples for ONNX Setup

This guide provides comprehensive examples for deploying the MarkdownStructureChunker with ONNX models using Podman. Podman offers several advantages over Docker, including rootless containers, better security, and systemd integration.

## 🔧 Podman Installation

### RHEL/CentOS/Fedora
```bash
# Fedora
sudo dnf install podman

# RHEL/CentOS 8+
sudo dnf install podman

# CentOS 7
sudo yum install podman
```

### Ubuntu/Debian
```bash
# Ubuntu 20.04+
sudo apt-get update
sudo apt-get install podman

# For older versions, add repository
echo "deb https://download.opensuse.org/repositories/devel:/kubic:/libcontainers:/stable/xUbuntu_$(lsb_release -rs)/ /" | sudo tee /etc/apt/sources.list.d/devel:kubic:libcontainers:stable.list
curl -L "https://download.opensuse.org/repositories/devel:/kubic:/libcontainers:/stable/xUbuntu_$(lsb_release -rs)/Release.key" | sudo apt-key add -
sudo apt-get update
sudo apt-get install podman
```

### macOS
```bash
# Using Homebrew
brew install podman

# Initialize Podman machine
podman machine init
podman machine start
```

## 🐳 Basic Podman Examples

### Building the Container Image

```bash
# Build using the example Dockerfile
podman build -f docs/onnx-setup/Dockerfile.example -t markdown-chunker:latest .

# Build with specific tag
podman build -f docs/onnx-setup/Dockerfile.example -t localhost/markdown-chunker:v1.0.2 .

# Build with build arguments
podman build \
  --build-arg ONNX_MODEL_VERSION=latest \
  --build-arg DOTNET_VERSION=8.0 \
  -f docs/onnx-setup/Dockerfile.example \
  -t markdown-chunker:latest .
```

### Running Containers

```bash
# Basic run
podman run -d \
  --name markdown-chunker \
  -p 8080:80 \
  markdown-chunker:latest

# Run with environment variables
podman run -d \
  --name markdown-chunker \
  -p 8080:80 \
  -e ONNX_INTER_OP_THREADS=4 \
  -e ONNX_INTRA_OP_THREADS=4 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  markdown-chunker:latest

# Run with resource limits
podman run -d \
  --name markdown-chunker \
  -p 8080:80 \
  --memory=4g \
  --cpus=2.0 \
  --memory-swap=4g \
  markdown-chunker:latest

# Run with health check
podman run -d \
  --name markdown-chunker \
  -p 8080:80 \
  --health-cmd="curl -f http://localhost/health || exit 1" \
  --health-interval=30s \
  --health-timeout=10s \
  --health-retries=3 \
  --health-start-period=60s \
  markdown-chunker:latest
```

## 🔒 Rootless Containers

One of Podman's key advantages is rootless container support:

```bash
# Run as non-root user (default in Podman)
podman run -d \
  --name markdown-chunker-rootless \
  -p 8080:80 \
  --user 1000:1000 \
  markdown-chunker:latest

# Check rootless configuration
podman info --format "{{.Host.Security.Rootless}}"

# Configure rootless networking (if needed)
echo 'net.ipv4.ip_unprivileged_port_start=80' | sudo tee -a /etc/sysctl.conf
sudo sysctl -p

# Run on privileged port as rootless
podman run -d \
  --name markdown-chunker-port80 \
  -p 80:80 \
  markdown-chunker:latest
```

## 🏗️ Podman Pods

Podman pods group containers together, similar to Kubernetes pods:

```bash
# Create a pod for the application stack
podman pod create \
  --name markdown-chunker-pod \
  --publish 8080:80 \
  --publish 6379:6379

# Run the main application in the pod
podman run -d \
  --name markdown-chunker-app \
  --pod markdown-chunker-pod \
  -e REDIS_CONNECTION_STRING="localhost:6379" \
  markdown-chunker:latest

# Run Redis in the same pod
podman run -d \
  --name markdown-chunker-redis \
  --pod markdown-chunker-pod \
  redis:7-alpine

# Check pod status
podman pod ps
podman pod logs markdown-chunker-pod

# Stop the entire pod
podman pod stop markdown-chunker-pod

# Remove the pod and all containers
podman pod rm -f markdown-chunker-pod
```

## 📝 Podman Compose (docker-compose alternative)

### Install podman-compose
```bash
# Using pip
pip3 install podman-compose

# Or using package manager (Fedora)
sudo dnf install podman-compose

# Verify installation
podman-compose --version
```

### Using docker-compose.yml with Podman
```bash
# Use the existing docker-compose file
podman-compose -f docs/onnx-setup/docker-compose.example.yml up -d

# Check status
podman-compose -f docs/onnx-setup/docker-compose.example.yml ps

# View logs
podman-compose -f docs/onnx-setup/docker-compose.example.yml logs markdown-chunker

# Scale services
podman-compose -f docs/onnx-setup/docker-compose.example.yml up -d --scale markdown-chunker=3

# Stop services
podman-compose -f docs/onnx-setup/docker-compose.example.yml down
```

### Podman-specific Compose File
```yaml
# podman-compose.yml
version: '3.8'

services:
  markdown-chunker:
    build:
      context: ../..
      dockerfile: docs/onnx-setup/Dockerfile.example
    container_name: markdown-chunker-app
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ONNX_MODEL_PATH=/app/models/multilingual-e5-large/model.onnx
      - ONNX_TOKENIZER_PATH=/app/models/multilingual-e5-large/tokenizer.json
      - ONNX_INTER_OP_THREADS=4
      - ONNX_INTRA_OP_THREADS=4
    
    # Podman-specific security options
    security_opt:
      - "label=type:container_runtime_t"
    
    # Resource limits (Podman format)
    deploy:
      resources:
        limits:
          memory: 4G
          cpus: '2.0'
    
    # Health check
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    container_name: markdown-chunker-redis
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
    volumes:
      - redis-data:/data
    restart: unless-stopped

volumes:
  redis-data:
```

## 🔧 Systemd Integration

Podman integrates excellently with systemd for service management:

### Generate Systemd Service Files
```bash
# Run container first
podman run -d \
  --name markdown-chunker-service \
  -p 8080:80 \
  markdown-chunker:latest

# Generate systemd service file
podman generate systemd \
  --name markdown-chunker-service \
  --files \
  --new

# This creates: container-markdown-chunker-service.service

# Move to systemd directory
sudo mv container-markdown-chunker-service.service /etc/systemd/system/

# For user services (rootless)
mkdir -p ~/.config/systemd/user
mv container-markdown-chunker-service.service ~/.config/systemd/user/

# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable container-markdown-chunker-service.service
sudo systemctl start container-markdown-chunker-service.service

# For user services
systemctl --user daemon-reload
systemctl --user enable container-markdown-chunker-service.service
systemctl --user start container-markdown-chunker-service.service
```

### Custom Systemd Service
```ini
# /etc/systemd/system/markdown-chunker.service
[Unit]
Description=MarkdownStructureChunker with ONNX
After=network-online.target
Wants=network-online.target

[Service]
Type=notify
NotifyAccess=all
ExecStartPre=/usr/bin/podman pull localhost/markdown-chunker:latest
ExecStart=/usr/bin/podman run \
  --rm \
  --name markdown-chunker-systemd \
  -p 8080:80 \
  -e ONNX_INTER_OP_THREADS=4 \
  -e ONNX_INTRA_OP_THREADS=4 \
  localhost/markdown-chunker:latest
ExecStop=/usr/bin/podman stop -t 30 markdown-chunker-systemd
Restart=always
RestartSec=10

# Security settings
User=podman-user
Group=podman-user
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/lib/containers

[Install]
WantedBy=multi-user.target
```

### Pod as Systemd Service
```bash
# Create pod
podman pod create \
  --name markdown-chunker-pod \
  --publish 8080:80

# Generate systemd files for the pod
podman generate systemd \
  --name markdown-chunker-pod \
  --files \
  --new

# Install and enable
sudo mv pod-markdown-chunker-pod.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable pod-markdown-chunker-pod.service
sudo systemctl start pod-markdown-chunker-pod.service
```

## 🚀 Advanced Podman Features

### Container Images and Registries
```bash
# Build and tag for different registries
podman build -t localhost/markdown-chunker:latest .
podman build -t quay.io/yourorg/markdown-chunker:latest .
podman build -t docker.io/yourorg/markdown-chunker:latest .

# Push to registries
podman push localhost/markdown-chunker:latest
podman push quay.io/yourorg/markdown-chunker:latest

# Pull from different registries
podman pull quay.io/yourorg/markdown-chunker:latest
podman pull docker.io/yourorg/markdown-chunker:latest

# List images
podman images

# Remove images
podman rmi markdown-chunker:latest
```

### Volume Management
```bash
# Create named volumes
podman volume create onnx-models
podman volume create app-data

# Run with volumes
podman run -d \
  --name markdown-chunker \
  -p 8080:80 \
  -v onnx-models:/app/models \
  -v app-data:/app/data \
  markdown-chunker:latest

# Inspect volumes
podman volume inspect onnx-models

# Backup volume
podman run --rm \
  -v onnx-models:/source:ro \
  -v $(pwd):/backup \
  alpine tar czf /backup/onnx-models-backup.tar.gz -C /source .

# Restore volume
podman run --rm \
  -v onnx-models:/target \
  -v $(pwd):/backup \
  alpine tar xzf /backup/onnx-models-backup.tar.gz -C /target
```

### Networking
```bash
# Create custom network
podman network create markdown-chunker-net

# Run containers on custom network
podman run -d \
  --name markdown-chunker-app \
  --network markdown-chunker-net \
  -p 8080:80 \
  markdown-chunker:latest

podman run -d \
  --name redis \
  --network markdown-chunker-net \
  redis:7-alpine

# Inspect network
podman network inspect markdown-chunker-net

# Connect running container to network
podman network connect markdown-chunker-net existing-container
```

## 🔍 Monitoring and Debugging

### Container Inspection
```bash
# Inspect running container
podman inspect markdown-chunker

# Check container logs
podman logs -f markdown-chunker

# Execute commands in container
podman exec -it markdown-chunker /bin/bash

# Check container stats
podman stats markdown-chunker

# Check container processes
podman top markdown-chunker
```

### Health Monitoring
```bash
# Check health status
podman healthcheck run markdown-chunker

# View health check logs
podman inspect markdown-chunker --format='{{.State.Health}}'

# Monitor container events
podman events --filter container=markdown-chunker
```

## 🔧 Performance Optimization

### Resource Management
```bash
# Set CPU limits
podman run -d \
  --name markdown-chunker \
  --cpus=2.0 \
  --cpu-shares=1024 \
  markdown-chunker:latest

# Set memory limits
podman run -d \
  --name markdown-chunker \
  --memory=4g \
  --memory-swap=4g \
  --oom-kill-disable \
  markdown-chunker:latest

# Set I/O limits
podman run -d \
  --name markdown-chunker \
  --device-read-bps /dev/sda:1mb \
  --device-write-bps /dev/sda:1mb \
  markdown-chunker:latest
```

### Security Hardening
```bash
# Run with security options
podman run -d \
  --name markdown-chunker \
  --security-opt no-new-privileges \
  --security-opt label=type:container_runtime_t \
  --cap-drop ALL \
  --cap-add NET_BIND_SERVICE \
  --read-only \
  --tmpfs /tmp \
  markdown-chunker:latest

# Use SELinux labels
podman run -d \
  --name markdown-chunker \
  --security-opt label=level:s0:c100,c200 \
  markdown-chunker:latest
```

## 🐛 Troubleshooting

### Common Issues

**Permission denied errors:**
```bash
# Check SELinux context
ls -Z /path/to/volume

# Fix SELinux labels
podman run --rm -v /path/to/volume:/volume:Z alpine chown -R 1000:1000 /volume

# Disable SELinux for testing (not recommended for production)
sudo setenforce 0
```

**Networking issues:**
```bash
# Reset networking
podman system reset --force

# Check firewall
sudo firewall-cmd --list-all
sudo firewall-cmd --add-port=8080/tcp --permanent
sudo firewall-cmd --reload

# Check iptables
sudo iptables -L -n
```

**Storage issues:**
```bash
# Check storage usage
podman system df

# Clean up unused resources
podman system prune -a

# Check storage configuration
podman info --format='{{.Store}}'
```

## 📊 Comparison: Podman vs Docker

| Feature | Podman | Docker |
|---------|--------|--------|
| **Rootless** | ✅ Native support | ⚠️ Experimental |
| **Daemon** | ✅ Daemonless | ❌ Requires daemon |
| **systemd** | ✅ Native integration | ⚠️ Limited |
| **Security** | ✅ Better isolation | ⚠️ Root daemon |
| **Pods** | ✅ Native support | ❌ No native pods |
| **Compatibility** | ✅ Docker CLI compatible | ✅ Native |
| **Kubernetes** | ✅ Pod-compatible | ⚠️ Via Docker Desktop |

## 🚀 Production Deployment

### High Availability Setup
```bash
# Create multiple instances with load balancing
for i in {1..3}; do
  podman run -d \
    --name markdown-chunker-$i \
    -p $((8080 + i)):80 \
    --health-cmd="curl -f http://localhost/health || exit 1" \
    --health-interval=30s \
    markdown-chunker:latest
done

# Use nginx for load balancing
podman run -d \
  --name nginx-lb \
  -p 80:80 \
  -v ./nginx.conf:/etc/nginx/nginx.conf:ro \
  nginx:alpine
```

### Backup and Recovery
```bash
# Backup container configuration
podman inspect markdown-chunker > markdown-chunker-config.json

# Export container as image
podman commit markdown-chunker markdown-chunker-backup:$(date +%Y%m%d)

# Save image to file
podman save -o markdown-chunker-backup.tar markdown-chunker-backup:$(date +%Y%m%d)

# Restore from backup
podman load -i markdown-chunker-backup.tar
podman run -d --name markdown-chunker-restored markdown-chunker-backup:$(date +%Y%m%d)
```

This comprehensive Podman guide provides everything needed to deploy and manage the MarkdownStructureChunker with ONNX models using Podman's advanced features!

