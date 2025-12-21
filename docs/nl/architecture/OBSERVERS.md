# Observer-patroon

Documentatie voor de implementatie van het event-gestuurde Observer-patroon in Ticket Masala.

## Overzicht

Het Observer-patroon maakt een **losse koppeling** (loose coupling) mogelijk tussen de gebeurtenissen in de levenscyclus van een ticket en de verschillende reacties van het systeem. Wanneer een ticket wordt aangemaakt, toegewezen of voltooid, kunnen meerdere observers onafhankelijk reageren zonder dat de kernservice van hun bestaan afweet.

```
TicketService.CreateAsync()
        ↓
    Meld aan alle Observers
        ├── GerdaTicketObserver    → AI-verwerking in wachtrij plaatsen
        ├── NotificationObserver   → Gebruikersmeldingen versturen
        └── LoggingObserver        → Audit logging
```

---

## Observer-interfaces

### ITicketObserver

Primaire interface voor gebeurtenissen in de levenscyclus van een ticket.

```csharp
public interface ITicketObserver
{
    Task OnTicketCreatedAsync(Ticket ticket);
    Task OnTicketAssignedAsync(Ticket ticket, Employee assignee);
    Task OnTicketCompletedAsync(Ticket ticket);
    Task OnTicketUpdatedAsync(Ticket ticket);
    Task OnTicketCommentedAsync(TicketComment comment);
}
```

### IProjectObserver

Interface voor gebeurtenissen in de levenscyclus van een project.

```csharp
public interface IProjectObserver
{
    Task OnProjectCreatedAsync(Project project);
    Task OnProjectUpdatedAsync(Project project);
    Task OnProjectCompletedAsync(Project project);
    Task OnTicketAddedToProjectAsync(Project project, Ticket ticket);
}
```

---

## Ingebouwde Observers

### GerdaTicketObserver

Activeert GERDA AI-verwerking voor nieuwe tickets.

**Locatie:** `Observers/GerdaTicketObserver.cs`

```csharp
public class GerdaTicketObserver : ITicketObserver
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        // Toevoegen aan wachtrij voor AI-verwerking op de achtergrond
        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var gerda = scope.ServiceProvider.GetRequiredService<IGerdaService>();
            await gerda.ProcessTicketAsync(ticket.Guid);
        });
    }
}
```

**Belangrijkste kenmerken:**
- Gebruikt een achtergrondwachtrij voor asynchrone verwerking.
- Creëert een geïsoleerde DI-scope voor de GERDA-service.
- Blokkeert de thread van het hoofdverzoek niet.

---

### NotificationTicketObserver

Verstuurt meldingen naar gebruikers wanneer tickets wijzigen.

**Locatie:** `Observers/NotificationTicketObserver.cs`

```csharp
public class NotificationTicketObserver : ITicketObserver
{
    public async Task OnTicketAssignedAsync(Ticket ticket, Employee assignee)
    {
        await _notificationService.NotifyAsync(
            userId: assignee.Id,
            message: $"Er is een ticket aan u toegewezen: {ticket.Title}",
            type: NotificationType.Assignment
        );
    }
}
```

---

### LoggingTicketObserver

Maakt auditlog-items aan voor ticketgebeurtenissen.

**Locatie:** `Observers/LoggingTicketObserver.cs`

---

## Registratie

Observers worden geregistreerd in `Extensions/ObserverExtensions.cs`:

```csharp
public static class ObserverExtensions
{
    public static IServiceCollection AddObservers(this IServiceCollection services)
    {
        // Registreer alle ticket-observers
        services.AddScoped<ITicketObserver, GerdaTicketObserver>();
        services.AddScoped<ITicketObserver, NotificationTicketObserver>();
        services.AddScoped<ITicketObserver, LoggingTicketObserver>();

        // Registreer project-observers
        services.AddScoped<IProjectObserver, LoggingProjectObserver>();
        services.AddScoped<IProjectObserver, NotificationProjectObserver>();

        // Registreer reactie-observers
        services.AddScoped<ICommentObserver, CommentObservers>();

        return services;
    }
}
```

---

## Aanroepen van Observers

De `TicketService` roept de observers aan na succesvolle bewerkingen:

```csharp
public class TicketService : ITicketService
{
    private readonly IEnumerable<ITicketObserver> _observers;

    public async Task<Ticket> CreateTicketAsync(...)
    {
        // 1. Maak het ticket aan
        var ticket = await _repository.AddAsync(newTicket);

        // 2. Meld aan alle observers
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketCreatedAsync(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer mislukt");
                // Niet opnieuw gooien - observers mogen de hoofdstroom niet onderbreken
            }
        }

        return ticket;
    }
}
```

---

## Aangepaste Observers aanmaken

### Stap 1: Implementeer de interface

```csharp
public class SlackNotificationObserver : ITicketObserver
{
    private readonly ISlackClient _slack;

    public SlackNotificationObserver(ISlackClient slack)
    {
        _slack = slack;
    }

    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        await _slack.PostMessageAsync(
            channel: "#support-tickets",
            message: $"Nieuw ticket: {ticket.Title}"
        );
    }

    // Andere methoden implementeren...
}
```

### Stap 2: Registreer de Observer

```csharp
// In Extensions/ObserverExtensions.cs
services.AddScoped<ITicketObserver, SlackNotificationObserver>();
```

---

## Beste Praktijken (Best Practices)

1. **Gooi geen uitzonderingen** - Observers moeten fouten opvangen en loggen, niet de hoofdstroom onderbreken.
2. **Gebruik achtergrondverwerking** - Langlopende taken moeten in een wachtrij worden geplaatst.
3. **Houd observers gefocust** - Elke observer moet één verantwoordelijkheid hebben.
4. **Vermijd circulaire afhankelijkheden** - Observers mogen geen services aanroepen die hen activeren.
5. **Overweeg de volgorde** - Als de volgorde belangrijk is, implementeer dan expliciete prioriteit.

---

## Verdere Informatie

- [Architectuuroverzicht](SUMMARY.md)
- [GERDA AI-documentatie](gerda-ai/GERDA_MODULES.md)
- [Testen Gids](../guides/TESTING.md)
