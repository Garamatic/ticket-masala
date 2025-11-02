using System.Collections.Generic;

namespace IT_Project2526.ViewModels
{
    public class CustomerDetailViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Code { get; set; }
        public List<ProjectViewModel> Projects { get; set; } = new List<ProjectViewModel>();
    }
}
