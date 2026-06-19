using IncidentApp.Models;
using IncidentApp.Models.KnowledgeBase;

namespace IncidentApp.AI.Prompts
{
    public class RAGPromptBuilder
    {
        public string BuildPrompt(
            List<Incident> retrievedIncidents, 
            Incident currentIncident,
            List<KnowledgeDocument> knowledgeDocuments = null,
            List<KnowledgeChunk> knowledgeChunks = null)
        {
            var historicalIncidentsText = FormatHistoricalIncidents(retrievedIncidents);
            var currentIncidentText = FormatCurrentIncident(currentIncident);
            var knowledgeContextText = FormatKnowledgeContext(knowledgeDocuments, knowledgeChunks);

            return $@"You are a Senior Site Reliability Engineer (SRE) and Incident Response Expert.

Your job is to analyze a new production incident using historical incidents and knowledge base articles as reference context.

You must:
- Identify the most likely root cause
- Compare with historical incidents
- Reference relevant knowledge articles
- Suggest mitigation steps
- Provide severity assessment
- Estimate confidence score

====================================================
CURRENT INCIDENT
====================================================

{currentIncidentText}

====================================================
SIMILAR HISTORICAL INCIDENTS
====================================================

{historicalIncidentsText}

====================================================
KNOWLEDGE ARTICLES
====================================================

{knowledgeContextText}

====================================================
ANALYSIS TASK
====================================================

Generate:
- Summary
- Root Cause
- Severity Assessment
- Mitigation Plan
- Confidence Score

---

INSTRUCTIONS:

1. Analyze similarities with historical incidents
2. Reference relevant knowledge articles and documentation
3. Identify probable root cause
4. Suggest mitigation steps (bullet points)
5. Assess severity: Low | Medium | High | Critical
6. Provide confidence score (0.0 to 1.0)
7. If insufficient data, explicitly state ""Insufficient context""

---

OUTPUT FORMAT (STRICT JSON ONLY):

{{
  ""summary"": """",
  ""rootCause"": """",
  ""severityAssessment"": """",
  ""mitigationPlan"": [],
  ""confidenceScore"": 0.0,
  ""referencedKnowledge"": [],
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

        private string FormatKnowledgeContext(List<KnowledgeDocument> documents, List<KnowledgeChunk> chunks)
        {
            if ((documents == null || documents.Count == 0) && (chunks == null || chunks.Count == 0))
                return "No knowledge articles found.";

            var text = string.Empty;

            if (documents != null && documents.Count > 0)
            {
                text += "KNOWLEDGE DOCUMENTS:\n\n";
                for (int i = 0; i < documents.Count; i++)
                {
                    var doc = documents[i];
                    text += $"Document {i + 1}:\n";
                    text += $"Title: {doc.Title}\n";
                    text += $"Category: {doc.Category}\n";
                    text += $"Source: {doc.Source}\n";
                    text += $"Content: {doc.Content.Substring(0, Math.Min(500, doc.Content.Length))}...\n\n";
                }
            }

            if (chunks != null && chunks.Count > 0)
            {
                text += "RELEVANT KNOWLEDGE CHUNKS:\n\n";
                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    text += $"Chunk {i + 1} (Index: {chunk.ChunkIndex}):\n";
                    text += $"{chunk.ChunkText}\n\n";
                }
            }

            return text;
        }
    }
}
