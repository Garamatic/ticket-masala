using IT_Project2526.Models;
using IT_Project2526.Models.Configuration;

namespace IT_Project2526.Services.GERDA.Features;

/// <summary>
/// Extracts a numerical feature vector from a Ticket based on configuration.
/// Used for ML model inference (ONNX/ML.NET).
/// </summary>
public interface IFeatureExtractor
{
    /// <summary>
    /// Extract features from a ticket into a float array.
    /// The order of features in the array matches config.Features.
    /// </summary>
    float[] ExtractFeatures(Ticket ticket, GerdaModelConfig config);
}
