using IncidentApp.Models;
using IncidentApp.Services;
using IncidentApp.DTOs;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace IncidentApp.AI.FunctionCalling
{
    public class IncidentTools
    {
        private readonly IncidentService _incidentService;

        public IncidentTools(IncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        [KernelFunction, Description("Creates a new incident with the provided details")]
        public async Task<Incident> CreateIncident(
            [Description("The title of the incident")] string title,
            [Description("The description of the incident")] string description,
            [Description("The severity level (Critical, High, Medium, Low)")] string severity,
            [Description("The status of the incident (Open, In Progress, Resolved, Closed)")] string status = "Open")
        {
            var dto = new CreateIncidentDto
            {
                Title = title,
                Description = description,
                Severity = severity
            };

            return await _incidentService.CreateAsync(dto);
        }

        [KernelFunction, Description("Searches for incidents based on criteria")]
        public async Task<List<Incident>> SearchIncidents(
            [Description("Search term to match in title or description")] string searchTerm,
            [Description("Filter by severity level")] string? severity = null,
            [Description("Filter by status")] string? status = null)
        {
            var incidents = await _incidentService.GetAllAsync();
            
            var filtered = incidents.Where(i =>
                (string.IsNullOrEmpty(searchTerm) || 
                 (i.Title?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                 (i.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)) &&
                (string.IsNullOrEmpty(severity) || i.Severity == severity) &&
                (string.IsNullOrEmpty(status) || i.Status == status)
            ).ToList();

            return filtered;
        }

        [KernelFunction, Description("Updates the severity of an existing incident")]
        public async Task<Incident?> UpdateSeverity(
            [Description("The ID of the incident to update")] int incidentId,
            [Description("The new severity level (Critical, High, Medium, Low)")] string newSeverity)
        {
            var incident = await _incidentService.GetByIdAsync(incidentId);
            if (incident == null) return null;

            // Note: Update functionality not implemented in IncidentService
            // This would require adding UpdateAsync to IncidentService and Repository
            incident.Severity = newSeverity;
            
            return incident;
        }

        [KernelFunction, Description("Retrieves historical incidents similar to the current one")]
        public async Task<List<Incident>> RetrieveHistoricalIncidents(
            [Description("The current incident title for similarity comparison")] string currentTitle,
            [Description("The current incident description for similarity comparison")] string currentDescription,
            [Description("Number of similar incidents to retrieve")] int limit = 5,
            [Description("Minimum similarity score threshold (0.0 to 1.0)")] float scoreThreshold = 0.6f)
        {
            var incidents = await _incidentService.GetAllAsync();
            
            // Simple similarity scoring based on text overlap
            var scoredIncidents = incidents
                .Where(i => i.Id != 0) // Exclude current if ID is 0
                .Select(i => new
                {
                    Incident = i,
                    Score = CalculateSimilarityScore(
                        $"{i.Title} {i.Description}",
                        $"{currentTitle} {currentDescription}"
                    )
                })
                .Where(x => x.Score >= scoreThreshold)
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => x.Incident)
                .ToList();

            return scoredIncidents;
        }

        private float CalculateSimilarityScore(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0f;

            var words1 = text1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var words2 = text2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();
            
            return union > 0 ? (float)intersection / union : 0f;
        }
    }
}
