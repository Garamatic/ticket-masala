
using Microsoft.ML.Data;

namespace TicketMasala.Web.Engine.GERDA.Models;

/// <summary>
/// Input data for ML.NET Matrix Factorization model
/// </summary>
public class AgentCustomerRating
{
    public string AgentId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public float Rating { get; set; } // 1-5 scale
}

/// <summary>
/// Prediction output from ML.NET model
/// </summary>
public class RatingPrediction
{
    public float Score { get; set; }
}
