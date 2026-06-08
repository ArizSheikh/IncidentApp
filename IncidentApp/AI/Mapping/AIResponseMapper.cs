using IncidentApp.Models.AI;

namespace IncidentApp.AI.Mapping
{
    public class AIResponseMapper
    {
        public AIIncidentResponse MapToDomain(AIIncidentRawResponse raw)
        {
            return new AIIncidentResponse
            {
                Summary = raw.summary,
                RootCause = raw.rootCause,
                MitigationPlan = raw.mitigationPlan,
                SeverityAssessment = raw.severityAssessment,
                ConfidenceScore = raw.confidenceScore
            };
        }
    }
}