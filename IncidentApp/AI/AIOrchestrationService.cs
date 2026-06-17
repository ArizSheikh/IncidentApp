using IncidentApp.AI.Mapping;
using IncidentApp.AI.Validation;
using IncidentApp.AI.Prompts;
using IncidentApp.AI.VectorSearch;
using IncidentApp.Models;
using IncidentApp.Models.AI;
using IncidentApp.Services;

namespace IncidentApp.AI
{
    public class AIOrchestrationService
    {
        private readonly IncidentService _incidentService;
        private readonly GroqService _llm;
        private readonly AIResponseValidator _validator;
        private readonly AIResponseMapper _mapper;
        private readonly QdrantVectorSearchService _vectorSearch;
        private readonly RAGPromptBuilder _ragPromptBuilder;
        private readonly AuditLoggingPrompt _auditPromptBuilder;

        public AIOrchestrationService(IncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        public AIOrchestrationService(
            IncidentService incidentService,
            GroqService llm,
            AIResponseValidator validator,
            AIResponseMapper mapper,
            QdrantVectorSearchService vectorSearch)
        {
            _incidentService = incidentService;
            _llm = llm;
            _validator = validator;
            _mapper = mapper;
            _vectorSearch = vectorSearch;
            _ragPromptBuilder = new RAGPromptBuilder();
            _auditPromptBuilder = new AuditLoggingPrompt();
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

                // Use vector search to find similar incidents
                var similarIncidents = await _vectorSearch.SearchSimilarIncidentsAsync(
                    incident.Description,
                    limit: 5,
                    scoreThreshold: 0.6f
                );

                // Build RAG prompt with retrieved context
                var prompt = _ragPromptBuilder.BuildPrompt(similarIncidents, incident);

                // Get LLM response
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

                // Generate audit log
                var auditLog = GenerateAuditLog(similarIncidents, mapped);

                return new AIAnalysisResult
                {
                    IsSuccess = true,
                    Data = mapped,
                    AuditLog = auditLog
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

        public async Task<bool> IndexIncidentForSearchAsync(int incidentId)
        {
            try
            {
                var incident = await _incidentService.GetByIdAsync(incidentId);

                if (incident == null)
                {
                    Console.WriteLine($"Incident {incidentId} not found");
                    return false;
                }

                Console.WriteLine($"Indexing incident {incidentId}: {incident.Title}");
                await _vectorSearch.IndexIncidentAsync(
                    incidentId: (ulong)incident.Id,
                    incidentTitle: incident.Title ?? "Unknown",
                    severity: incident.Severity ?? "Unknown",
                    incidentDescription: incident.Description ?? "No description",
                    category: "General",
                    logs: "",
                    systemComponent: "General"
                );

                Console.WriteLine($"Successfully indexed incident {incidentId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error indexing incident {incidentId}: {ex.Message}");
                return false;
            }
        }

        private string GenerateAuditLog(List<Incident> retrievedIncidents, AIIncidentResponse response)
        {
            var context = retrievedIncidents.Count > 0
                ? $"Retrieved {retrievedIncidents.Count} similar incidents"
                : "No similar incidents found";

            var relevanceReason = retrievedIncidents.Count > 0
                ? "Historical incidents used for context and correlation"
                : "Analysis based on current incident only";

            var reasoningSteps = $"1. Retrieved {retrievedIncidents.Count} similar incidents from vector database\n" +
                               $"2. Analyzed current incident against historical patterns\n" +
                               $"3. Identified root cause with confidence score of {response.ConfidenceScore}\n" +
                               $"4. Generated mitigation plan based on similar resolved incidents";

            return _auditPromptBuilder.BuildAuditLog(context, relevanceReason, reasoningSteps);
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
