using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Web.Models;
    public class KnowledgeBaseArticle
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string Tags { get; set; } = string.Empty; // Comma separated tags

        public string? AuthorId { get; set; }
        [ForeignKey("AuthorId")]
        public ApplicationUser? Author { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Department link removed
        // public Guid? DepartmentId { get; set; }
        // public Department? Department { get; set; }
}
