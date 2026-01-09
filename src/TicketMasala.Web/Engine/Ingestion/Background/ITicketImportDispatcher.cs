using System.Threading.Tasks;

namespace TicketMasala.Web.Engine.Ingestion.Background;

public interface ITicketImportDispatcher
{
    Task DispatchImportAsync(string fileId, string originalFileName, Dictionary<string, string> mapping, string uploaderId, Guid departmentId);
}
