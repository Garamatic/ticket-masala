using Microsoft.ML;

namespace TicketMasala.Web.Engine.GERDA.Persistence;

/// <summary>
/// Service for persisting and loading ML.NET models to/from disk.
/// Supports model versioning linked to DomainConfigVersion for reproducibility.
/// </summary>
public interface IModelPersistenceService
{
    /// <summary>
    /// Saves a trained model to disk with metadata
    /// </summary>
    Task SaveModelAsync(ITransformer model, string modelName, string domainId, string? configVersion = null);

    /// <summary>
    /// Loads a model from disk, returns null if not found
    /// </summary>
    ITransformer? LoadModel(string modelName, string domainId);

    /// <summary>
    /// Gets metadata for a persisted model
    /// </summary>
    ModelInfo? GetModelInfo(string modelName, string domainId);

    /// <summary>
    /// Lists all available models for a domain
    /// </summary>
    IEnumerable<ModelInfo> ListModels(string domainId);
}

/// <summary>
/// Metadata about a persisted ML model
/// </summary>
public class ModelInfo
{
    public required string Name { get; set; }
    public required string DomainId { get; set; }
    public DateTime TrainedAt { get; set; }
    public string? ConfigVersion { get; set; }
    public string? FilePath { get; set; }
    public long FileSizeBytes { get; set; }
}

/// <summary>
/// File-based implementation of model persistence
/// </summary>
public class ModelPersistenceService : IModelPersistenceService
{
    private readonly MLContext _mlContext;
    private readonly ILogger<ModelPersistenceService> _logger;
    private readonly string _modelsPath;

    public ModelPersistenceService(
        MLContext mlContext,
        ILogger<ModelPersistenceService> logger,
        IWebHostEnvironment environment)
    {
        _mlContext = mlContext;
        _logger = logger;
        _modelsPath = Path.Combine(environment.ContentRootPath, "models");

        // Ensure models directory exists
        if (!Directory.Exists(_modelsPath))
        {
            Directory.CreateDirectory(_modelsPath);
            _logger.LogInformation("Created models directory at {Path}", _modelsPath);
        }
    }

    public async Task SaveModelAsync(ITransformer model, string modelName, string domainId, string? configVersion = null)
    {
        var domainPath = Path.Combine(_modelsPath, domainId);
        if (!Directory.Exists(domainPath))
        {
            Directory.CreateDirectory(domainPath);
        }

        var modelPath = Path.Combine(domainPath, $"{modelName}.zip");
        var metadataPath = Path.Combine(domainPath, $"{modelName}.meta.json");

        // Save the model
        _mlContext.Model.Save(model, null, modelPath);

        // Save metadata
        var metadata = new ModelInfo
        {
            Name = modelName,
            DomainId = domainId,
            TrainedAt = DateTime.UtcNow,
            ConfigVersion = configVersion,
            FilePath = modelPath,
            FileSizeBytes = new FileInfo(modelPath).Length
        };

        var json = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(metadataPath, json);

        _logger.LogInformation("Saved model {ModelName} for domain {DomainId} (config: {ConfigVersion})",
            modelName, domainId, configVersion ?? "latest");
    }

    public ITransformer? LoadModel(string modelName, string domainId)
    {
        var modelPath = Path.Combine(_modelsPath, domainId, $"{modelName}.zip");

        if (!File.Exists(modelPath))
        {
            _logger.LogDebug("Model {ModelName} not found for domain {DomainId}", modelName, domainId);
            return null;
        }

        try
        {
            var model = _mlContext.Model.Load(modelPath, out _);
            _logger.LogInformation("Loaded model {ModelName} for domain {DomainId}", modelName, domainId);
            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model {ModelName} for domain {DomainId}", modelName, domainId);
            return null;
        }
    }

    public ModelInfo? GetModelInfo(string modelName, string domainId)
    {
        var metadataPath = Path.Combine(_modelsPath, domainId, $"{modelName}.meta.json");

        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(metadataPath);
            return System.Text.Json.JsonSerializer.Deserialize<ModelInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    public IEnumerable<ModelInfo> ListModels(string domainId)
    {
        var domainPath = Path.Combine(_modelsPath, domainId);

        if (!Directory.Exists(domainPath))
        {
            yield break;
        }

        foreach (var metaFile in Directory.GetFiles(domainPath, "*.meta.json"))
        {
            var info = GetModelInfo(Path.GetFileNameWithoutExtension(metaFile).Replace(".meta", ""), domainId);
            if (info != null)
            {
                yield return info;
            }
        }
    }
}
