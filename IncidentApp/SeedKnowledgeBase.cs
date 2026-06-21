using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IncidentApp.Data;
using IncidentApp.Models.KnowledgeBase;
using IncidentApp.Repositories;
using IncidentApp.Services.KnowledgeBase;

namespace IncidentApp
{
    public class SeedKnowledgeBase
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")))
                .AddScoped<IKnowledgeRepository, KnowledgeRepository>()
                .AddScoped<DocumentChunkingService>()
                .BuildServiceProvider();

            var count = args.Length > 0 ? int.Parse(args[0]) : 500;

            Console.WriteLine($"Starting knowledge base seeding with {count} documents...");

            using var scope = serviceProvider.CreateScope();
            var knowledgeRepository = scope.ServiceProvider.GetRequiredService<IKnowledgeRepository>();
            var chunkingService = scope.ServiceProvider.GetRequiredService<DocumentChunkingService>();

            var categories = new[]
            {
                "Troubleshooting", "Best Practices", "Security", "Performance", "Architecture",
                "Database", "API", "DevOps", "Monitoring", "Configuration"
            };

            var sources = new[]
            {
                "Internal Documentation", "Knowledge Base", "Technical Guides", "Runbooks", "Standard Operating Procedures"
            };

            var seededCount = 0;

            for (int i = 1; i <= count; i++)
            {
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

                    seededCount++;
                    
                    if (seededCount % 50 == 0)
                    {
                        Console.WriteLine($"Seeded {seededCount}/{count} documents...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error seeding document {i}: {ex.Message}");
                }
            }

            Console.WriteLine($"Knowledge base seeding completed. Total documents seeded: {seededCount}");
        }

        private static string GenerateSampleContent(int index, string category)
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
