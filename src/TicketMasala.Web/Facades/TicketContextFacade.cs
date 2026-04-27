using System.Text.Json;
using TicketMasala.Domain.Configuration;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Facades;

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

    public async Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketId, string? userId, bool isCustomer)
    {
        return await _detailService.GetTicketDetailsAsync(ticketId, userId, isCustomer);
    }

    public async Task<TicketDetailContext> GetTicketDetailContextAsync(TicketDetailsViewModel viewModel)
    {
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
                context.CustomFieldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(viewModel.CustomFieldsJson)
                    ?? new Dictionary<string, object>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize CustomFieldsJson for ticket context");
                context.CustomFieldValues = new Dictionary<string, object>();
            }
        }

        return context;
    }

    public async Task<TicketCreateContext> GetCreateContextAsync(bool isCustomer, string? preselectedCustomerId = null, Guid? projectGuid = null)
    {
        var context = await _createService.GetCreateContextAsync(isCustomer, preselectedCustomerId, projectGuid);

        // Domain configuration is still handled here as it's configuration, not data
        var defaultDomain = _domainConfig.GetDefaultDomainId();
        context.DomainId = defaultDomain;
        context.EntityLabels = _domainConfig.GetEntityLabels(defaultDomain);
        context.WorkItemTypes = _domainConfig.GetWorkItemTypes(defaultDomain).ToList();
        context.CustomFields = _domainConfig.GetCustomFields(defaultDomain).ToList();

        return context;
    }

    public async Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user)
    {
        var context = await _editService.GetEditContextAsync(ticketId, user);

        if (context != null)
        {
            // Domain configuration is still handled here as it's configuration, not data
            // The ticket's domain ID should be available from the view model's WorkItemTypeCode
            var domainId = context.WorkItemTypeCode ?? _domainConfig.GetDefaultDomainId();
            context.DomainId = domainId;
            context.EntityLabels = _domainConfig.GetEntityLabels(domainId);
            context.CustomFields = _domainConfig.GetCustomFields(domainId).ToList();

            if (context.ViewModel != null && context.ViewModel.Guid != Guid.Empty)
            {
                try
                {
                    var customFieldsJson = context.CustomFieldValues != null
                        ? JsonSerializer.Serialize(context.CustomFieldValues)
                        : "{}";

                    if (!string.IsNullOrEmpty(customFieldsJson) && customFieldsJson != "{}")
                    {
                        context.CustomFieldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(customFieldsJson) ?? new Dictionary<string, object>();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize custom fields for edit context for ticket {TicketId}", context.ViewModel.Guid);
                }
            }
        }

        return context;
    }

    public async Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user)
    {
        var context = await _editService.GetEditReloadContextAsync(ticketId, user);

        var reloadDomainId = _domainConfig.GetDefaultDomainId();
        context.DomainId = reloadDomainId;
        context.EntityLabels = _domainConfig.GetEntityLabels(reloadDomainId);
        context.CustomFields = _domainConfig.GetCustomFields(reloadDomainId).ToList();

        return context;
    }
}
