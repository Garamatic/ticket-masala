using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Web.Models;
    public class TicketComment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TicketId { get; set; }
        [ForeignKey("TicketId")]
        public Ticket? Ticket { get; set; }

        [Required]
        public string Body { get; set; } = string.Empty;

        public string? AuthorId { get; set; }
        [ForeignKey("AuthorId")]
        public ApplicationUser? Author { get; set; }

        public bool IsInternal { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
