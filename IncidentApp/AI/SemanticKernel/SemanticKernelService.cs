using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using IncidentApp.Services;

namespace IncidentApp.AI.SemanticKernel
{
    public class SemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly AIGovernanceService _governanceService;
        private readonly IConfiguration _config;
        private readonly string _modelId;
        private readonly string _provider;

        public SemanticKernelService(IConfiguration config, AIGovernanceService governanceService)
        {
            _config = config;
            _governanceService = governanceService;
            
            var builder = Kernel.CreateBuilder();
            
            var groqApiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API key is not configured");
            var groqEndpoint = config["SemanticKernel:GroqEndpoint"] ?? "https://api.groq.com/openai/v1";
            _modelId = config["SemanticKernel:GroqModelId"] ?? "llama-3.3-70b-versatile";
            _provider = "Groq";

            builder.AddOpenAIChatCompletion(
                modelId: _modelId,
                apiKey: groqApiKey,
                endpoint: new Uri(groqEndpoint)
            );

            _kernel = builder.Build();
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            
            // Initialize model version if not exists
            _ = InitializeModelVersionAsync();
        }

        private async Task InitializeModelVersionAsync()
        {
            try
            {
                var existingModel = await _governanceService.GetActiveModelVersionAsync("SemanticKernel");
                if (existingModel == null)
                {
                    await _governanceService.CreateModelVersionAsync(
                        name: "SemanticKernel",
                        version: "1.0",
                        provider: _provider,
                        modelId: _modelId,
                        endpoint: _config["SemanticKernel:GroqEndpoint"],
                        createdBy: "System"
                    );
                }
            }
            catch
            {
                // Ignore initialization errors
            }
        }

        public async Task<string> GetChatCompletionAsync(string prompt)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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

            stopwatch.Stop();
            var response = result.Content ?? string.Empty;
            response = CleanLlmResponse(response);

            // Log governance data (wrapped in try-catch to prevent blocking)
            try
            {
                _ = LogGovernanceDataAsync(prompt, response, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                // Governance logging failed but don't block the main workflow
                Console.WriteLine($"[SemanticKernelService] Governance logging failed: {ex.Message}");
            }

            return response;
        }

        private static string CleanLlmResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return response;
            response = response
                .Replace("```json", "").Replace("```JSON", "")
                .Replace("```", "").Trim();
            var start = response.IndexOf('{');
            var end = response.LastIndexOf('}');
            if (start >= 0 && end > start)
                return response.Substring(start, end - start + 1);
            return response;
        }

        private async Task LogGovernanceDataAsync(string prompt, string response, long latencyMs)
        {
            try
            {
                // Get or create prompt version
                var promptVersion = await _governanceService.GetActivePromptVersionAsync("SemanticKernel");
                if (promptVersion == null)
                {
                    promptVersion = await _governanceService.CreatePromptVersionAsync(
                        name: "SemanticKernel",
                        version: "1.0",
                        content: prompt,
                        purpose: "Incident analysis and planning",
                        createdBy: "System"
                    );
                }

                // Get model version
                var modelVersion = await _governanceService.GetActiveModelVersionAsync("SemanticKernel");
                if (modelVersion != null)
                {
                    // Calculate simple evaluation scores
                    var accuracyScore = CalculateAccuracyScore(response);
                    var relevanceScore = CalculateRelevanceScore(prompt, response);
                    var coherenceScore = CalculateCoherenceScore(response);
                    var safetyScore = CalculateSafetyScore(response);
                    var tokenCount = EstimateTokenCount(response);

                    // Record evaluation
                    await _governanceService.RecordEvaluationAsync(
                        promptVersionId: promptVersion.Id,
                        modelVersionId: modelVersion.Id,
                        accuracyScore: accuracyScore,
                        relevanceScore: relevanceScore,
                        coherenceScore: coherenceScore,
                        safetyScore: safetyScore,
                        latencyMs: (int)latencyMs,
                        tokenCount: tokenCount,
                        evaluatedBy: "System"
                    );
                }
            }
            catch
            {
                // Ignore logging errors to not disrupt main functionality
            }
        }

        private double CalculateAccuracyScore(string response)
        {
            // Simple heuristic: check if response contains valid JSON
            try
            {
                System.Text.Json.JsonDocument.Parse(response);
                return 0.9; // High score if valid JSON
            }
            catch
            {
                return 0.5; // Lower score if not valid JSON
            }
        }

        private double CalculateRelevanceScore(string prompt, string response)
        {
            // Simple heuristic: check if response length is reasonable
            if (string.IsNullOrEmpty(response)) return 0.0;
            if (response.Length < 10) return 0.3;
            if (response.Length > 5000) return 0.7;
            return 0.8;
        }

        private double CalculateCoherenceScore(string response)
        {
            // Simple heuristic: check for basic sentence structure
            if (string.IsNullOrEmpty(response)) return 0.0;
            var sentences = response.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return Math.Min(1.0, sentences.Length / 5.0);
        }

        private double CalculateSafetyScore(string response)
        {
            // Simple heuristic: check for potentially harmful content
            var harmfulKeywords = new[] { "hack", "exploit", "bypass", "inject", "malicious" };
            var lowerResponse = response.ToLower();
            var harmfulCount = harmfulKeywords.Count(keyword => lowerResponse.Contains(keyword));
            return Math.Max(0.0, 1.0 - (harmfulCount * 0.2));
        }

        private int EstimateTokenCount(string text)
        {
            // Rough estimation: ~4 characters per token
            return text.Length / 4;
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
