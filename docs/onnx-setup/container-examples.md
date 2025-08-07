# Container Deployment Examples

This document provides complete examples for deploying the MarkdownStructureChunker with ONNX models in various container environments.

## 🐳 Docker Examples

### Basic Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install dependencies for ONNX model download
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    python3 \
    python3-pip \
    && rm -rf /var/lib/apt/lists/*

# Install huggingface_hub
RUN python3 -m pip install huggingface_hub

# Create app directory
WORKDIR /app

# Copy ONNX setup script
COPY docs/onnx-setup/setup-onnx.sh /tmp/setup-onnx.sh
RUN chmod +x /tmp/setup-onnx.sh

# Download ONNX models during build
ENV ONNX_MODEL_DIR=/app/models/multilingual-e5-large
RUN /tmp/setup-onnx.sh && rm /tmp/setup-onnx.sh

# Copy application files
COPY bin/Release/net8.0/publish/ .

# Set environment variables
ENV ONNX_MODEL_PATH=/app/models/multilingual-e5-large/model.onnx
ENV ONNX_TOKENIZER_PATH=/app/models/multilingual-e5-large/tokenizer.json

# Expose port
EXPOSE 80

# Start application
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

### Multi-stage Build (Optimized)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["YourApp/YourApp.csproj", "YourApp/"]
RUN dotnet restore "YourApp/YourApp.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/YourApp"
RUN dotnet build "YourApp.csproj" -c Release -o /app/build
RUN dotnet publish "YourApp.csproj" -c Release -o /app/publish

# ONNX model download stage
FROM python:3.11-slim AS onnx-downloader
RUN pip install huggingface_hub
COPY docs/onnx-setup/setup-onnx.sh /tmp/setup-onnx.sh
RUN chmod +x /tmp/setup-onnx.sh
ENV ONNX_MODEL_DIR=/models/multilingual-e5-large
RUN /tmp/setup-onnx.sh

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy application
COPY --from=build /app/publish .

# Copy ONNX models
COPY --from=onnx-downloader /models /app/models

# Set environment variables
ENV ONNX_MODEL_PATH=/app/models/multilingual-e5-large/model.onnx
ENV ONNX_TOKENIZER_PATH=/app/models/multilingual-e5-large/tokenizer.json

EXPOSE 80
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

### Docker Compose

```yaml
version: '3.8'

services:
  markdown-chunker:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ONNX_MODEL_PATH=/app/models/multilingual-e5-large/model.onnx
      - ONNX_TOKENIZER_PATH=/app/models/multilingual-e5-large/tokenizer.json
      - ONNX_INTER_OP_THREADS=4
      - ONNX_INTRA_OP_THREADS=4
    volumes:
      - onnx-models:/app/models
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  onnx-models:
```

## ☸️ Kubernetes Examples

### Deployment with ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: onnx-config
  namespace: default
data:
  model-path: "/app/models/multilingual-e5-large/model.onnx"
  tokenizer-path: "/app/models/multilingual-e5-large/tokenizer.json"
  inter-op-threads: "4"
  intra-op-threads: "4"

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: markdown-chunker
  namespace: default
spec:
  replicas: 3
  selector:
    matchLabels:
      app: markdown-chunker
  template:
    metadata:
      labels:
        app: markdown-chunker
    spec:
      containers:
      - name: markdown-chunker
        image: your-registry/markdown-chunker:latest
        ports:
        - containerPort: 80
        env:
        - name: ONNX_MODEL_PATH
          valueFrom:
            configMapKeyRef:
              name: onnx-config
              key: model-path
        - name: ONNX_TOKENIZER_PATH
          valueFrom:
            configMapKeyRef:
              name: onnx-config
              key: tokenizer-path
        - name: ONNX_INTER_OP_THREADS
          valueFrom:
            configMapKeyRef:
              name: onnx-config
              key: inter-op-threads
        - name: ONNX_INTRA_OP_THREADS
          valueFrom:
            configMapKeyRef:
              name: onnx-config
              key: intra-op-threads
        resources:
          requests:
            memory: "2Gi"
            cpu: "1000m"
          limits:
            memory: "4Gi"
            cpu: "2000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /ready
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
        volumeMounts:
        - name: onnx-models
          mountPath: /app/models
          readOnly: true
      volumes:
      - name: onnx-models
        persistentVolumeClaim:
          claimName: onnx-models-pvc

---
apiVersion: v1
kind: Service
metadata:
  name: markdown-chunker-service
  namespace: default
spec:
  selector:
    app: markdown-chunker
  ports:
  - protocol: TCP
    port: 80
    targetPort: 80
  type: ClusterIP
```

### Persistent Volume for Models

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: onnx-models-pvc
  namespace: default
spec:
  accessModes:
    - ReadOnlyMany
  resources:
    requests:
      storage: 2Gi
  storageClassName: fast-ssd

---
apiVersion: batch/v1
kind: Job
metadata:
  name: onnx-model-downloader
  namespace: default
spec:
  template:
    spec:
      containers:
      - name: downloader
        image: python:3.11-slim
        command: ["/bin/bash"]
        args:
          - -c
          - |
            pip install huggingface_hub
            curl -sSL https://raw.githubusercontent.com/DevelApp-ai/MarkdownStructureChunker/main/docs/onnx-setup/setup-onnx.sh | bash
            cp -r models/* /shared/
        volumeMounts:
        - name: shared-models
          mountPath: /shared
        env:
        - name: ONNX_MODEL_DIR
          value: "/tmp/models/multilingual-e5-large"
      restartPolicy: OnFailure
      volumes:
      - name: shared-models
        persistentVolumeClaim:
          claimName: onnx-models-pvc
```

## 🚀 Cloud Platform Examples

### Azure Container Instances

```yaml
apiVersion: 2021-07-01
location: eastus
name: markdown-chunker-aci
properties:
  containers:
  - name: markdown-chunker
    properties:
      image: your-registry/markdown-chunker:latest
      resources:
        requests:
          cpu: 2
          memoryInGb: 4
      ports:
      - port: 80
        protocol: TCP
      environmentVariables:
      - name: ONNX_MODEL_PATH
        value: /app/models/multilingual-e5-large/model.onnx
      - name: ONNX_TOKENIZER_PATH
        value: /app/models/multilingual-e5-large/tokenizer.json
      - name: ONNX_INTER_OP_THREADS
        value: "4"
      - name: ONNX_INTRA_OP_THREADS
        value: "4"
  osType: Linux
  restartPolicy: Always
  ipAddress:
    type: Public
    ports:
    - protocol: TCP
      port: 80
tags:
  app: markdown-chunker
  environment: production
```

### AWS ECS Task Definition

```json
{
  "family": "markdown-chunker",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "2048",
  "memory": "4096",
  "executionRoleArn": "arn:aws:iam::account:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "markdown-chunker",
      "image": "your-registry/markdown-chunker:latest",
      "portMappings": [
        {
          "containerPort": 80,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ONNX_MODEL_PATH",
          "value": "/app/models/multilingual-e5-large/model.onnx"
        },
        {
          "name": "ONNX_TOKENIZER_PATH",
          "value": "/app/models/multilingual-e5-large/tokenizer.json"
        },
        {
          "name": "ONNX_INTER_OP_THREADS",
          "value": "4"
        },
        {
          "name": "ONNX_INTRA_OP_THREADS",
          "value": "4"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/markdown-chunker",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      },
      "healthCheck": {
        "command": [
          "CMD-SHELL",
          "curl -f http://localhost/health || exit 1"
        ],
        "interval": 30,
        "timeout": 5,
        "retries": 3,
        "startPeriod": 60
      }
    }
  ]
}
```

## 🔧 Performance Optimization

### Resource Allocation Guidelines

```yaml
# Minimum resources (development)
resources:
  requests:
    memory: "2Gi"
    cpu: "500m"
  limits:
    memory: "3Gi"
    cpu: "1000m"

# Recommended resources (production)
resources:
  requests:
    memory: "3Gi"
    cpu: "1000m"
  limits:
    memory: "6Gi"
    cpu: "2000m"

# High-performance (heavy load)
resources:
  requests:
    memory: "4Gi"
    cpu: "2000m"
  limits:
    memory: "8Gi"
    cpu: "4000m"
```

### Environment Variables for Tuning

```bash
# ONNX Runtime optimization
ONNX_INTER_OP_THREADS=4          # Number of threads for parallel execution
ONNX_INTRA_OP_THREADS=4          # Number of threads within operations
OMP_NUM_THREADS=4                # OpenMP thread count

# Memory optimization
ONNX_ENABLE_MEMORY_PATTERN=1     # Enable memory pattern optimization
ONNX_ENABLE_CPU_MEM_ARENA=1      # Enable CPU memory arena

# Model caching
ONNX_CACHE_MODELS=1              # Cache loaded models
ONNX_MODEL_CACHE_SIZE=1          # Number of models to cache
```

## 🐛 Troubleshooting

### Common Container Issues

**Model loading timeout:**
```dockerfile
# Increase startup timeout
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_SHUTDOWNTIMEOUTSECONDS=30
HEALTHCHECK --start-period=60s CMD curl -f http://localhost/health
```

**Out of memory during startup:**
```yaml
# Increase memory limits
resources:
  limits:
    memory: "6Gi"  # Increase from default
```

**Slow model loading:**
```dockerfile
# Pre-warm the model during build
RUN dotnet YourApp.dll --warm-up-models || true
```

### Monitoring and Logging

```yaml
# Add monitoring sidecar
- name: monitoring
  image: prom/node-exporter
  ports:
  - containerPort: 9100
  
# Enhanced logging
env:
- name: ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT
  value: "Information"
- name: ONNX_ENABLE_LOGGING
  value: "true"
```

## 📊 Performance Benchmarks

### Container Performance (CPU-only)

| Container Size | Memory | CPU | Startup Time | Throughput |
|---------------|--------|-----|--------------|------------|
| Small         | 2GB    | 1 core | ~30s | 10 texts/sec |
| Medium        | 4GB    | 2 cores | ~20s | 25 texts/sec |
| Large         | 6GB    | 4 cores | ~15s | 50 texts/sec |

### Scaling Recommendations

- **Horizontal scaling**: 3-5 replicas for production
- **Vertical scaling**: 2-4 CPU cores, 4-6GB RAM per instance
- **Load balancing**: Round-robin with health checks
- **Auto-scaling**: Based on CPU (70%) and memory (80%) thresholds

