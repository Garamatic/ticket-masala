using Microsoft.AspNetCore.Http;

namespace IT_Project2526.Services.Core;
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string subDirectory);
        Task<Stream> GetFileStreamAsync(string fileName, string subDirectory);
        string GetContentType(string fileName);
    }
}
