Next Steps

We have the Structure (ERD) and the Logic (Compiler).

The last massive risk in your RFC is Gerda AI Integration.

1. The Architectural Pivot: Dynamic Feature Extraction

We need to replace the ai_prompts configuration with a feature_mapping configuration.

The Workflow:

    Hydration: Load Ticket + Custom Fields.

    Extraction: Map specific JSON fields to Model Inputs (Features).

    Transformation: Normalize/One-Hot Encode (if not done within the model pipeline).

    Inference: Pass vector to Model (ONNX/ML.NET).

    Output: Score (Priority), Class (Dispatch Group), or Value (Effort).

2. The New Configuration (YAML)

Forget prompts. You need to tell the system how to read the JSON for the model.

File: masala_gerda.yaml

domains:
  Gardening:
    # ----------------------------------------------------
    # RANKING MODEL (Regression/Learning to Rank)
    # Goal: Output a float score 0.0 - 1.0
    # ----------------------------------------------------
    ranking_model:
      type: "ONNX"
      path: "models/gardening_ranker_v2.onnx"

      # The critical part: Mapping JSON fields to Model Inputs
      features:
        - name: "soil_ph_norm"
          source_field: "soil_ph"
          type: "number"
          transformation: "min_max_scale" # Optional: if model expects 0-1
          params: { min: 0, max: 14 }
          
        - name: "is_shade"
          source_field: "sunlight_exposure"
          type: "categorical"
          transformation: "one_hot"
          # Maps "Full Shade" -> 1.0, others -> 0.0
          params: { target_value: "Full Shade" } 

        - name: "days_since_water"
          source_type: "derived" # Calculated in code, not raw field
          handler: "DaysSinceDate"
          params: { field: "last_watering_date" }

    # ----------------------------------------------------
    # DISPATCH MODEL (Multi-class Classification)
    # Goal: Output "Zone A", "Zone B", "Specialist"
    # ----------------------------------------------------
    dispatch_model:
      type: "ML.NET"
      path: "models/gardening_dispatcher.zip"
      features:
        - name: "lat"
          source_field: "geo_lat"
        - name: "long"
          source_field: "geo_long"

3. The Implementation: IFeatureExtractor

Since we are in .NET, and you want performance, we use ML.NET (Microsoft.ML) or ONNX Runtime. This keeps the inference in-process (super fast, no HTTP overhead), which is critical for ranking 50 tickets on a dashboard instantly.

The Interface:

public interface IFeatureExtractor
{
    // Returns a dictionary of input names to values, ready for the model
    float[] ExtractFeatures(WorkItem ticket, GerdaModelDefinition config);
}

The Implementation (The Bridge):

public class DynamicFeatureExtractor : IFeatureExtractor
{
    public float[] ExtractFeatures(WorkItem ticket, GerdaModelDefinition config)
    {
        var features = new List<float>();

        foreach (var featureDef in config.Features)
        {
            float value = 0f;

            // 1. EXTRACT RAW VALUE
            // Reuse the FieldExtractor we built for the Rule Engine!
            if (featureDef.SourceType == "field")
            {
                 // Gets raw number or converts bool/string
                 value = FieldExtractor.GetNumber(ticket.CustomFieldsJson, featureDef.SourceField);
            }
            else if (featureDef.SourceType == "derived")
            {
                 // Handle calculated fields (Time delta, etc)
                 value = CalculateDerived(ticket, featureDef);
            }

            // 2. TRANSFORM (Scaling/Normalization)
            // Ideally, your Data Science team puts scaling INSIDE the ONNX pipeline.
            // But if done here:
            if (featureDef.Transformation == "min_max_scale")
            {
                float min = featureDef.Params["min"];
                float max = featureDef.Params["max"];
                value = (value - min) / (max - min);
            }

            features.Add(value);
        }

        return features.ToArray();
    }
}

4. Integration Strategy: The GerdaSidecar Decision

Since you are strictly ML (not GenAI), you have a choice to make on Stack Pragmatism:
Option A: ML.NET (Stay in .NET)

    Pros: Runs inside your IIS/Container process. Zero latency. Strongly typed.

    Cons: Your Data Scientists likely work in Python (scikit-learn/PyTorch). They hate converting models to .zip or .onnx (though ONNX is getting standard).

    Verdict: Best for "Ticket Masala" if the team is small and .NET-centric.

5. Revised Architecture Diagram (ML Flow)

sequenceDiagram
    participant UI as Dashboard
    participant API as Ticket Service
    participant FE as Feature Extractor
    participant ML as ML Engine (ONNX)

    UI->>API: Get Tickets (Ranked)
    loop For Each Ticket
        API->>FE: ExtractFeatures(Ticket, DomainConfig)
        FE->>FE: Parse JSONB -> Float[]
        FE-->>API: Vector [0.2, 0.9, 4.5]
        API->>ML: Predict(Vector)
        ML-->>API: Score (0.87)
    end
    API->>UI: Return Sorted List

Final RFC Critique for "Gerda ML"

    Delete ai_prompts from YAML.

    Add feature_mapping to YAML.

    Ensure the WorkItem table has columns for cached inference results (PriorityScore, EffortEstimate). You do not want to run the ML model every time you sort the grid.

        Trigger: Run ML on Ticket Created and Ticket Updated.

        Storage: Persist the score.

        Read: Sort by the persisted column.

We have the Data (Hybrid Schema), the Logic (Compiled Rules), and the Intelligence (Dynamic Feature Extraction).
