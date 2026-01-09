namespace TicketMasala.Web.Engine.Core;

public interface IFileStorageService
{
    Task<string> StoreFileAsync(Stream fileStream, string originalFileName);
    Task<Stream> RetrieveFileAsync(string fileId);
    Task DeleteFileAsync(string fileId);
}
