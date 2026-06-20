using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace IncidentApp.AI.MCP
{
    public class MCPSemanticKernelPlugin
    {
        private readonly IMCPToolExecutionService _toolExecutionService;

        public MCPSemanticKernelPlugin(IMCPToolExecutionService toolExecutionService)
        {
            _toolExecutionService = toolExecutionService;
        }

        [KernelFunction, Description("Search for incidents based on a search term and optional filters")]
        public async Task<string> SearchIncidents(
            [Description("The search term to find incidents")] string searchTerm,
            [Description("Optional severity filter (Low, Medium, High, Critical)")] string? severity = null,
            [Description("Optional status filter (Open, In Progress, Resolved, Closed)")] string? status = null)
        {
            var arguments = new Dictionary<string, object>
            {
                { "searchTerm", searchTerm }
            };

            if (!string.IsNullOrEmpty(severity))
                arguments.Add("severity", severity);

            if (!string.IsNullOrEmpty(status))
                arguments.Add("status", status);

            var result = await _toolExecutionService.ExecuteToolAsync("search_incidents", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }

        [KernelFunction("get_incident")]
        [Description("Get a specific incident by ID")]
        public async Task<string> GetIncident(
            [Description("The incident ID")] int incidentId)
        {
            var arguments = new Dictionary<string, object>
            {
                { "incidentId", incidentId }
            };

            var result = await _toolExecutionService.ExecuteToolAsync("get_incident", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }

        [KernelFunction("create_incident")]
        [Description("Create a new incident")]
        public async Task<string> CreateIncident(
            [Description("The incident title")] string title,
            [Description("The incident description")] string description,
            [Description("The incident severity (Low, Medium, High, Critical)")] string severity)
        {
            var arguments = new Dictionary<string, object>
            {
                { "title", title },
                { "description", description },
                { "severity", severity }
            };

            var result = await _toolExecutionService.ExecuteToolAsync("create_incident", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }

        [KernelFunction("update_incident")]
        [Description("Update an existing incident")]
        public async Task<string> UpdateIncident(
            [Description("The incident ID")] int incidentId,
            [Description("Optional new title")] string? title = null,
            [Description("Optional new description")] string? description = null,
            [Description("Optional new severity")] string? severity = null)
        {
            var arguments = new Dictionary<string, object>
            {
                { "incidentId", incidentId }
            };

            if (!string.IsNullOrEmpty(title))
                arguments.Add("title", title);

            if (!string.IsNullOrEmpty(description))
                arguments.Add("description", description);

            if (!string.IsNullOrEmpty(severity))
                arguments.Add("severity", severity);

            var result = await _toolExecutionService.ExecuteToolAsync("update_incident", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }

        [KernelFunction("retrieve_similar_incidents")]
        [Description("Retrieve similar incidents based on current incident details")]
        public async Task<string> RetrieveSimilarIncidents(
            [Description("Current incident title")] string currentTitle,
            [Description("Current incident description")] string currentDescription,
            [Description("Maximum number of similar incidents to return")] int limit = 5,
            [Description("Similarity score threshold (0.0 to 1.0)")] float scoreThreshold = 0.6f)
        {
            var arguments = new Dictionary<string, object>
            {
                { "currentTitle", currentTitle },
                { "currentDescription", currentDescription },
                { "limit", limit },
                { "scoreThreshold", scoreThreshold }
            };

            var result = await _toolExecutionService.ExecuteToolAsync("retrieve_similar_incidents", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }

        [KernelFunction("search_knowledge")]
        [Description("Search knowledge base articles and chunks")]
        public async Task<string> SearchKnowledge(
            [Description("The search query")] string query,
            [Description("Maximum number of results to return")] int limit = 5,
            [Description("Similarity score threshold (0.0 to 1.0)")] float scoreThreshold = 0.7f)
        {
            var arguments = new Dictionary<string, object>
            {
                { "query", query },
                { "limit", limit },
                { "scoreThreshold", scoreThreshold }
            };

            var result = await _toolExecutionService.ExecuteToolAsync("search_knowledge", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }

        [KernelFunction("get_document")]
        [Description("Get a specific knowledge document by ID")]
        public async Task<string> GetDocument(
            [Description("The document ID")] int documentId)
        {
            var arguments = new Dictionary<string, object>
            {
                { "documentId", documentId }
            };

            var result = await _toolExecutionService.ExecuteToolAsync("get_document", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }

        [KernelFunction("get_document_chunks")]
        [Description("Get all chunks for a specific document")]
        public async Task<string> GetDocumentChunks(
            [Description("The document ID")] int documentId)
        {
            var arguments = new Dictionary<string, object>
            {
                { "documentId", documentId }
            };

            var result = await _toolExecutionService.ExecuteToolAsync("get_document_chunks", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }

        [KernelFunction("retrieve_knowledge_context")]
        [Description("Retrieve relevant knowledge context for an incident description")]
        public async Task<string> RetrieveKnowledgeContext(
            [Description("The incident description to find relevant knowledge for")] string incidentDescription,
            [Description("Maximum number of knowledge chunks to return")] int limit = 3)
        {
            var arguments = new Dictionary<string, object>
            {
                { "incidentDescription", incidentDescription },
                { "limit", limit }
            };

            var result = await _toolExecutionService.ExecuteToolAsync("retrieve_knowledge_context", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }

        [KernelFunction("analyze_incident")]
        [Description("Analyze an incident using AI with historical context and knowledge base")]
        public async Task<string> AnalyzeIncident(
            [Description("The incident ID to analyze")] int incidentId)
        {
            var arguments = new Dictionary<string, object>
            {
                { "incidentId", incidentId }
            };

            var result = await _toolExecutionService.ExecuteToolAsync("analyze_incident", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }

        [KernelFunction("generate_recommendations")]
        [Description("Generate mitigation recommendations for an incident")]
        public async Task<string> GenerateRecommendations(
            [Description("The incident ID to generate recommendations for")] int incidentId)
        {
            var arguments = new Dictionary<string, object>
            {
                { "incidentId", incidentId }
            };

            var result = await _toolExecutionService.ExecuteToolAsync("generate_recommendations", arguments, "MCPSemanticKernelPlugin");
            return result.Success ? System.Text.Json.JsonSerializer.Serialize(result.Result) : $"Error: {result.Error}";
        }
    }
}
