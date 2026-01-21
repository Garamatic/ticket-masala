using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Configuration;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

public interface IDispatchingStrategySelector
{
    string GetDefaultStrategyName();
    string GetStrategyNameForDomain(string domainId);
    string GetStrategyNameForTicket(Ticket ticket);
}

public sealed class DomainDispatchingStrategySelector : IDispatchingStrategySelector
{
    private const string DefaultDispatchingStrategyName = "MatrixFactorization";

    private readonly IDomainConfigurationService _domainConfigurationService;

    public DomainDispatchingStrategySelector(IDomainConfigurationService domainConfigurationService)
    {
        _domainConfigurationService = domainConfigurationService;
    }

    public string GetDefaultStrategyName()
    {
        var defaultDomainId = _domainConfigurationService.GetDefaultDomainId();
        return GetStrategyNameForDomain(defaultDomainId);
    }

    public string GetStrategyNameForDomain(string domainId)
    {
        var domainConfig = _domainConfigurationService.GetDomain(domainId);
        return domainConfig?.AiStrategies.Dispatching ?? DefaultDispatchingStrategyName;
    }

    public string GetStrategyNameForTicket(Ticket ticket)
    {
        var domainId = ticket.DomainId ?? _domainConfigurationService.GetDefaultDomainId();
        return GetStrategyNameForDomain(domainId);
    }
}
