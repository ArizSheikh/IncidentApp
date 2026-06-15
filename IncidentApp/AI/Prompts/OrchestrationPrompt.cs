namespace IncidentApp.AI.Prompts
{
    public class OrchestrationPrompt
    {
        public string BuildDecisionPrompt(string incident)
        {
            return $@"You are an AI incident orchestration engine.

Decide whether the current incident requires:
- RAG retrieval
- Direct LLM analysis
- Escalation to human review

Incident:
{incident}

Return JSON:
{{
  ""useRag"": true/false,
  ""reason"": """",
  ""priority"": ""
}}";
        }
    }
}
