namespace TicketMasala.Web.Utilities;

/// <summary>
/// Helper methods for mapping view model data.
/// </summary>
public static class ViewModelMappers
{
    /// <summary>
    /// Computes the progress percentage based on completed vs total items.
    /// </summary>
    public static int ComputeProgressPercent(int done, int total)
    {
        if (total == 0) return 0;
        return (int)Math.Round((done / (double)total) * 100);
    }
}
