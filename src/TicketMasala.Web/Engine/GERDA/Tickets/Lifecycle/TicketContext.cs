namespace TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

/// <summary>
/// Ambient execution context for ticket lifecycle operations.
/// Carries user identity, tenant, and timestamp — never pulled from HTTP context inside the module.
/// </summary>
public sealed record TicketContext(
    string UserId,
    string? TenantId = null,
    DateTime? ExecutedAt = null
)
{
    public DateTime ExecutedAtOrNow => ExecutedAt ?? DateTime.UtcNow;
}
