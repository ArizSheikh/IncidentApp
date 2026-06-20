using Microsoft.AspNetCore.Mvc;
using IncidentApp.Models.KnowledgeBase;
using IncidentApp.Services.KnowledgeBase;
using IncidentApp.Repositories;

namespace IncidentApp.Controllers
{
    [ApiController]
    [Route("api/knowledge")]
    public class KnowledgeController : ControllerBase
    {
        private readonly KnowledgeDocumentService _knowledgeDocumentService;
        private readonly KnowledgeRetrievalService _knowledgeRetrievalService;
        private readonly KnowledgeVectorIndexingService _vectorIndexingService;
        private readonly IKnowledgeRepository _knowledgeRepository;

        public KnowledgeController(
            KnowledgeDocumentService knowledgeDocumentService,
            KnowledgeRetrievalService knowledgeRetrievalService,
            KnowledgeVectorIndexingService vectorIndexingService,
            IKnowledgeRepository knowledgeRepository)
        {
            _knowledgeDocumentService = knowledgeDocumentService;
            _knowledgeRetrievalService = knowledgeRetrievalService;
            _vectorIndexingService = vectorIndexingService;
            _knowledgeRepository = knowledgeRepository;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(
            [FromForm] IFormFile file,
            [FromForm] string title,
            [FromForm] string category,
            [FromForm] string source)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("File is required");

                if (string.IsNullOrWhiteSpace(title))
                    return BadRequest("Title is required");

                if (string.IsNullOrWhiteSpace(category))
                    return BadRequest("Category is required");

                if (string.IsNullOrWhiteSpace(source))
                    return BadRequest("Source is required");

                using var stream = file.OpenReadStream();
                var document = await _knowledgeDocumentService.UploadDocumentAsync(
                    title,
                    category,
                    source,
                    stream,
                    file.FileName);

                return Ok(new
                {
                    message = "Document uploaded successfully",
                    documentId = document.Id,
                    title = document.Title,
                    category = document.Category,
                    chunkCount = document.Chunks.Count
                });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] int? limit = null)
        {
            try
            {
                var documents = await _knowledgeDocumentService.GetDocumentsAsync(limit);
                var documentsList = documents.ToList();
                return Ok(new
                {
                    totalDocuments = documentsList.Count,
                    documents = documentsList.Select(d => new
                    {
                        d.Id,
                        d.Title,
                        d.Category,
                        d.Source,
                        d.CreatedDate,
                        chunkCount = d.Chunks.Count
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var allDocuments = await _knowledgeRepository.GetDocumentsAsync();
                var documentsList = allDocuments.ToList();
                
                var documentsWithChunks = documentsList.Where(d => d.Chunks.Any()).ToList();
                var documentsWithoutChunks = documentsList.Where(d => !d.Chunks.Any()).ToList();

                return Ok(new
                {
                    totalDocuments = documentsList.Count,
                    documentsWithChunks = documentsWithChunks.Count,
                    documentsWithoutChunks = documentsWithoutChunks.Count,
                    documentsWithoutChunkIds = documentsWithoutChunks.Select(d => d.Id).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDocumentById(int id)
        {
            try
            {
                var document = await _knowledgeDocumentService.GetDocumentByIdAsync(id);
                if (document == null)
                    return NotFound($"Document with ID {id} not found");

                return Ok(new
                {
                    document.Id,
                    document.Title,
                    document.Category,
                    document.Source,
                    document.Content,
                    document.CreatedDate,
                    document.UpdatedDate,
                    chunks = document.Chunks.Select(c => new
                    {
                        c.Id,
                        c.ChunkIndex,
                        c.ChunkText,
                        c.EmbeddingGenerated
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var result = await _knowledgeDocumentService.DeleteDocumentAsync(id);
                if (!result)
                    return NotFound($"Document with ID {id} not found");

                return Ok(new { message = $"Document with ID {id} deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchKnowledge([FromQuery] string query, [FromQuery] int limit = 5, [FromQuery] float scoreThreshold = 0.7f)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Query parameter is required");

                var retrievalResult = await _knowledgeRetrievalService.RetrieveRelevantKnowledgeAsync(query, limit, scoreThreshold);

                return Ok(new
                {
                    query = retrievalResult.Query,
                    retrievedAt = retrievalResult.RetrievedAt,
                    documents = retrievalResult.Documents.Select(doc => new
                    {
                        id = doc.Id,
                        title = doc.Title,
                        category = doc.Category,
                        source = doc.Source,
                        content = doc.Content,
                        createdDate = doc.CreatedDate,
                        chunkCount = doc.Chunks.Count
                    }),
                    chunks = retrievalResult.KnowledgeChunks.Select(chunk => new
                    {
                        id = chunk.Id,
                        documentId = chunk.DocumentId,
                        chunkIndex = chunk.ChunkIndex,
                        chunkText = chunk.ChunkText,
                        similarityScore = retrievalResult.SimilarityScores.TryGetValue(chunk.Id, out var score) ? score : 0f
                    }),
                    totalDocuments = retrievalResult.Documents.Count,
                    totalChunks = retrievalResult.KnowledgeChunks.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("initialize-vector-index")]
        public async Task<IActionResult> InitializeVectorIndex()
        {
            try
            {
                await _vectorIndexingService.InitializeCollectionAsync();
                return Ok(new { message = "Vector collection initialized successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("index/{id}")]
        public async Task<IActionResult> IndexDocument(int id)
        {
            try
            {
                var document = await _knowledgeRepository.GetDocumentByIdAsync(id);
                if (document == null)
                    return NotFound($"Document with ID {id} not found");

                await _vectorIndexingService.IndexDocumentAsync(document);

                return Ok(new
                {
                    message = $"Document {id} indexed successfully",
                    documentId = id,
                    chunkCount = document.Chunks.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("index-all")]
        public async Task<IActionResult> IndexAllDocuments()
        {
            try
            {
                var documents = await _knowledgeRepository.GetDocumentsAsync();
                var documentsList = documents.ToList();
                
                if (!documentsList.Any())
                    return Ok(new { message = "No documents to index", indexedCount = 0 });

                var indexedCount = 0;
                var errorCount = 0;
                var semaphore = new SemaphoreSlim(2);

                await Task.WhenAll(documentsList.Select(async document =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await _vectorIndexingService.IndexDocumentAsync(document);
                        Interlocked.Increment(ref indexedCount);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref errorCount);
                        Console.WriteLine($"Error indexing document {document.Id}: {ex.Message}");
                    }
                    finally { semaphore.Release(); }
                }));

                return Ok(new 
                { 
                    message = $"Indexing completed. Successfully indexed {indexedCount} documents. {errorCount} errors.",
                    indexedCount,
                    errorCount,
                    totalDocuments = documentsList.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
