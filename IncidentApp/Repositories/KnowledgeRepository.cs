using IncidentApp.Data;
using IncidentApp.Models.KnowledgeBase;
using Microsoft.EntityFrameworkCore;

namespace IncidentApp.Repositories
{
    public class KnowledgeRepository : IKnowledgeRepository
    {
        private readonly AppDbContext _context;

        public KnowledgeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<KnowledgeDocument?> GetDocumentByIdAsync(int id)
        {
            return await _context.KnowledgeDocuments
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<KnowledgeDocument>> GetDocumentsAsync(int? limit = null)
        {
            var query = _context.KnowledgeDocuments
                .Include(d => d.Chunks)
                .OrderByDescending(d => d.CreatedDate);

            return limit.HasValue
                ? await query.Take(limit.Value).ToListAsync()
                : await query.ToListAsync();
        }

        public async Task<KnowledgeDocument> CreateDocumentAsync(KnowledgeDocument document)
        {
            _context.KnowledgeDocuments.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<KnowledgeDocument> UpdateDocumentAsync(KnowledgeDocument document)
        {
            document.UpdatedDate = DateTime.UtcNow;
            _context.KnowledgeDocuments.Update(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            var document = await _context.KnowledgeDocuments.FindAsync(id);
            if (document == null)
                return false;

            _context.KnowledgeDocuments.Remove(document);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<KnowledgeChunk?> GetChunkByIdAsync(int id)
        {
            return await _context.KnowledgeChunks.FindAsync(id);
        }

        public async Task<KnowledgeChunk> CreateChunkAsync(KnowledgeChunk chunk)
        {
            _context.KnowledgeChunks.Add(chunk);
            await _context.SaveChangesAsync();
            return chunk;
        }

        public async Task<KnowledgeChunk> UpdateChunkAsync(KnowledgeChunk chunk)
        {
            _context.KnowledgeChunks.Update(chunk);
            await _context.SaveChangesAsync();
            return chunk;
        }

        public async Task UpdateChunksBatchAsync(IEnumerable<KnowledgeChunk> chunks)
        {
            foreach (var chunk in chunks)
                _context.KnowledgeChunks.Update(chunk);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<KnowledgeChunk>> GetChunksByDocumentIdAsync(int documentId)
        {
            return await _context.KnowledgeChunks
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync();
        }

        public async Task<bool> DeleteChunksByDocumentIdAsync(int documentId)
        {
            var chunks = await _context.KnowledgeChunks
                .Where(c => c.DocumentId == documentId)
                .ToListAsync();

            if (chunks.Any())
            {
                _context.KnowledgeChunks.RemoveRange(chunks);
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}
