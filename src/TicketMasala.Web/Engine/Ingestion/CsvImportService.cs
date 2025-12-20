namespace TicketMasala.Web.Engine.Ingestion;

using System.Data;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using TicketMasala.Web.Models;
using TicketMasala.Web.Data;

public interface ITicketImportService
{
    List<dynamic> ParseFile(Stream fileStream, string fileName);
    Task<int> ImportTicketsAsync(List<dynamic> rows, Dictionary<string, string> mapping, string uploaderId, Guid departmentId);
}

public class TicketImportService : ITicketImportService
{
    private const string DefaultTicketTitle = "Imported Ticket";
    private const string DefaultTicketDescription = "No Description";

    private readonly MasalaDbContext _context;
    private readonly ILogger<TicketImportService> _logger;

    public TicketImportService(MasalaDbContext context, ILogger<TicketImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<dynamic> ParseFile(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();

        if (extension == ".csv")
        {
            return ParseCsv(fileStream);
        }
        else if (extension == ".xlsx" || extension == ".xls")
        {
            return ParseExcel(fileStream);
        }
        else
        {
            throw new NotSupportedException($"File extension {extension} is not supported.");
        }
    }

    private List<dynamic> ParseCsv(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null
        });

        return csv.GetRecords<dynamic>().ToList();
    }

    private List<dynamic> ParseExcel(Stream fileStream)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
        {
            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
            {
                UseHeaderRow = true
            }
        });

        var dataTable = result.Tables[0];
        var rows = new List<dynamic>();

        foreach (DataRow row in dataTable.Rows)
        {
            var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
            foreach (DataColumn col in dataTable.Columns)
            {
                expando[col.ColumnName] = row[col];
            }
            rows.Add(expando);
        }

        return rows;
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
                string description = DefaultTicketDescription;
                if (mapping.ContainsKey("Description") && rowDict.ContainsKey(mapping["Description"]))
                {
                    description = rowDict[mapping["Description"]]?.ToString() ?? DefaultTicketDescription;
                }

                // 2. Resolve Customer
                ApplicationUser? customer = null;
                if (mapping.ContainsKey("CustomerEmail") && rowDict.ContainsKey(mapping["CustomerEmail"]))
                {
                    var email = rowDict[mapping["CustomerEmail"]]?.ToString();
                    if (!string.IsNullOrEmpty(email))
                    {
                        customer = _context.Users.FirstOrDefault(u => u.Email == email);
                        if (customer == null)
                        {
                            // Create new customer
                            customer = new ApplicationUser
                            {
                                UserName = email,
                                Email = email,
                                FirstName = "Imported",
                                LastName = "User",
                                Code = "IMP-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                                Phone = "N/A"
                            };
                            _context.Users.Add(customer);
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
                string title;
                if (rowDict.ContainsKey("Title") && rowDict["Title"] != null)
                {
                    title = rowDict["Title"].ToString() ?? DefaultTicketTitle;
                }
                else
                {
                    // Fallback to description
                    title = description.Split('\n')[0];
                    if (title.Length > 100) title = title.Substring(0, 100);
                    if (string.IsNullOrWhiteSpace(title)) title = DefaultTicketTitle;
                }
                var ticket = new Ticket
                {
                    Guid = Guid.NewGuid(),
                    Title = title,
                    Description = description,
                    DomainId = "IT",
                    Status = "New",
                    CreatorGuid = Guid.Parse(customer.Id),
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
