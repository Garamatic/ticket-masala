using Microsoft.AspNetCore.Identity;

namespace IT_Project2526.Models
{
    public abstract class BaseModel
    {
        public Guid Guid { get; } = Guid.NewGuid();
        public DateTime CreationDate { get; } = DateTime.UtcNow;
        public DateTime? ValidUntil { get; set;}
        public Guid? CreatorGuid { get; set; }
    }
}
