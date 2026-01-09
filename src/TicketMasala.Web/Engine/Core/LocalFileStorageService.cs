namespace TicketMasala.Web.Engine.Core;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _tempPath;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _tempPath = Path.Combine(env.ContentRootPath, "TempUploads");
        if (!Directory.Exists(_tempPath))
        {
            Directory.CreateDirectory(_tempPath);
        }
    }

    public async Task<string> StoreFileAsync(Stream fileStream, string originalFileName)
    {
        var fileId = Guid.NewGuid().ToString() + Path.GetExtension(originalFileName);
        var filePath = Path.Combine(_tempPath, fileId);

        using (var destStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(destStream);
        }

        return fileId;
    }

    public Task<Stream> RetrieveFileAsync(string fileId)
    {
        var filePath = Path.Combine(_tempPath, fileId);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", fileId);
        }

        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public Task DeleteFileAsync(string fileId)
    {
        var filePath = Path.Combine(_tempPath, fileId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}
