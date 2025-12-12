# Ticket Masala Demo Script

Demonstratiescenario's voor het Ticket Masala platform.

---

## 🎯 Demo Overzicht

| Scenario | Duur | Doelgroep |
|----------|------|-----------|
| 1. Klant Journey | 5 min | Eindgebruikers |
| 2. Agent Workflow | 5 min | Support medewerkers |
| 3. Manager Dashboard | 7 min | Team leads, managers |
| 4. Admin Configuratie | 5 min | Systeembeheerders |

> **Tip**: Start de app met seeded data voor realistische demo: `dotnet run --project src/TicketMasala.Web/`

---

## 📋 Scenario 1: Klant Journey

**Login**: `alice.customer@example.com` / `Customer123!`

### Stappen

1. **Dashboard bekijken**
   - Navigeer naar Home
   - Toon overzicht van eigen tickets

2. **Nieuw ticket aanmaken**
   - Klik op "Nieuw Ticket"
   - Vul in: Titel, Beschrijving, Type
   - Verstuur en toon bevestiging

3. **Ticket status volgen**
   - Ga naar ticket lijst
   - Klik op ticket om details te zien
   - Toon status updates en notificaties

---

## 📋 Scenario 2: Agent Workflow

**Login**: `david.support@ticketmasala.com` / `Employee123!`

### Stappen

1. **Mijn tickets bekijken**
   - Navigeer naar "Mijn Tickets"
   - Toon toegewezen taken

2. **Ticket afhandelen**
   - Open een ticket
   - Voeg opmerkingen toe
   - Wijzig status naar "In behandeling"
   - Markeer als "Voltooid"

3. **Projecten bekijken**
   - Navigeer naar Projecten
   - Toon project dashboard

---

## 📋 Scenario 3: Manager Dashboard (GERDA AI)

**Login**: `mike.pm@ticketmasala.com` / `Employee123!`

### Stappen

1. **Team Dashboard**
   - Navigeer naar "Team Dashboard"
   - Toon GERDA AI metrics:
     - Agent workload verdeling
     - Ticket throughput
     - SLA compliance

2. **GERDA Dispatch Backlog** ⭐
   - Navigeer naar "Dispatch Backlog"
   - Toon AI-aanbevelingen voor ticket toewijzing
   - Demonstreer:
     - Match scores per agent
     - Workload balancing
     - Auto-assign functionaliteit

3. **Capacity Forecast** ⭐
   - Navigeer naar "Capacity Forecast"
   - Toon 30-dagen voorspelling:
     - Verwachte ticket instroom
     - Team capaciteit
     - Risico alerts

---

## 📋 Scenario 4: Admin Configuratie

**Login**: `admin@ticketmasala.com` / `Admin123!`

### Stappen

1. **Gebruikersbeheer**
   - Navigeer naar Admin → Gebruikers
   - Toon rol en rechten beheer

2. **Klantenbeheer**
   - Navigeer naar Klanten
   - Toon klant historie en projecten

3. **Configuratie bestanden** (optioneel)
   - Toon `config/masala_config.json` - Feature flags
   - Toon `config/masala_domains.yaml` - GERDA strategieën

---

## 🔑 Belangrijke URLs

| Functie | URL |
|---------|-----|
| Applicatie | http://localhost:5054 |
| Swagger API | http://localhost:5054/swagger |

---

## 📝 Voorbereidingen

1. Start de applicatie: `dotnet run --project src/TicketMasala.Web/`
2. Zorg dat database is ge-seed (gebeurt automatisch bij eerste run)
3. Open browser op http://localhost:5054
