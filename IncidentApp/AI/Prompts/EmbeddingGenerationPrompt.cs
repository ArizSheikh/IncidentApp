namespace IncidentApp.AI.Prompts
{
    public class EmbeddingGenerationPrompt
    {
        public string GeneratePrompt(string incidentTitle, string severity, string incidentDescription, string category, string logs, string systemComponent)
        {
            return $@"{incidentTitle}

Severity: {severity}

Description:
{incidentDescription}

Category: {category}

Error Logs:
{logs}

System Context:
{systemComponent}";
        }
    }
}
