using System.Globalization;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Configuration;
using TicketMasala.Web.Engine.Compiler;

namespace TicketMasala.Web.Engine.GERDA.Features;

public class DynamicFeatureExtractor : IFeatureExtractor
{
    private readonly ILogger<DynamicFeatureExtractor> _logger;

    public DynamicFeatureExtractor(ILogger<DynamicFeatureExtractor> logger)
    {
        _logger = logger;
    }

    public float[] ExtractFeatures(Ticket ticket, GerdaModelConfig config)
    {
        var features = new List<float>();

        foreach (var featureDef in config.Features)
        {
            try
            {
                // 1. Extract Raw Value using FieldExtractor
                // We primarily support Numbers and Strings (categorical)
                double rawValue = 0;
                string? rawString = null;

                // Try to get as number first
                rawValue = FieldExtractor.GetNumber(ticket.CustomFieldsJson, featureDef.SourceField);

                // If it's 0, it might be a categorical string or truly 0.
                if (rawValue == 0)
                {
                    rawString = FieldExtractor.GetString(ticket.CustomFieldsJson, featureDef.SourceField);
                }

                // 2. Apply Transformation
                var value = ApplyTransformation(featureDef, rawValue, rawString);
                features.Add(value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract feature {FeatureName} from ticket {TicketId}", featureDef.Name, ticket.Guid);
                features.Add(0f); // Fallback
            }
        }

        return features.ToArray();
    }

    private float ApplyTransformation(FeatureDefinition def, double rawNumber, string? rawString)
    {
        switch (def.Transformation.ToLowerInvariant())
        {
            case "min_max":
                return ApplyMinMax(def, rawNumber);
            case "one_hot":
                return ApplyOneHot(def, rawString);
            case "bool":
                return ApplyBool(def, rawString);
            default:
                return (float)rawNumber; // Raw pass-through
        }
    }

    private float ApplyMinMax(FeatureDefinition def, double value)
    {
        if (def.Params.TryGetValue("min", out var minObj) && def.Params.TryGetValue("max", out var maxObj))
        {
            float min = Convert.ToSingle(minObj);
            float max = Convert.ToSingle(maxObj);

            if (max == min) return 0f;

            var val = (float)value;
            // Clamp
            if (val < min) val = min;
            if (val > max) val = max;

            return (val - min) / (max - min);
        }
        return (float)value;
    }

    private float ApplyOneHot(FeatureDefinition def, string? value)
    {
        if (def.Params.TryGetValue("target_value", out var targetObj) && targetObj != null)
        {
            string target = targetObj.ToString() ?? "";
            string current = value ?? "";
            return string.Equals(target, current, StringComparison.OrdinalIgnoreCase) ? 1.0f : 0.0f;
        }
        return 0f;
    }

    private float ApplyBool(FeatureDefinition def, string? value)
    {
        if (bool.TryParse(value, out bool boolVal))
        {
            return boolVal ? 1.0f : 0.0f;
        }
        return 0f;
    }

}
