using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketMasala.Web.Controllers; // For ILogger<ImportController> context if we want to keep it, or switch to its own logger
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.Ingestion;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TicketMasala.Web.Engine.Ingestion.Background;

public class TicketImportDispatcher : ITicketImportDispatcher
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketImportDispatcher> _logger;

    public TicketImportDispatcher(
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<TicketImportDispatcher> logger)
    {
        _taskQueue = taskQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task DispatchImportAsync(string fileId, string originalFileName, Dictionary<string, string> mapping, string uploaderId, Guid departmentId)
    {
        // Enqueue Background Job
        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            using var scope = _scopeFactory.CreateScope();
            // We can resolve dependencies from the scope now
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TicketImportDispatcher>>();
            var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

            try
            {
                logger.LogInformation("Starting background import for {FileName} (User: {UserId})", originalFileName, uploaderId);
                var importService = scope.ServiceProvider.GetRequiredService<ITicketImportService>();

                using var stream = await fileStorage.RetrieveFileAsync(fileId);
                var rows = importService.ParseFile(stream, originalFileName);

                var count = await importService.ImportTicketsAsync(rows, mapping, uploaderId, departmentId);
                logger.LogInformation("Background import completed. Imported {Count} tickets.", count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during background import of {FileName}", originalFileName);
            }
            finally
            {
                // Always try to cleanup the file
                try
                {
                    await fileStorage.DeleteFileAsync(fileId);
                }
                catch (Exception deleteEx)
                {
                    logger.LogWarning(deleteEx, "Failed to delete temporary file {FileId}", fileId);
                }
            }
        });
    }
}
