# ONNX Model Setup Instructions

## Download all-MiniLM-L6-v2 ONNX Model

### Option 1: Download from HuggingFace

1. Visit: https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2
2. Click "Files and versions"
3. Download `model.onnx` or search for ONNX version
4. Rename it to `all-MiniLM-L6-v2.onnx`
5. Place it in: `IncidentApp\Models/` directory

### Option 2: Download Direct URL

```bash
# Download the ONNX model
curl -L -o IncidentApp/Models/all-MiniLM-L6-v2.onnx \
  https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/pytorch_model.bin
```

### Option 3: Convert PyTorch to ONNX

If you only have the PyTorch model, convert it to ONNX:

```python
import torch
from sentence_transformers import SentenceTransformer

# Load the model
model = SentenceTransformer('all-MiniLM-L6-v2')

# Export to ONNX
dummy_input = ['example text']
output_path = 'all-MiniLM-L6-v2.onnx'
torch.onnx.export(
    model,
    dummy_input,
    output_path,
    input_names=['input'],
    output_names=['output'],
    dynamic_axes={'input': {0: 'batch_size'}, 'output': {0: 'batch_size'}}
)
```

## Directory Structure

```
IncidentApp/
├── Models/
│   └── all-MiniLM-L6-v2.onnx  ← Place downloaded model here
├── AI/
│   └── Embedding/
│       └── ONNXEmbeddingService.cs
```

## Verification

After placing the model file, run the application and check the console output:

```
Loaded ONNX model from C:\...\Models\all-MiniLM-L6-v2.onnx
```

If you see this message, the model is loaded successfully!

## Alternative: Use Pre-trained ONNX Model

If conversion is difficult, you can download pre-converted ONNX models from:

- https://github.com/onnx/models
- https://huggingface.co/models?library=onnx

Search for "sentence-transformers" or "all-MiniLM-L6-v2" in ONNX format.
