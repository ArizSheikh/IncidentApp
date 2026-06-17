using System.Diagnostics;

namespace IncidentApp.AI.Evaluation
{
    public class AIEvaluationService
    {
        private readonly List<AIEvaluationRecord> _evaluationRecords = new();

        public async Task<AIEvaluationResult> EvaluateAsync(
            string prompt,
            string response,
            string model,
            Func<Task<string>> action)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var result = await action();
                stopwatch.Stop();

                var evaluation = new AIEvaluationResult
                {
                    Success = true,
                    Prompt = prompt,
                    Response = result,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    Model = model,
                    Timestamp = DateTime.UtcNow
                };

                // Estimate token count (rough approximation: ~4 chars per token)
                evaluation.PromptTokens = EstimateTokenCount(prompt);
                evaluation.ResponseTokens = EstimateTokenCount(result);
                evaluation.TotalTokens = evaluation.PromptTokens + evaluation.ResponseTokens;

                _evaluationRecords.Add(new AIEvaluationRecord(evaluation));
                
                return evaluation;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                var evaluation = new AIEvaluationResult
                {
                    Success = false,
                    Prompt = prompt,
                    Response = string.Empty,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    Model = model,
                    Timestamp = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                };

                evaluation.PromptTokens = EstimateTokenCount(prompt);
                evaluation.TotalTokens = evaluation.PromptTokens;

                _evaluationRecords.Add(new AIEvaluationRecord(evaluation));
                
                return evaluation;
            }
        }

        public void CalculateSimilarityScore(string response1, string response2)
        {
            var similarity = CalculateCosineSimilarity(response1, response2);
            
            if (_evaluationRecords.Count >= 2)
            {
                var lastRecord = _evaluationRecords.Last();
                lastRecord.SimilarityScore = similarity;
            }
        }

        public List<AIEvaluationRecord> GetEvaluationRecords(int limit = 100)
        {
            return _evaluationRecords.TakeLast(limit).ToList();
        }

        public AIEvaluationMetrics GetMetrics()
        {
            if (_evaluationRecords.Count == 0)
            {
                return new AIEvaluationMetrics();
            }

            var successfulRecords = _evaluationRecords.Where(r => r.Success).ToList();
            
            return new AIEvaluationMetrics
            {
                TotalRequests = _evaluationRecords.Count,
                SuccessfulRequests = successfulRecords.Count,
                FailedRequests = _evaluationRecords.Count - successfulRecords.Count,
                AverageLatencyMs = successfulRecords.Any() 
                    ? successfulRecords.Average(r => r.LatencyMs) 
                    : 0,
                AverageTokensPerRequest = successfulRecords.Any()
                    ? successfulRecords.Average(r => r.TotalTokens)
                    : 0,
                AverageSimilarityScore = successfulRecords.Any(r => r.SimilarityScore.HasValue)
                    ? successfulRecords.Where(r => r.SimilarityScore.HasValue).Average(r => r.SimilarityScore.Value)
                    : 0
            };
        }

        private int EstimateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        private float CalculateCosineSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0f;

            var words1 = text1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var words2 = text2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var intersection = words1.Intersect(words2).Count();
            var magnitude1 = Math.Sqrt(words1.Length);
            var magnitude2 = Math.Sqrt(words2.Length);
            
            if (magnitude1 == 0 || magnitude2 == 0)
                return 0f;

            return (float)(intersection / (magnitude1 * magnitude2));
        }
    }

    public class AIEvaluationResult
    {
        public bool Success { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
        public int PromptTokens { get; set; }
        public int ResponseTokens { get; set; }
        public int TotalTokens { get; set; }
        public string Model { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AIEvaluationRecord
    {
        public AIEvaluationRecord(AIEvaluationResult result)
        {
            Success = result.Success;
            Prompt = result.Prompt;
            Response = result.Response;
            LatencyMs = result.LatencyMs;
            PromptTokens = result.PromptTokens;
            ResponseTokens = result.ResponseTokens;
            TotalTokens = result.TotalTokens;
            Model = result.Model;
            Timestamp = result.Timestamp;
            ErrorMessage = result.ErrorMessage;
        }

        public bool Success { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
        public int PromptTokens { get; set; }
        public int ResponseTokens { get; set; }
        public int TotalTokens { get; set; }
        public string Model { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? ErrorMessage { get; set; }
        public float? SimilarityScore { get; set; }
    }

    public class AIEvaluationMetrics
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageLatencyMs { get; set; }
        public double AverageTokensPerRequest { get; set; }
        public double AverageSimilarityScore { get; set; }
    }
}
