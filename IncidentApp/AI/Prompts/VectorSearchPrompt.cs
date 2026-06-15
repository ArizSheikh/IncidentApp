namespace IncidentApp.AI.Prompts
{
    public class VectorSearchPrompt
    {
        public string GenerateSearchQuery(string incidentDescription)
        {
            return $@"Find similar production incidents for the following issue:

{incidentDescription}

Focus on:
- Same or similar root cause
- Similar system component
- Similar error patterns
- Authentication, latency, database, or API issues";
        }
    }
}
