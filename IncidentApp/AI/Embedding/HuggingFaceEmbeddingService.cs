using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IncidentApp.AI.Prompts;

namespace IncidentApp.AI.Embedding
{
    public class HuggingFaceEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelId;
        private readonly EmbeddingGenerationPrompt _promptBuilder;

        public HuggingFaceEmbeddingService(IConfiguration config)
        {
            _apiKey = config["HuggingFace:ApiKey"];
            _modelId = config["HuggingFace:ModelId"] ?? "sentence-transformers/all-MiniLM-L6-v2";
            
            // Configure HttpClient for VPN compatibility
            var handler = new HttpClientHandler();
            handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
            handler.UseProxy = false; // Let VPN handle routing
            handler.DefaultProxyCredentials = null;
            
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "IncidentApp/1.0");
            _promptBuilder = new EmbeddingGenerationPrompt();
        }

        public async Task<float[]> GenerateEmbeddingAsync(
            string incidentTitle,
            string severity,
            string incidentDescription,
            string category,
            string logs,
            string systemComponent)
        {
            var prompt = _promptBuilder.GeneratePrompt(
                incidentTitle,
                severity,
                incidentDescription,
                category,
                logs,
                systemComponent
            );

            return await GenerateEmbeddingFromTextAsync(prompt);
        }

        public async Task<float[]> GenerateEmbeddingFromTextAsync(string text)
        {
            try
            {
                Console.WriteLine($"Attempting to generate embedding for text: {text.Substring(0, Math.Min(50, text.Length))}...");
                
                var requestBody = new
                {
                    inputs = text
                };

                // Try HTTP first to bypass VPN SSL interference
                string[] protocols = { "http", "https" };
                HttpResponseMessage? response = null;

                foreach (var protocol in protocols)
                {
                    try
                    {
                        var apiUrl = $"{protocol}://api-inference.huggingface.co/models/{_modelId}";
                        Console.WriteLine($"Trying {protocol.ToUpper()}://{apiUrl}");
                        
                        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                        response = await _httpClient.SendAsync(request);
                        Console.WriteLine($"Response status: {response.StatusCode}");

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Success with {protocol.ToUpper()}!");
                            break;
                        }
                        else
                        {
                            Console.WriteLine($"{protocol.ToUpper()} returned {response.StatusCode}, trying next protocol...");
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Console.WriteLine($"{protocol.ToUpper()} failed: {httpEx.Message}");
                        Console.WriteLine($"Inner Exception: {httpEx.InnerException?.Message ?? "None"}");
                        
                        if (protocol == "http")
                            continue; // Try HTTPS next
                        else
                            throw; // HTTPS also failed
                    }
                }

                if (response == null)
                {
                    throw new Exception("Both HTTP and HTTPS failed");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error: {response.StatusCode} - {errorContent}");
                    throw new Exception($"HuggingFace API error: {response.StatusCode} - {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response length: {json.Length}");

                using var doc = JsonDocument.Parse(json);

                // Handle different response formats from HuggingFace
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var embeddings = new List<float>();
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        embeddings.Add(element.GetSingle());
                    }
                    Console.WriteLine($"Generated embedding with {embeddings.Count} dimensions");
                    return embeddings.ToArray();
                }
                else if (doc.RootElement.TryGetProperty("embeddings", out var embeddingsProp))
                {
                    var embeddings = new List<float>();
                    foreach (var element in embeddingsProp.EnumerateArray())
                    {
                        embeddings.Add(element.GetSingle());
                    }
                    Console.WriteLine($"Generated embedding with {embeddings.Count} dimensions (from embeddings property)");
                    return embeddings.ToArray();
                }
                else if (doc.RootElement.TryGetProperty("error", out var errorProp))
                {
                    throw new Exception($"HuggingFace API returned error: {errorProp.GetString()}");
                }
                else
                {
                    Console.WriteLine($"Unexpected response structure: {json}");
                    throw new Exception("Unexpected response format from HuggingFace API");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request Error: {httpEx.Message}");
                Console.WriteLine($"Inner Exception: {httpEx.InnerException?.Message ?? "None"}");
                if (httpEx.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception Type: {httpEx.InnerException.GetType().Name}");
                    if (httpEx.InnerException.InnerException != null)
                    {
                        Console.WriteLine($"Nested Inner Exception: {httpEx.InnerException.InnerException.Message}");
                    }
                }
                throw new Exception($"Network error calling HuggingFace API: {httpEx.Message}", httpEx);
            }
            catch (TaskCanceledException timeoutEx)
            {
                Console.WriteLine($"Request timeout: {timeoutEx.Message}");
                throw new Exception("HuggingFace API request timed out", timeoutEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message ?? "None"}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception Type: {ex.InnerException.GetType().Name}");
                }
                throw new Exception($"Error generating embedding: {ex.Message}", ex);
            }
        }

        public async Task<List<float[]>> GenerateBatchEmbeddingsAsync(List<string> texts)
        {
            var embeddings = new List<float[]>();
            
            foreach (var text in texts)
            {
                var embedding = await GenerateEmbeddingFromTextAsync(text);
                embeddings.Add(embedding);
            }

            return embeddings;
        }
    }
}
