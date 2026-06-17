using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace IncidentApp.AI.SemanticKernel
{
    public class SemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;

        public SemanticKernelService(IConfiguration config)
        {
            var builder = Kernel.CreateBuilder();
            
            var groqApiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API key is not configured");
            var groqEndpoint = config["SemanticKernel:GroqEndpoint"] ?? "https://api.groq.com/openai/v1";
            var groqModelId = config["SemanticKernel:GroqModelId"] ?? "llama-3.3-70b-versatile";

            builder.AddOpenAIChatCompletion(
                modelId: groqModelId,
                apiKey: groqApiKey,
                endpoint: new Uri(groqEndpoint)
            );

            _kernel = builder.Build();
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<string> GetChatCompletionAsync(string prompt)
        {
            var chatHistory = new ChatHistory();
            
            chatHistory.AddSystemMessage("You are an enterprise incident management AI. Always return structured JSON only.");
            chatHistory.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.2,
                MaxTokens = 2000
            };

            var result = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                settings,
                _kernel
            );

            return result.Content ?? string.Empty;
        }

        public async Task<string> GetChatCompletionWithHistoryAsync(ChatHistory chatHistory, string? additionalMessage = null)
        {
            if (!string.IsNullOrEmpty(additionalMessage))
            {
                chatHistory.AddUserMessage(additionalMessage);
            }

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.2,
                MaxTokens = 2000
            };

            var result = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                settings,
                _kernel
            );

            return result.Content ?? string.Empty;
        }
    }
}
