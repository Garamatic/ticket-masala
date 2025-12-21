# Ontwikkelingsgids

Volledige handleiding voor de lokale ontwikkeling van Ticket Masala.

## Vereisten

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- **IDE** - Visual Studio 2022, VS Code (met C# Dev Kit) of JetBrains Rider.
- **Git** - Versiebeheer.

Optioneel:
- **Docker** - Voor gecontaineriseerde ontwikkeling.
- **SQLite Browser** - Om de database te inspecteren.

---

## Snel aan de slag

```bash
# Clone de repository
git clone https://github.com/your-org/ticket-masala.git
cd ticket-masala

# Ga naar het project
cd src/factory/TicketMasala

# Herstel afhankelijkheden
dotnet restore

# Build
dotnet build

# Start de applicatie (database wordt bij de eerste start aangemaakt)
dotnet run --project src/TicketMasala.Web/

# Open de browser op
# http://localhost:5054
```

---

## Projectstructuur

- `src/TicketMasala.Web/`: De hoofd-ASP.NET Core applicatie.
- `src/TicketMasala.Domain/`: De domeinentiteiten.
- `src/TicketMasala.Tests/`: Het testproject.
- `config/`: Standaard configuratiebestanden.
- `docs/`: Documentatie.

---

## Databasebeheer

### Eerste keer starten

De database wordt automatisch aangemaakt en gevuld met initiële gegevens (seed) wanneer u de applicatie voor het eerst start.

### Migraties

Gebruik de .NET Core CLI om nieuwe migraties toe te voegen of de database bij te werken:

```bash
# Nieuwe migratie toevoegen
dotnet ef migrations add NaamVanMigratie --project src/TicketMasala.Web --context MasalaDbContext

# Database bijwerken
dotnet ef database update --project src/TicketMasala.Web
```

---

## De applicatie draaien

- **Standaard**: `dotnet run --project src/TicketMasala.Web/`
- **Met Hot Reload**: `dotnet watch run --project src/TicketMasala.Web/`
- **Met Docker**: `docker-compose up --build`

---

## Testaccounts

| Rol | E-mail | Wachtwoord |
|------|-------|----------|
| Beheerder | admin@ticketmasala.com | Admin123! |
| Medewerker | mike.pm@ticketmasala.com | Employee123! |
| Klant | alice.customer@example.com | Customer123! |

---

## Foutopsporing (Debugging)

Logniveaus kunnen worden aangepast in `appsettings.Development.json`. Voorbeelden van logniveaus zijn `Information`, `Debug` of `Warning`.

---

## Veelvoorkomende ontwikkelingstaken

### Een nieuwe controller toevoegen
Voeg controllers toe in de map `Controllers/`. Gebruik de `[Authorize]` attribuut voor beveiligde routes.

### Een nieuwe service toevoegen
Maak een interface en een implementatie aan in de map `Engine/`. Registreer de service vervolgens in `Extensions/ServiceCollectionExtensions.cs`.

---

## Teambeheer en Tenants

Het systeem ondersteunt meerdere 'tenants'. U kunt een nieuwe tenant aanmaken door een map aan te maken in `tenants/` met de benodigde configuratie.

---

## Verdere Informatie

- [Testen Gids](TESTING.md)
- [Configuratiegids](CONFIGURATION.md)
- [Architectuuroverzicht](../architecture/SUMMARY.md)
- [API-naslagwerk](../api/API_REFERENCE.md)
