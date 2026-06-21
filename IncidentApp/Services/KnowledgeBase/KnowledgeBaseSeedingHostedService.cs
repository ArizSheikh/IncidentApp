using IncidentApp.Models.KnowledgeBase;
using IncidentApp.Repositories;

namespace IncidentApp.Services.KnowledgeBase
{
    public class KnowledgeBaseSeedingHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KnowledgeBaseSeedingHostedService> _logger;

        public KnowledgeBaseSeedingHostedService(
            IServiceProvider serviceProvider,
            ILogger<KnowledgeBaseSeedingHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var knowledgeRepository = scope.ServiceProvider.GetRequiredService<IKnowledgeRepository>();
            var chunkingService = scope.ServiceProvider.GetRequiredService<DocumentChunkingService>();

            try
            {
                var existingDocs = await knowledgeRepository.GetDocumentsAsync();
                if (existingDocs.Count() >= 500)
                {
                    _logger.LogInformation($"Knowledge base already has {existingDocs.Count()} documents. Skipping seeding.");
                    return;
                }

                _logger.LogInformation($"Seeding knowledge base with {500 - existingDocs.Count()} additional documents...");

                var categories = new[]
                {
                    "Troubleshooting", "Best Practices", "Security", "Performance", "Architecture",
                    "Database", "API", "DevOps", "Monitoring", "Configuration"
                };

                var sources = new[]
                {
                    "Internal Documentation", "Knowledge Base", "Technical Guides", "Runbooks", "Standard Operating Procedures"
                };

                var targetCount = 500;
                var currentCount = existingDocs.Count();

                for (int i = currentCount + 1; i <= targetCount; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var category = categories[i % categories.Length];
                    var source = sources[i % sources.Length];

                    var document = new KnowledgeDocument
                    {
                        Title = $"Knowledge Document {i} - {category}",
                        Content = GenerateSampleContent(i, category),
                        Category = category,
                        Source = source,
                        CreatedDate = DateTime.UtcNow
                    };

                    try
                    {
                        var createdDocument = await knowledgeRepository.CreateDocumentAsync(document);

                        var chunks = chunkingService.CreateChunks(createdDocument.Id, createdDocument.Content);
                        foreach (var chunk in chunks)
                        {
                            await knowledgeRepository.CreateChunkAsync(chunk);
                        }

                        if (i % 50 == 0)
                        {
                            _logger.LogInformation($"Seeded {i}/{targetCount} documents...");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error seeding document {i}: {ex.Message}");
                    }
                }

                _logger.LogInformation($"Knowledge base seeding completed. Total documents: {targetCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during knowledge base seeding: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private string GenerateSampleContent(int index, string category)
        {
            return $@"# Knowledge Document {index} - {category}

## Overview
This is a comprehensive knowledge document about {category.ToLower()} topics. 
It contains valuable information for troubleshooting, best practices, and operational procedures.

## Key Concepts
- Primary concept related to {category.ToLower()}
- Secondary patterns and methodologies
- Implementation details and considerations

## Implementation Steps
1. Assessment and planning
2. Design and configuration
3. Implementation and testing
4. Deployment and monitoring
5. Maintenance and optimization

## Best Practices
- Follow established patterns for {category.ToLower()}
- Maintain consistency across implementations
- Document all changes and decisions
- Monitor performance and adjust as needed

## Troubleshooting
Common issues and their resolutions:
- Configuration problems: Check settings and validate parameters
- Performance issues: Optimize queries and implement caching
- Integration problems: Verify connections and test endpoints

## References
- Internal documentation on {category.ToLower()}
- Team runbooks and procedures
- Industry best practices
- Vendor documentation

Document ID: {index}
Category: {category}
Generated: {DateTime.UtcNow:yyyy-MM-dd}
";
        }
    }
}
