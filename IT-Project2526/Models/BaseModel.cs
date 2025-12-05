using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Models
{
    public abstract class BaseModel
    {
        [Key]
        public Guid Guid { get; set; } = Guid.NewGuid();
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public DateTime? ValidUntil { get; set;}
        public Guid? CreatorGuid { get; set; }
    }
}
