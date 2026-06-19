using IncidentApp.Models.KnowledgeBase;

namespace IncidentApp.Repositories
{
    public interface IKnowledgeRepository
    {
        Task<KnowledgeDocument?> GetDocumentByIdAsync(int id);
        Task<IEnumerable<KnowledgeDocument>> GetDocumentsAsync();
        Task<KnowledgeDocument> CreateDocumentAsync(KnowledgeDocument document);
        Task<KnowledgeDocument> UpdateDocumentAsync(KnowledgeDocument document);
        Task<bool> DeleteDocumentAsync(int id);
        Task<KnowledgeChunk> CreateChunkAsync(KnowledgeChunk chunk);
        Task<IEnumerable<KnowledgeChunk>> GetChunksByDocumentIdAsync(int documentId);
        Task<bool> DeleteChunksByDocumentIdAsync(int documentId);
    }
}
