using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IncidentApp.Services;

namespace IncidentApp.AI
{
    public class GroqService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly AIGovernanceService _governanceService;
        private readonly string _modelId = "llama-3.3-70b-versatile";
        private readonly string _provider = "Groq";

        public GroqService(IConfiguration config, AIGovernanceService governanceService)
        {
            _apiKey = config["Groq:ApiKey"];
            _governanceService = governanceService;
            _httpClient = new HttpClient();
            
            // Initialize model version if not exists
            _ = InitializeModelVersionAsync();
        }

        private async Task InitializeModelVersionAsync()
        {
            try
            {
                var existingModel = await _governanceService.GetActiveModelVersionAsync("Groq");
                if (existingModel == null)
                {
                    await _governanceService.CreateModelVersionAsync(
                        name: "Groq",
                        version: "1.0",
                        provider: _provider,
                        modelId: _modelId,
                        endpoint: "https://api.groq.com/openai/v1",
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
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Groq API key is not configured. Please set the Groq:ApiKey in appsettings.Development.json");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var requestBody = new
            {
                model = _modelId,
                messages = new[]
                {
                    new { role = "system", content = "You are an enterprise incident management AI. Always return structured JSON only." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Groq API request failed with status {response.StatusCode}. Response: {json}");
            }

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Invalid response format from Groq API: 'choices' property missing or empty");
            }

            var result = choices[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            stopwatch.Stop();

            // Log governance data
            _ = LogGovernanceDataAsync(prompt, result ?? string.Empty, stopwatch.ElapsedMilliseconds);

            return result;
        }

        private async Task LogGovernanceDataAsync(string prompt, string response, long latencyMs)
        {
            try
            {
                // Get or create prompt version
                var promptVersion = await _governanceService.GetActivePromptVersionAsync("Groq");
                if (promptVersion == null)
                {
                    promptVersion = await _governanceService.CreatePromptVersionAsync(
                        name: "Groq",
                        version: "1.0",
                        content: prompt,
                        purpose: "Incident analysis and recommendations",
                        createdBy: "System"
                    );
                }

                // Get model version
                var modelVersion = await _governanceService.GetActiveModelVersionAsync("Groq");
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
                JsonDocument.Parse(response);
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
    }
}