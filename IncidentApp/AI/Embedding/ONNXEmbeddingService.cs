using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Generic;
using System.Linq;

namespace IncidentApp.AI.Embedding
{
    public class ONNXEmbeddingService
    {
        private readonly InferenceSession _session;
        private readonly string _modelPath;

        public ONNXEmbeddingService(IConfiguration config)
        {
            // Path to the ONNX model - will be placed in the application directory
            _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "all-MiniLM-L6-v2.onnx");
            
            // Ensure model directory exists
            var modelDirectory = Path.GetDirectoryName(_modelPath);
            if (!Directory.Exists(modelDirectory))
            {
                Directory.CreateDirectory(modelDirectory);
            }

            // Load the ONNX model
            if (File.Exists(_modelPath))
            {
                _session = new InferenceSession(_modelPath);
                Console.WriteLine($"Loaded ONNX model from {_modelPath}");
            }
            else
            {
                Console.WriteLine($"ONNX model not found at {_modelPath}. Please download the model.");
                throw new FileNotFoundException($"ONNX model not found at {_modelPath}. Please download all-MiniLM-L6-v2.onnx and place it in the Models directory.");
            }
        }

        public Task<float[]> GenerateEmbeddingAsync(
            string incidentTitle,
            string severity,
            string incidentDescription,
            string category,
            string logs,
            string systemComponent)
        {
            var combinedText = $"{incidentTitle} {severity} {incidentDescription} {category} {logs} {systemComponent}";
            return Task.FromResult(GenerateEmbeddingFromText(combinedText));
        }

        public float[] GenerateEmbeddingFromText(string text)
        {
            // Tokenize and preprocess text
            var tokens = Tokenize(text);
            
            // Create input tensor
            var inputTensor = new DenseTensor<float>(tokens.ToArray(), new[] { 1, tokens.Count });
            
            // Prepare inputs
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };
            
            // Run inference
            using var results = _session.Run(inputs);
            
            // Get output embedding
            var resultTensor = results.First().AsEnumerable<float>().ToArray();
            
            return resultTensor;
        }

        private List<float> Tokenize(string text)
        {
            // Simple word tokenization (for demonstration)
            // In production, you'd use the same tokenizer as the model was trained with
            var words = text.ToLower().Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '-', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Convert words to token IDs (simplified - in production use proper tokenizer)
            var tokens = new List<float>();
            var vocabularySize = 30522; // BERT vocabulary size
            var maxLength = 256; // Maximum sequence length
            
            foreach (var word in words)
            {
                // Simple hash-based token ID (for demonstration)
                var hash = word.GetHashCode();
                var tokenId = Math.Abs(hash % vocabularySize);
                tokens.Add(tokenId);
                
                if (tokens.Count >= maxLength)
                    break;
            }
            
            // Pad or truncate to max length
            while (tokens.Count < maxLength)
            {
                tokens.Add(0); // Padding token
            }
            
            return tokens;
        }

        // Utility method to check if model is available
        public bool IsModelAvailable()
        {
            return File.Exists(_modelPath);
        }
    }
}
