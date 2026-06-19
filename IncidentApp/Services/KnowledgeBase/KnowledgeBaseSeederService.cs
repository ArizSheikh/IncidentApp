using IncidentApp.Models.KnowledgeBase;
using IncidentApp.Repositories;
using System.Text;

namespace IncidentApp.Services.KnowledgeBase
{
    public class KnowledgeBaseSeederService
    {
        private readonly IKnowledgeRepository _knowledgeRepository;
        private readonly KnowledgeVectorIndexingService _vectorIndexingService;
        private readonly DocumentChunkingService _chunkingService;

        public KnowledgeBaseSeederService(
            IKnowledgeRepository knowledgeRepository,
            KnowledgeVectorIndexingService vectorIndexingService,
            DocumentChunkingService chunkingService)
        {
            _knowledgeRepository = knowledgeRepository;
            _vectorIndexingService = vectorIndexingService;
            _chunkingService = chunkingService;
        }

        public async Task SeedKnowledgeBaseAsync(int targetCount = 500)
        {
            Console.WriteLine($"Starting knowledge base seeding with {targetCount} documents...");

            var categories = new[]
            {
                "Troubleshooting",
                "Best Practices",
                "Security",
                "Performance",
                "Architecture",
                "Database",
                "API",
                "DevOps",
                "Monitoring",
                "Configuration"
            };

            var sources = new[]
            {
                "Internal Documentation",
                "Knowledge Base",
                "Technical Guides",
                "Runbooks",
                "Standard Operating Procedures"
            };

            var seededCount = 0;

            for (int i = 1; i <= targetCount; i++)
            {
                var category = categories[i % categories.Length];
                var source = sources[i % sources.Length];
                
                var document = GenerateSampleDocument(i, category, source);
                
                try
                {
                    // Create document directly in database
                    var createdDocument = await CreateDocumentAsync(document);
                    
                    // Chunk the document
                    await ChunkDocumentAsync(createdDocument);
                    
                    // Index in vector database
                    await _vectorIndexingService.IndexDocumentAsync(createdDocument.Id);
                    
                    seededCount++;
                    
                    if (seededCount % 50 == 0)
                    {
                        Console.WriteLine($"Seeded {seededCount}/{targetCount} documents...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error seeding document {i}: {ex.Message}");
                }
            }

            Console.WriteLine($"Knowledge base seeding completed. Total documents seeded: {seededCount}");
        }

        private async Task<KnowledgeDocument> CreateDocumentAsync(KnowledgeDocument document)
        {
            return await _knowledgeRepository.CreateDocumentAsync(document);
        }

        private async Task ChunkDocumentAsync(KnowledgeDocument document)
        {
            var chunks = _chunkingService.CreateChunks(document.Id, document.Content);
            foreach (var chunk in chunks)
            {
                await _knowledgeRepository.CreateChunkAsync(chunk);
            }
        }

        private KnowledgeDocument GenerateSampleDocument(int index, string category, string source)
        {
            var topics = GetTopicsForCategory(category);
            var topic = topics[index % topics.Length];
            
            return new KnowledgeDocument
            {
                Title = $"{topic} - Guide {index}",
                Content = GenerateDocumentContent(topic, category, index),
                Category = category,
                Source = source,
                CreatedDate = DateTime.UtcNow
            };
        }

        private string[] GetTopicsForCategory(string category)
        {
            return category switch
            {
                "Troubleshooting" => new[]
                {
                    "Database Connection Issues",
                    "API Performance Problems",
                    "Memory Leaks",
                    "Network Timeout Errors",
                    "Authentication Failures",
                    "File System Errors",
                    "Concurrency Issues",
                    "Cache Problems",
                    "Service Discovery Failures",
                    "Load Balancer Issues"
                },
                "Best Practices" => new[]
                {
                    "Code Review Guidelines",
                    "Testing Strategies",
                    "Documentation Standards",
                    "Code Organization",
                    "Error Handling Patterns",
                    "Logging Best Practices",
                    "Security Guidelines",
                    "Performance Optimization",
                    "Deployment Procedures",
                    "Monitoring Setup"
                },
                "Security" => new[]
                {
                    "Authentication Methods",
                    "Authorization Patterns",
                    "Data Encryption",
                    "Secure Communication",
                    "Input Validation",
                    "Output Encoding",
                    "Session Management",
                    "API Security",
                    "Network Security",
                    "Container Security"
                },
                "Performance" => new[]
                {
                    "Database Optimization",
                    "Caching Strategies",
                    "Load Testing",
                    "Performance Monitoring",
                    "Query Optimization",
                    "Memory Management",
                    "CPU Optimization",
                    "I/O Optimization",
                    "Network Optimization",
                    "Application Profiling"
                },
                "Architecture" => new[]
                {
                    "Microservices Design",
                    "Event-Driven Architecture",
                    "API Design Patterns",
                    "Database Design",
                    "System Integration",
                    "Scalability Patterns",
                    "Resilience Patterns",
                    "Security Architecture",
                    "Deployment Architecture",
                    "Monitoring Architecture"
                },
                "Database" => new[]
                {
                    "Schema Design",
                    "Index Optimization",
                    "Query Performance",
                    "Backup Strategies",
                    "Replication Setup",
                    "Migration Procedures",
                    "Connection Pooling",
                    "Transaction Management",
                    "Data Modeling",
                    "Database Security"
                },
                "API" => new[]
                {
                    "REST API Design",
                    "GraphQL Implementation",
                    "API Versioning",
                    "Authentication",
                    "Rate Limiting",
                    "Error Handling",
                    "Documentation",
                    "Testing",
                    "Monitoring",
                    "Security"
                },
                "DevOps" => new[]
                {
                    "CI/CD Pipelines",
                    "Infrastructure as Code",
                    "Container Orchestration",
                    "Configuration Management",
                    "Deployment Strategies",
                    "Monitoring and Alerting",
                    "Log Management",
                    "Secret Management",
                    "Backup and Recovery",
                    "Disaster Recovery"
                },
                "Monitoring" => new[]
                {
                    "Application Monitoring",
                    "Infrastructure Monitoring",
                    "Log Analysis",
                    "Performance Metrics",
                    "Alerting Strategies",
                    "Dashboard Design",
                    "Root Cause Analysis",
                    "Capacity Planning",
                    "SLA Monitoring",
                    "Health Checks"
                },
                "Configuration" => new[]
                {
                    "Environment Variables",
                    "Configuration Files",
                    "Secret Management",
                    "Feature Flags",
                    "Dynamic Configuration",
                    "Configuration Validation",
                    "Configuration Testing",
                    "Configuration Drift",
                    "Configuration Security",
                    "Configuration Auditing"
                },
                _ => new[] { "General Topic" }
            };
        }

        private string GenerateDocumentContent(string topic, string category, int index)
        {
            var sections = new List<string>
            {
                $"# {topic}",
                "",
                "## Overview",
                $"This comprehensive guide covers {topic.ToLower()} in the context of {category.ToLower()}.",
                "This document provides detailed explanations, best practices, and troubleshooting steps.",
                "",
                "## Background",
                $"Understanding {topic.ToLower()} is essential for maintaining robust and efficient systems.",
                "This guide addresses common challenges and provides proven solutions.",
                "",
                "## Key Concepts",
                $"- **Primary Concept**: The fundamental principle behind {topic.ToLower()}",
                "- **Secondary Concept**: Related patterns and methodologies",
                "- **Implementation Details**: Practical considerations for deployment",
                "",
                "## Implementation Steps",
                "1. **Assessment**: Evaluate current system state and requirements",
                "2. **Planning**: Design the approach based on specific needs",
                "3. **Implementation**: Execute the planned changes systematically",
                "4. **Testing**: Validate the implementation thoroughly",
                "5. **Deployment**: Roll out changes with proper monitoring",
                "",
                "## Best Practices",
                $"- Follow established patterns for {topic.ToLower()}",
                "- Maintain consistency across implementations",
                "- Document all changes and decisions",
                "- Monitor performance and adjust as needed",
                "",
                "## Common Issues",
                $"### Issue 1: {topic} Configuration Problems",
                "Symptoms: Unexpected behavior in system operation",
                "Resolution: Review configuration settings and validate parameters",
                "",
                "### Issue 2: Performance Degradation",
                "Symptoms: Slower response times under load",
                "Resolution: Optimize queries and implement caching strategies",
                "",
                "## Troubleshooting",
                "When encountering issues with this topic, follow these steps:",
                "1. Check system logs for error messages",
                "2. Verify configuration settings",
                "3. Test individual components",
                "4. Review recent changes",
                "5. Consult team documentation",
                "",
                "## Monitoring",
                $"Key metrics to monitor for {topic.ToLower()}:",
                "- Response times",
                "- Error rates",
                "- Resource utilization",
                "- Throughput",
                "",
                "## References",
                $"- Internal documentation on {category.ToLower()}",
                "- Team runbooks and procedures",
                "- Industry best practices",
                "- Vendor documentation",
                "",
                $"## Appendix",
                $"Additional resources and supplementary information about {topic.ToLower()}.",
                "This document is part of the knowledge base and should be updated as practices evolve.",
                "",
                $"Document ID: {index}",
                $"Category: {category}",
                $"Last Updated: {DateTime.UtcNow:yyyy-MM-dd}"
            };

            return string.Join(Environment.NewLine, sections);
        }
    }
}
