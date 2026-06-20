using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IncidentApp.Models.KnowledgeBase
{
    public class KnowledgeChunk
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [Required]
        public int ChunkIndex { get; set; }

        [Required]
        public string ChunkText { get; set; } = string.Empty;

        public bool EmbeddingGenerated { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("DocumentId")]
        public KnowledgeDocument? Document { get; set; }
    }
}
