using Microsoft.AspNetCore.Http;

namespace TicketMasala.Web.Engine.Core;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file, string subDirectory);
    Task<Stream> GetFileStreamAsync(string fileName, string subDirectory);
    string GetContentType(string fileName);
}
