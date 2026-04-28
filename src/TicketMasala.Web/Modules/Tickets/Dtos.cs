namespace TicketMasala.Web.Modules.Tickets;

public record TicketDetailsDto(
    Guid Guid,
    string Title,
    string Description,
    string Status,
    DateTime CreationDate,
    DateTime? CompletionTarget,
    string? ResponsibleName,
    string? CustomerName,
    string? ProjectName,
    double PriorityScore,
    string? GerdaTags,
    IReadOnlyList<string> ValidNextStatuses);

public record TicketSearchQuery(
    string? SearchTerm,
    string? Status,
    string? ResponsibleId,
    string? CustomerId,
    Guid? ProjectId,
    int Page = 1,
    int PageSize = 20);

public record TicketSearchResult(
    IReadOnlyList<TicketSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record TicketSummaryDto(
    Guid Guid,
    string Title,
    string Status,
    DateTime CreationDate,
    string? ResponsibleName);
