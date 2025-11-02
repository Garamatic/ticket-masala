using IT_Project2526.Models;

namespace IT_Project2526.ViewModels
{
    public class ProjectViewModel
    {
       public string Name { get; set; }
       public string Description { get; set; }
       public Status Status { get; set; }
       public Employee ProjectManager {  get; set; }

    }
}
