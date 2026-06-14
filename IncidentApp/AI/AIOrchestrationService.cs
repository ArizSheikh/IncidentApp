using IncidentApp.AI.Mapping;
using IncidentApp.AI.Validation;
using IncidentApp.Models;
using IncidentApp.Models.AI;
using IncidentApp.Services;
using System.Text;
using System.Text.Json;

namespace IncidentApp.AI
{
    public class AIOrchestrationService
    {
        private readonly IncidentService _incidentService;
        private readonly GroqService _llm;
        private readonly AIResponseValidator _validator;
        private readonly AIResponseMapper _mapper;

        public AIOrchestrationService(IncidentService incidentService)
        {
            _incidentService = incidentService;
        }
        public AIOrchestrationService(
     IncidentService incidentService,
     GroqService llm,
     AIResponseValidator validator,
     AIResponseMapper mapper)
        {
            _incidentService = incidentService;
            _llm = llm;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<AIAnalysisResult> AnalyzeIncidentAsync(int incidentId)
        {
            try
            {
                var incident = await _incidentService.GetByIdAsync(incidentId);

                if (incident == null)
                {
                    return new AIAnalysisResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Incident not found"
                    };
                }

                var similar = await _incidentService.SearchAsync(incident.Title);

                var prompt = BuildPrompt(incident, similar);

                var rawResponse = await _llm.GetChatCompletionAsync(prompt);

                if (string.IsNullOrWhiteSpace(rawResponse))
                {
                    return new AIAnalysisResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "LLM returned empty response"
                    };
                }

                rawResponse = CleanJson(rawResponse);

                AIIncidentRawResponse validated;

                try
                {
                    validated = _validator.ValidateAndParse(rawResponse);
                }
                catch (Exception ex)
                {
                    return new AIAnalysisResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "JSON parsing failed: " + ex.Message
                    };
                }

                AIIncidentResponse mapped;

                try
                {
                    mapped = _mapper.MapToDomain(validated);
                }
                catch (Exception ex)
                {
                    return new AIAnalysisResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Mapping failed: " + ex.Message
                    };
                }

                return new AIAnalysisResult
                {
                    IsSuccess = true,
                    Data = mapped
                };
            }
            catch (Exception ex)
            {
                // LAST SAFETY NET
                return new AIAnalysisResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Unexpected AI failure: " + ex.Message
                };
            }
        }
        private string BuildPrompt(Incident incident, List<Incident> similarIncidents)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are an enterprise AI incident management assistant.");
            sb.AppendLine();
            sb.AppendLine("Analyze the production incident using:");
            sb.AppendLine("- Current incident details");
            sb.AppendLine("- Similar historical incidents");
            sb.AppendLine("- Operational reasoning");
            sb.AppendLine();

            sb.AppendLine("Your job:");
            sb.AppendLine("- identify likely root cause");
            sb.AppendLine("- correlate with similar incidents");
            sb.AppendLine("- provide actionable mitigation steps,mitigationPlan must ALWAYS be an array of strings");
            sb.AppendLine("- estimate severity");
            sb.AppendLine("- provide confidence score");
            sb.AppendLine();

            sb.AppendLine("Current Incident:");
            sb.AppendLine($"Title: {incident.Title}");
            sb.AppendLine($"Description: {incident.Description}");
            sb.AppendLine($"Severity: {incident.Severity}");
            sb.AppendLine($"Status: {incident.Status}");
            sb.AppendLine();

            sb.AppendLine("Similar Historical Incidents:");

            foreach (var x in similarIncidents)
            {
                sb.AppendLine("-----------------");
                sb.AppendLine($"Title: {x.Title}");
                sb.AppendLine($"Description: {x.Description}");
                sb.AppendLine($"Severity: {x.Severity}");
                sb.AppendLine($"Status: {x.Status}");
            }

            sb.AppendLine();
            sb.AppendLine("Rules:");
            sb.AppendLine("- Use historical incidents for correlation");
            sb.AppendLine("- Be specific and technical");
            sb.AppendLine("- Avoid generic answers");
            sb.AppendLine("- Only say unknown if absolutely necessary");
            sb.AppendLine("- Confidence must reflect evidence strength");
            sb.AppendLine();

            sb.AppendLine("Return ONLY valid JSON:");
            sb.AppendLine(@"
{
  ""summary"": """",
  ""rootCause"": """",
  ""mitigationPlan"": """",
  ""severityAssessment"": """",
  ""confidenceScore"": 0.0
}
");

            return sb.ToString();
        }

        private async Task<AIIncidentResponse> CallLLMAsync(string prompt)
        {
            var result = await _llm.GetChatCompletionAsync(prompt);
            result = CleanJson(result);
            return JsonSerializer.Deserialize<AIIncidentResponse>(result);
        }
        private string CleanJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "{}";

            return input
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();
        }

       
    }
}