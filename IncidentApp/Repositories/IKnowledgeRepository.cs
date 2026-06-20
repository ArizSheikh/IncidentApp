using IncidentApp.Models.KnowledgeBase;

namespace IncidentApp.Repositories
{
    public interface IKnowledgeRepository
    {
        Task<KnowledgeDocument?> GetDocumentByIdAsync(int id);
        Task<IEnumerable<KnowledgeDocument>> GetDocumentsAsync(int? limit = null);
        Task<KnowledgeDocument> CreateDocumentAsync(KnowledgeDocument document);
        Task<KnowledgeDocument> UpdateDocumentAsync(KnowledgeDocument document);
        Task<bool> DeleteDocumentAsync(int id);
        Task<KnowledgeChunk?> GetChunkByIdAsync(int id);
        Task<KnowledgeChunk> CreateChunkAsync(KnowledgeChunk chunk);
        Task<KnowledgeChunk> UpdateChunkAsync(KnowledgeChunk chunk);
        Task UpdateChunksBatchAsync(IEnumerable<KnowledgeChunk> chunks);
        Task<IEnumerable<KnowledgeChunk>> GetChunksByDocumentIdAsync(int documentId);
        Task<bool> DeleteChunksByDocumentIdAsync(int documentId);
    }
}
