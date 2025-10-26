using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IT_Project2526.ViewModels
{
    public class NewProject
    {
        // Projectvelden
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        // De projectmanager wordt meestal ingelogd (of uit een lijst gehaald)
        // en hoeft hier niet in de ViewModel te staan, tenzij de user deze moet kunnen kiezen.

        // Klantenkeuze
        public bool IsNewCustomer { get; set; } = true; // Standaard staat 'Nieuwe klant' geselecteerd
        public Guid? SelectedCustomerId { get; set; }
        public List<SelectListItem> CustomerList { get; set; }

        // Velden voor een nieuwe klant
        public string? NewCustomerName { get; set; }
        public string? NewCustomerEmail { get; set; }
        public string? NewCustomerPhone { get; set; }
    }
}
