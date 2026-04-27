using Microsoft.Extensions.Configuration;

namespace TicketMasala.Web.Engine.Core;

public class DiskFileStorageService : IFileStorageService
{
    private readonly string _storagePath;

    public DiskFileStorageService(IWebHostEnvironment env, IConfiguration configuration)
    {
        // Allow override via config, default to App_Data/Uploads to be secure by default (not in wwwroot)
        var configPath = configuration["Storage:Path"];
        if (!string.IsNullOrEmpty(configPath))
        {
            _storagePath = Path.IsPathRooted(configPath) 
                ? configPath 
                : Path.Combine(env.ContentRootPath, configPath);
        }
        else
        {
            _storagePath = Path.Combine(env.ContentRootPath, "App_Data", "Uploads");
        }

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> StoreFileAsync(Stream fileStream, string originalFileName)
    {
        // Generate secure filename
        var fileId = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
        var filePath = Path.Combine(_storagePath, fileId);

        using (var destStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(destStream);
        }

        return fileId;
    }

    public Task<Stream> RetrieveFileAsync(string fileId)
    {
        // Prevent directory traversal
        var fileName = Path.GetFileName(fileId);
        var filePath = Path.Combine(_storagePath, fileName);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", fileId);
        }

        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public Task DeleteFileAsync(string fileId)
    {
        var fileName = Path.GetFileName(fileId);
        var filePath = Path.Combine(_storagePath, fileName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}
