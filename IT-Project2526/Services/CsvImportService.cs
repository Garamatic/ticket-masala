using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using IT_Project2526.Models;

namespace IT_Project2526.Services
{
    public interface ICsvImportService
    {
        List<dynamic> ParseCsv(Stream fileStream);
        Task<int> ImportTicketsAsync(List<dynamic> rows, Dictionary<string, string> mapping, string uploaderId, Guid departmentId);
    }

    public class CsvImportService : ICsvImportService
    {
        private readonly ITProjectDB _context;
        private readonly ILogger<CsvImportService> _logger;

        public CsvImportService(ITProjectDB context, ILogger<CsvImportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<dynamic> ParseCsv(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null
            });

            return csv.GetRecords<dynamic>().ToList();
        }

        public async Task<int> ImportTicketsAsync(List<dynamic> rows, Dictionary<string, string> mapping, string uploaderId, Guid departmentId)
        {
            int count = 0;
            foreach (var row in rows)
            {
                try
                {
                    var rowDict = (IDictionary<string, object>)row;
                    
                    // 1. Resolve Description
                    string description = "No Description";
                    if (mapping.ContainsKey("Description") && rowDict.ContainsKey(mapping["Description"]))
                    {
                        description = rowDict[mapping["Description"]]?.ToString() ?? "No Description";
                    }

                    // 2. Resolve Customer
                    Customer? customer = null;
                    if (mapping.ContainsKey("CustomerEmail") && rowDict.ContainsKey(mapping["CustomerEmail"]))
                    {
                        var email = rowDict[mapping["CustomerEmail"]]?.ToString();
                        if (!string.IsNullOrEmpty(email))
                        {
                            customer = _context.Customers.FirstOrDefault(u => u.Email == email);
                            if (customer == null)
                            {
                                // Create new customer
                                customer = new Customer
                                {
                                    UserName = email,
                                    Email = email,
                                    FirstName = "Imported",
                                    LastName = "User",
                                    Code = "IMP-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                                    Phone = "N/A"
                                };
                                _context.Customers.Add(customer);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }

                    if (customer == null)
                    {
                        _logger.LogWarning("Skipping row: No customer email provided or found");
                        continue;
                    }

                    // 3. Create Ticket with required properties
                    var ticket = new Ticket
                    {
                        Guid = Guid.NewGuid(),
                        Description = description,
                        Customer = customer,
                        CustomerId = customer.Id,
                        TicketStatus = Status.Pending,
                        ResponsibleId = null
                    };

                    // 4. Set optional properties
                    if (mapping.ContainsKey("TicketType") && rowDict.ContainsKey(mapping["TicketType"]))
                    {
                        if (Enum.TryParse<TicketType>(rowDict[mapping["TicketType"]]?.ToString(), true, out var type))
                            ticket.TicketType = type;
                    }

                    _context.Tickets.Add(ticket);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing row");
                    // Continue to next row
                }
            }

            await _context.SaveChangesAsync();
            return count;
        }
    }
}
