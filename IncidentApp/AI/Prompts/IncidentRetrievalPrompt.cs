namespace IncidentApp.AI.Prompts
{
    public class IncidentRetrievalPrompt
    {
        public string BuildFilteringPrompt(List<string> candidateIncidents, string query)
        {
            var incidentsText = string.Join("\n\n", candidateIncidents);

            return $@"From the following incident list, select the most relevant ones for diagnosing a new production issue.

Return only top 5 most relevant incidents.

Criteria:
- Same root cause
- Same system module
- Closest semantic similarity

Incidents:
{incidentsText}

New Issue:
{query}

Return the selected incidents in the same format as they were provided.";
        }
    }
}
