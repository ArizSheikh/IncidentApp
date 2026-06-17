namespace IncidentApp.AI.SemanticKernel
{
    public class SemanticKernelConfig
    {
        public string GroqApiKey { get; set; } = string.Empty;
        public string GroqModelId { get; set; } = "llama-3.3-70b-versatile";
        public string GroqEndpoint { get; set; } = "https://api.groq.com/openai/v1";
        
        public string OllamaEndpoint { get; set; } = "http://localhost:11434";
        public string OllamaModelId { get; set; } = "nomic-embed-text";
    }
}
