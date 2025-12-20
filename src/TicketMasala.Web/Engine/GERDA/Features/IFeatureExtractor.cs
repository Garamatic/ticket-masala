using TicketMasala.Web.Models;
using TicketMasala.Web.Models.Configuration;

namespace TicketMasala.Web.Engine.GERDA.Features;

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
