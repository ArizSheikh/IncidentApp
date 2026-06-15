using IncidentApp.Models;

namespace IncidentApp.AI.Prompts
{
    public class RAGPromptBuilder
    {
        public string BuildPrompt(List<Incident> retrievedIncidents, Incident currentIncident)
        {
            var historicalIncidentsText = FormatHistoricalIncidents(retrievedIncidents);
            var currentIncidentText = FormatCurrentIncident(currentIncident);

            return $@"You are a Senior Site Reliability Engineer (SRE) and Incident Response Expert.

Your job is to analyze a new production incident using historical incidents as reference context.

You must:
- Identify the most likely root cause
- Compare with historical incidents
- Suggest mitigation steps
- Provide severity assessment
- Estimate confidence score

---

HISTORICAL INCIDENTS (retrieved from vector database):

{historicalIncidentsText}

---

CURRENT INCIDENT:

{currentIncidentText}

---

INSTRUCTIONS:

1. Analyze similarities with historical incidents
2. Identify probable root cause
3. Suggest mitigation steps (bullet points)
4. Assess severity: Low | Medium | High | Critical
5. Provide confidence score (0.0 to 1.0)
6. If insufficient data, explicitly state ""Insufficient historical context""

---

OUTPUT FORMAT (STRICT JSON ONLY):

{{
  ""rootCause"": """",
  ""mitigationPlan"": [],
  ""severityAssessment"": """",
  ""confidenceScore"": 0.0,
  ""similarIncidentsSummary"": """",
  ""recommendation"": ""
}}";
        }

        private string FormatHistoricalIncidents(List<Incident> incidents)
        {
            if (incidents == null || incidents.Count == 0)
                return "No historical incidents found.";

            var text = string.Empty;
            for (int i = 0; i < incidents.Count; i++)
            {
                var incident = incidents[i];
                text += $"Incident {i + 1}:\n";
                text += $"Title: {incident.Title}\n";
                text += $"Description: {incident.Description}\n";
                text += $"Severity: {incident.Severity}\n";
                text += $"Status: {incident.Status}\n";
                text += $"Created: {incident.CreatedAt}\n\n";
            }
            return text;
        }

        private string FormatCurrentIncident(Incident incident)
        {
            return $"Title: {incident.Title}\n" +
                   $"Description: {incident.Description}\n" +
                   $"Severity: {incident.Severity}\n" +
                   $"Status: {incident.Status}\n" +
                   $"Created: {incident.CreatedAt}";
        }
    }
}
