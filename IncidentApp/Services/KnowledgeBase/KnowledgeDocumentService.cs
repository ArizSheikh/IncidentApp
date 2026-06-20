using IncidentApp.Models.KnowledgeBase;
using IncidentApp.Repositories;

namespace IncidentApp.Services.KnowledgeBase
{
    public class KnowledgeDocumentService
    {
        private readonly IKnowledgeRepository _knowledgeRepository;
        private readonly IEnumerable<ITextExtractionService> _textExtractionServices;
        private readonly DocumentChunkingService _chunkingService;
        private readonly KnowledgeVectorIndexingService _vectorIndexingService;

        public KnowledgeDocumentService(
            IKnowledgeRepository knowledgeRepository,
            IEnumerable<ITextExtractionService> textExtractionServices,
            DocumentChunkingService chunkingService,
            KnowledgeVectorIndexingService vectorIndexingService)
        {
            _knowledgeRepository = knowledgeRepository;
            _textExtractionServices = textExtractionServices;
            _chunkingService = chunkingService;
            _vectorIndexingService = vectorIndexingService;
        }

        public async Task<KnowledgeDocument> UploadDocumentAsync(
            string title,
            string category,
            string source,
            Stream fileStream,
            string fileName)
        {
            // Extract text from file
            var text = await ExtractTextFromFileAsync(fileStream, fileName);
            
            // Create document
            var document = new KnowledgeDocument
            {
                Title = title,
                Category = category,
                Source = source,
                Content = text,
                CreatedDate = DateTime.UtcNow
            };

            // Save document
            var savedDocument = await _knowledgeRepository.CreateDocumentAsync(document);
            
            // Create chunks
            var chunks = _chunkingService.CreateChunks(savedDocument.Id, text);
            
            // Save chunks
            foreach (var chunk in chunks)
            {
                await _knowledgeRepository.CreateChunkAsync(chunk);
            }

            // Generate embeddings and index in Qdrant
            await _vectorIndexingService.IndexDocumentAsync(savedDocument.Id);

            return savedDocument;
        }

        public async Task<IEnumerable<KnowledgeDocument>> GetDocumentsAsync(int? limit = null)
        {
            return await _knowledgeRepository.GetDocumentsAsync(limit);
        }

        public async Task<KnowledgeDocument?> GetDocumentByIdAsync(int id)
        {
            return await _knowledgeRepository.GetDocumentByIdAsync(id);
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            // Remove from Qdrant first
            await _vectorIndexingService.RemoveDocumentAsync(id);
            
            // Delete chunks from database
            await _knowledgeRepository.DeleteChunksByDocumentIdAsync(id);
            
            // Delete document
            return await _knowledgeRepository.DeleteDocumentAsync(id);
        }

        private async Task<string> ExtractTextFromFileAsync(Stream fileStream, string fileName)
        {
            var extractionService = _textExtractionServices.FirstOrDefault(s => s.CanHandle(fileName));
            
            if (extractionService == null)
            {
                throw new NotSupportedException($"No text extraction service found for file: {fileName}");
            }

            return await extractionService.ExtractTextAsync(fileStream, fileName);
        }
    }
}
