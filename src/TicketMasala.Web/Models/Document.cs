using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Web.Models;
    public class Document
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(255)]
        public required string FileName { get; set; }

        [Required]
        [StringLength(255)]
        public required string StoredFileName { get; set; }

        [StringLength(100)]
        public required string ContentType { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public string? UploaderId { get; set; }
        [ForeignKey("UploaderId")]
        public ApplicationUser? Uploader { get; set; }

        public Guid TicketId { get; set; }
        [ForeignKey("TicketId")]
        public Ticket? Ticket { get; set; }

        public bool IsPublic { get; set; } = false;
}
