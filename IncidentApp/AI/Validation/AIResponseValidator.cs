using System.Text.Json;
using IncidentApp.Models.AI;

namespace IncidentApp.AI.Validation
{
    public class AIResponseValidator
    {
        public AIIncidentRawResponse ValidateAndParse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new Exception("Empty LLM response");

            json = Clean(json);

            var result = JsonSerializer.Deserialize<AIIncidentRawResponse>(json);

            if (result == null)
                throw new Exception("Invalid JSON structure from LLM");

            if (result.confidenceScore < 0 || result.confidenceScore > 1)
                throw new Exception("Invalid confidence score");

            return result;
        }

        private string Clean(string input)
        {
            return input
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();
        }
    }
}