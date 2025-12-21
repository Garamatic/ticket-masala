# Testen Gids

Volledige handleiding voor het testen in Ticket Masala.

## Snel aan de slag

```bash
# Voer alle tests uit
cd src/factory/TicketMasala
dotnet test

# Voer tests uit met gedetailleerde uitvoer
dotnet test --logger "console;verbosity=detailed"

# Voer een specifiek testproject uit
dotnet test src/TicketMasala.Tests/
```

---

## Structuur testproject

De tests zijn georganiseerd in verschillende mappen binnen `src/TicketMasala.Tests/`, waaronder:
- `Api/`: Tests voor API-endpoints.
- `Architecture/`: Tests voor architectuurregels (NetArchTest).
- `IntegrationTests/`: Integratietests met `WebApplicationFactory`.
- `Services/`: Unit-tests voor de bedrijfslogica.
- `UnitTests/`: Pure unit-tests.

---

## Testframeworks & Libraries

- **xUnit**: Het testframework.
- **FluentAssertions**: Voor leesbare asserts.
- **Moq**: Voor het mocken van afhankelijkheden.
- **Bogus**: Voor het genereren van nepgegevens.
- **NetArchTest**: Voor de validatie van architectuurregels.

---

## Unit-tests

Test individuele componenten in isolatie met behulp van mocks. Hiermee controleert u of de logica van een specifieke methode of service correct werkt zonder afhankelijk te zijn van andere onderdelen van het systeem.

---

## Integratietests

Test de samenwerking tussen verschillende componenten en de database met behulp van `WebApplicationFactory`. Deze tests gebruiken vaak een in-memory database om een realistische omgeving te simuleren zonder de echte database te vervuilen.

---

## Architectuurtests

Dwing architecturale beperkingen af met NetArchTest. U kunt bijvoorbeeld controleren of controllers niet rechtstreeks afhankelijk zijn van repositories, of dat alle services een bepaalde naamgeving volgend.

---

## Testgegevens genereren

Gebruik Bogus om realistische testgegevens te genereren voor gebruikers, tickets en andere entiteiten. Dit helpt om een breed scala aan scenario's te dekken zonder handmatig grote hoeveelheden gegevens te hoeven invoeren.

---

## Code coverage

U kunt code coverage-rapporten genereren om te zien welk deel van uw code door tests wordt gedekt. Gebruik hiervoor de optie `--collect:"XPlat Code Coverage"` bij `dotnet test`.

---

## Beste Praktijken (Best Practices)

1. **Naamgeving**: Gebruik een duidelijke structuur zoals `Methodenaam_Scenario_VerwachtGedrag`.
2. **Arrange-Act-Assert**: Zorg voor een duidelijke structuur binnen elke test.
3. **Focus**: Test één ding per keer.
4. **Onafhankelijkheid**: Tests mogen niet van elkaar afhankelijk zijn en moeten in willekeurige volgorde kunnen draaien.
5. **Edge cases**: Test ook randgevallen zoals null-waarden, lege invoer en grenswaarden.

---

## Verdere Informatie

- [xUnit Documentatie](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Ontwikkelingsgids](DEVELOPMENT.md)
- [Architectuuroverzicht](../architecture/SUMMARY.md)
