using System.Text.Json;
using TicketMasala.Domain.Configuration;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Facades;

// Note: CancellationToken parameters are added for interface consistency and future-proofing.
// Currently, the underlying services don't fully support cancellation, but this prepares for that.

/// <summary>
/// Facade for ticket context operations.
/// Uses composition with specialized services to maintain Single Responsibility.
/// </summary>
public class TicketContextFacade : ITicketContextFacade
{
    private readonly ITicketDetailService _detailService;
    private readonly ITicketCreateService _createService;
    private readonly ITicketEditService _editService;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly ILogger<TicketContextFacade> _logger;

    public TicketContextFacade(
        ITicketDetailService detailService,
        ITicketCreateService createService,
        ITicketEditService editService,
        IDomainConfigurationService domainConfig,
        ILogger<TicketContextFacade> logger)
    {
        _detailService = detailService;
        _createService = createService;
        _editService = editService;
        _domainConfig = domainConfig;
        _logger = logger;
    }

    public async Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketId, string? userId, bool isCustomer, CancellationToken ct = default)
    {
        // Note: CancellationToken is accepted but not yet passed to underlying service (future enhancement)
        return await _detailService.GetTicketDetailsAsync(ticketId, userId, isCustomer).ConfigureAwait(false);
    }

    public async Task<TicketDetailContext> GetTicketDetailContextAsync(TicketDetailsViewModel viewModel)
    {
        // Note: This method doesn't accept CancellationToken because it performs only
        // synchronous in-memory operations (deserialization, object construction).
        var domainId = viewModel.DomainId ?? _domainConfig.GetDefaultDomainId();

        var context = new TicketDetailContext
        {
            DomainId = domainId,
            EntityLabels = _domainConfig.GetEntityLabels(domainId),
            CustomFields = _domainConfig.GetCustomFields(domainId).ToList(),
            WorkItemTypeCode = viewModel.WorkItemTypeCode
        };

        if (!string.IsNullOrEmpty(viewModel.CustomFieldsJson))
        {
            try
            {
                context.CustomFieldValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(viewModel.CustomFieldsJson)
                    ?? new Dictionary<string, JsonElement>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize CustomFieldsJson for ticket context");
                context.CustomFieldValues = new Dictionary<string, JsonElement>();
            }
        }

        return context;
    }

    public async Task<TicketCreateContext> GetCreateContextAsync(bool isCustomer, string? preselectedCustomerId = null, Guid? projectGuid = null, CancellationToken ct = default)
    {
        // Note: CancellationToken is accepted but not yet passed to underlying service (future enhancement)
        var context = await _createService.GetCreateContextAsync(isCustomer, preselectedCustomerId, projectGuid).ConfigureAwait(false);

        // Domain configuration is still handled here as it's configuration, not data
        var defaultDomain = _domainConfig.GetDefaultDomainId();
        context.DomainId = defaultDomain;
        context.EntityLabels = _domainConfig.GetEntityLabels(defaultDomain);
        context.WorkItemTypes = _domainConfig.GetWorkItemTypes(defaultDomain).ToList();
        context.CustomFields = _domainConfig.GetCustomFields(defaultDomain).ToList();

        return context;
    }

    public async Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct = default)
    {
        // Note: CancellationToken is accepted but not yet passed to underlying service (future enhancement)
        var context = await _editService.GetEditContextAsync(ticketId, user).ConfigureAwait(false);

        if (context != null)
        {
            // Domain configuration is handled here as it's configuration, not data
            // DomainId is already set by TicketEditService from ticket.DomainId
            context.EntityLabels = _domainConfig.GetEntityLabels(context.DomainId);
            context.CustomFields = _domainConfig.GetCustomFields(context.DomainId).ToList();

            // CustomFieldValues is already populated by TicketEditService from ticket.CustomFieldsJson
            // No additional processing needed here
        }

        return context;
    }

    public async Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct = default)
    {
        // Note: CancellationToken is accepted but not yet passed to underlying service (future enhancement)
        var context = await _editService.GetEditReloadContextAsync(ticketId, user).ConfigureAwait(false);

        // DomainId and WorkItemTypeCode are already set by TicketEditService from the ticket
        // Only load domain configuration based on the existing DomainId
        context.EntityLabels = _domainConfig.GetEntityLabels(context.DomainId);
        context.CustomFields = _domainConfig.GetCustomFields(context.DomainId).ToList();

        return context;
    }
}
