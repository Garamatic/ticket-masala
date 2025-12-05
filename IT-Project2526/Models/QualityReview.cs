using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Project2526.Models
{
    public class QualityReview
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TicketId { get; set; }
        [ForeignKey("TicketId")]
        public Ticket? Ticket { get; set; }

        public string? ReviewerId { get; set; }
        [ForeignKey("ReviewerId")]
        public Employee? Reviewer { get; set; }

        [Range(1, 5)]
        public int Score { get; set; } // 1-5 stars

        public string Feedback { get; set; } = string.Empty;

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
    }
}
