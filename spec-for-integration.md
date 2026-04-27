Architectuur Blueprint: The "Masala Bridge" Pattern

In plaats van Masala uit elkaar te trekken, bouwen we adapters aan de randen.

[Externe Teams] <--> [RabbitMQ] <--> [Masala RabbitMQ Bridge] <--> [System.Threading.Channels] <--> [GERDA Engine] <--> [SQLite (WAL)]
Epic 1: The Connectivity Layer (RabbitMQ & Heartbeats)

Doel: Masala verbinden met de buitenwereld zonder de core te vervuilen.

    Task 1.1: RabbitMQ Ingestion Service (Background Service)

        Eis: Luisteren naar queues van Drupal (Inschrijvingen) en Odoo (Kassa).

        Tech: Implementeer IHostedService. Gebruik RabbitMQ.Client.

        Pattern: "Fire and Forget" naar onze interne EnrichmentQueue.

        Critical: Implementeer een XmlToWorkItemConverter. De slide zegt "Transformatie naar XML = vóór de queue", dus we ontvangen waarschijnlijk XML. Masala werkt intern met JSON.

    Task 1.2: RabbitMQ Dispatcher (Outbound)

        Eis: Berichten sturen naar Facturatie (FOSSBilling) en Mailing (SendGrid).

        Tech: Maak een IMessageBus interface die we kunnen injecteren in onze TicketWorkflowController.

    Task 1.3: The Heartbeat Provider

        Eis: Elke seconde een signaal: "I am alive".

        Tech: Een simpele BackgroundService met een PeriodicTimer(TimeSpan.FromSeconds(1)). Payload: {"system": "TicketMasala", "status": "Healthy", "cpu": ...}. Stuur dit naar de "Monitoring" queue.

Epic 2: Domain Configuration (YAML Driven)

Doel: De business logica van Shiftfestival definiëren zonder C# hercompilatie.

    Task 2.1: Tenant Configuratie (shiftfestival)

        Maak een nieuwe map: config/tenants/shiftfestival.

        Configureer masala_domains.yaml met specifieke entiteiten.

    Task 2.2: Data Model Uitbreiding (Custom Fields)

        Definieer de volgende velden in YAML (die via GenerateColumns in SQLite komen):

            TicketType: (Session, Consumable, AccessBadge).

            IsCorporate: (Boolean - voor facturatie logica).

            BadgeID: (String - voor IoT koppeling).

            DietaryReq: (String - voor catering).

Epic 3: Workflow Automatisering (GERDA & Observers)

Doel: Automatische acties op basis van triggers ("Sad Paths" & Business Rules).

    Task 3.1: De "Facturatie" Observer

        Logica: Als een WorkItem (Bestelling) de status Confirmed krijgt EN IsCorporate == true.

        Actie: De GerdaService triggert een FinancialObserver. Deze stuurt een XML-bericht naar de RabbitMQ queue van Team Facturatie.

        Sad Path: Als RabbitMQ down is, markeer het ticket met de tag SystemRetry en zet het in de DispatchBacklog.

    Task 3.2: De "Spreker Vertraging" Workflow (Anticipation)

        Scenario: Team Planning stuurt bericht "Spreker X +30min".

        Actie: Masala ontvangt bericht -> Update de WorkContainer (Sessie Project) -> NotificationObserver stuurt bericht naar Team Mailing om deelnemers te mailen.

    Task 3.3: Bar/Catering Orders (High Performance)

        Scenario: Badge scan aan de bar.

        Eis: Snelle verwerking.

        Tech: Gebruik SlimSemaphore op de SQLite connectie. Zorg dat de tabel Consumables in SQLite WAL Mode (Write-Ahead Logging) geforceerd aan heeft staan.

Epic 4: AI & IoT Integration (The "Extra's")

Doel: Punten scoren met geavanceerde integraties.

    Task 4.1: MCP (Model Context Protocol) Endpoint

        Eis: Het "AI Team" moet data kunnen opvragen.

        Implementatie: Bouw een API endpoint /mcp/query dat SQLite FTS5 (Full Text Search) aanspreekt. Hierdoor kan de AI agent vragen beantwoorden als "Hoeveel mensen eten vegetarisch tijdens de lunch?" zonder ruwe SQL toegang.

    Task 4.2: IoT Badge Ingest Endpoint

        Eis: Raspberry Pi scant QR code.

        Implementatie: Een ultra-lightweight endpoint POST /api/iot/scan. Payload: { "badgeId": "xyz", "location": "Bar", "timestamp": "..." }. Dit wordt direct omgezet in een WorkItem en de queue in geschoten.

Technical Constraints Checklist (Architect Audit)

Voordat je begint, verifieer deze settings in appsettings.json en je Program.cs:

    SQLite Mode: Zorg dat PRAGMA journal_mode=WAL; wordt uitgevoerd bij startup. Dit is cruciaal voor de gelijktijdige badge-scans en RabbitMQ writes.

    No Reflection in Hot Paths: Validatie van binnenkomende RabbitMQ berichten (JSON/XML schema) moet gebeuren via gecompileerde Expression Trees bij startup, niet via runtime reflectie. Gebruik onze RuleCompilerService.

    Idempotency Keys: Elk inkomend RabbitMQ bericht heeft een ID. Sla dit op. Als Team Facturatie per ongeluk 2x hetzelfde stuurt, mag Masala niet 2x een ticket aanmaken.

Volgende stap:
Wil je dat ik de C# Interface voor de RabbitMqBridge uitschrijf, of wil je eerst de YAML configuratie voor het shiftfestival domein zien?