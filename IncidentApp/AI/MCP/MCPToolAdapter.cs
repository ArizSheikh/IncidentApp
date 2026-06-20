using IncidentApp.AI.FunctionCalling;

namespace IncidentApp.AI.MCP
{
    public class MCPToolAdapter
    {
        private readonly MCPServer _mcpServer;
        private readonly IncidentTools _incidentTools;

        public MCPToolAdapter(MCPServer mcpServer, IncidentTools incidentTools)
        {
            _mcpServer = mcpServer;
            _incidentTools = incidentTools;
            
            RegisterIncidentTools();
        }

        private void RegisterIncidentTools()
        {
            // Register CreateIncident tool
            _mcpServer.RegisterTool(new MCPTool
            {
                Name = "create_incident",
                Description = "Creates a new incident with the provided details",
                Schema = new Dictionary<string, object>
                {
                    { "type", "object" },
                    { "properties", new Dictionary<string, object>
                        {
                            { "title", new { type = "string", description = "The title of the incident" } },
                            { "description", new { type = "string", description = "The description of the incident" } },
                            { "severity", new { type = "string", description = "The severity level (Critical, High, Medium, Low)" } },
                            { "status", new { type = "string", description = "The status of the incident (Open, In Progress, Resolved, Closed)" } }
                        }
                    },
                    { "required", new[] { "title", "description", "severity" } }
                },
                ExecuteAsync = async (parameters) =>
                {
                    var title = parameters.GetValueOrDefault("title")?.ToString() ?? string.Empty;
                    var description = parameters.GetValueOrDefault("description")?.ToString() ?? string.Empty;
                    var severity = parameters.GetValueOrDefault("severity")?.ToString() ?? string.Empty;
                    var status = parameters.GetValueOrDefault("status")?.ToString() ?? "Open";

                    return await _incidentTools.CreateIncident(title, description, severity, status);
                }
            });

            // Register SearchIncidents tool
            _mcpServer.RegisterTool(new MCPTool
            {
                Name = "search_incidents",
                Description = "Searches for incidents based on criteria",
                Schema = new Dictionary<string, object>
                {
                    { "type", "object" },
                    { "properties", new Dictionary<string, object>
                        {
                            { "searchTerm", new { type = "string", description = "Search term to match in title or description" } },
                            { "severity", new { type = "string", description = "Filter by severity level" } },
                            { "status", new { type = "string", description = "Filter by status" } }
                        }
                    }
                },
                ExecuteAsync = async (parameters) =>
                {
                    var searchTerm = parameters.GetValueOrDefault("searchTerm")?.ToString() ?? string.Empty;
                    var severity = parameters.GetValueOrDefault("severity")?.ToString();
                    var status = parameters.GetValueOrDefault("status")?.ToString();

                    return await _incidentTools.SearchIncidents(searchTerm, severity, status);
                }
            });

            // Register UpdateSeverity tool
            _mcpServer.RegisterTool(new MCPTool
            {
                Name = "update_severity",
                Description = "Updates the severity of an existing incident",
                Schema = new Dictionary<string, object>
                {
                    { "type", "object" },
                    { "properties", new Dictionary<string, object>
                        {
                            { "incidentId", new { type = "integer", description = "The ID of the incident to update" } },
                            { "newSeverity", new { type = "string", description = "The new severity level (Critical, High, Medium, Low)" } }
                        }
                    },
                    { "required", new[] { "incidentId", "newSeverity" } }
                },
                ExecuteAsync = async (parameters) =>
                {
                    var incidentId = Convert.ToInt32(parameters.GetValueOrDefault("incidentId"));
                    var newSeverity = parameters.GetValueOrDefault("newSeverity")?.ToString() ?? string.Empty;

                    return await _incidentTools.UpdateSeverity(incidentId, newSeverity);
                }
            });

            // Register RetrieveHistoricalIncidents tool
            _mcpServer.RegisterTool(new MCPTool
            {
                Name = "retrieve_historical_incidents",
                Description = "Retrieves historical incidents similar to the current one",
                Schema = new Dictionary<string, object>
                {
                    { "type", "object" },
                    { "properties", new Dictionary<string, object>
                        {
                            { "currentTitle", new { type = "string", description = "The current incident title for similarity comparison" } },
                            { "currentDescription", new { type = "string", description = "The current incident description for similarity comparison" } },
                            { "limit", new { type = "integer", description = "Number of similar incidents to retrieve" } },
                            { "scoreThreshold", new { type = "number", description = "Minimum similarity score threshold (0.0 to 1.0)" } }
                        }
                    },
                    { "required", new[] { "currentTitle", "currentDescription" } }
                },
                ExecuteAsync = async (parameters) =>
                {
                    var currentTitle = parameters.GetValueOrDefault("currentTitle")?.ToString() ?? string.Empty;
                    var currentDescription = parameters.GetValueOrDefault("currentDescription")?.ToString() ?? string.Empty;
                    var limit = parameters.ContainsKey("limit") ? Convert.ToInt32(parameters["limit"]) : 5;
                    var scoreThreshold = parameters.ContainsKey("scoreThreshold") ? Convert.ToSingle(parameters["scoreThreshold"]) : 0.6f;

                    return await _incidentTools.RetrieveHistoricalIncidents(currentTitle, currentDescription, limit, scoreThreshold);
                }
            });
        }
    }
}
