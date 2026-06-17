namespace IncidentApp.AI.Prompts
{
    public class AuditLoggingPrompt
    {
        public string BuildAuditLog(string retrievedContext, string relevanceReason, string reasoningSteps)
        {
            return $@"AI Incident Analysis Execution Audit

Context Retrieved:
{retrievedContext}

Relevance Reason:
{relevanceReason}

Key Reasoning Steps:
{reasoningSteps}

Timestamp: {DateTime.UtcNow:yyyyMMddHHmmss}
";
        }
    }
}
