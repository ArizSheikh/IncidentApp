using Microsoft.AspNetCore.Mvc;
using IncidentApp.Models.KnowledgeBase;
using IncidentApp.Services.KnowledgeBase;

namespace IncidentApp.Controllers
{
    [ApiController]
    [Route("api/knowledge")]
    public class KnowledgeController : ControllerBase
    {
        private readonly KnowledgeDocumentService _knowledgeDocumentService;

        public KnowledgeController(KnowledgeDocumentService knowledgeDocumentService)
        {
            _knowledgeDocumentService = knowledgeDocumentService;
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
        public async Task<IActionResult> GetDocuments()
        {
            try
            {
                var documents = await _knowledgeDocumentService.GetDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocumentById(int id)
        {
            try
            {
                var document = await _knowledgeDocumentService.GetDocumentByIdAsync(id);
                if (document == null)
                    return NotFound($"Document with ID {id} not found");

                return Ok(document);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
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
    }
}
