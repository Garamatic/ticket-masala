namespace IT_Project2526.Models
{
    public class Customer : BaseModel
    {
        public required string Name { get; set; }
        public string? Code { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
    }
}
