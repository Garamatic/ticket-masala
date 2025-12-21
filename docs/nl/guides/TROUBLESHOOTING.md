# Probleemoplossing (Troubleshooting)

Veelvoorkomende problemen en oplossingen voor Ticket Masala.

---

## Databaseproblemen

### "Unable to open database file"
**Oplossing:** Controleer de bestandsrechten van `app.db` en zorg ervoor dat de map waarin het bestand staat schrijfbaar is voor de applicatie.

### "No such table"
**Oplossing:** Pas ontbrekende database-migraties toe met `dotnet ef database update`. Als u zich in een ontwikkelomgeving bevindt, kunt u ook de database verwijderen en de applicatie opnieuw starten om deze opnieuw te laten opbouwen.

### "Database is locked"
**Oplossing:** Sluit andere programma's die de database gebruiken, zoals SQLite-browsers of andere exemplaren van de applicatie.

---

## Build-problemen

### "Target framework 'net10.0' not found"
**Oplossing:** Zorg ervoor dat de .NET 10 SDK is geïnstalleerd. Controleer dit met `dotnet --list-sdks`.

### "Package restore failed"
**Oplossing:** Wis de lokale NuGet-cache met `dotnet nuget locals all --clear` en probeer de afhankelijkheden opnieuw te herstellen met `dotnet restore`.

---

## Configuratieproblemen

### "GERDA config not found" of "Domain configuration file not found"
**Oplossing:** Controleer of de bestanden `masala_config.json` en `masala_domains.yaml` aanwezig zijn in de map die is opgegeven via de omgevingsvariabele `MASALA_CONFIG_PATH`.

### "Strategy validation FAILED"
**Oplossing:** Controleer in de configuratie of de namen van de AI-strategieën correct zijn gespeld en overeenkomen met de beschikbare strategieën voor rangschikking, verzending en schatting.

---

## Runtime-problemen

### "Port already in use"
**Oplossing:** Beëindig het proces dat de poort gebruikt (bijvoorbeeld poort 5054) of start de applicatie op een andere poort met `dotnet run --urls "http://localhost:5055"`.

### "Hot reload niet werkend"
**Oplossing:** Gebruik het commando `dotnet watch run` om de applicatie te starten en wijzigingen in de broncode automatisch te detecteren.

---

## Authenticatieproblemen

### "Inloggen mislukt"
**Oplossing:** Controleer of u de juiste inloggegevens gebruikt (zie de standaardwachtwoorden in de [Ontwikkelingsgids](DEVELOPMENT.md)). Als het probleem aanhoudt, kunt u de database resetten om de initiële testaccounts te herstellen.

---

## GERDA AI-problemen

### "GERDA verwerkt geen tickets"
**Oplossing:** Controleer in `masala_config.json` of het GERDA AI-systeem is ingeschakeld (`IsEnabled: true`). Bekijk ook de applicatielogs op eventuele foutmeldingen tijdens de verwerking door de AI-modules.

---

## Verdere Informatie

- [Ontwikkelingsgids](DEVELOPMENT.md)
- [Configuratiegids](CONFIGURATION.md)
- [Testen Gids](TESTING.md)
