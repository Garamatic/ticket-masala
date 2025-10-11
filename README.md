# IT-Project-25/26 - Ticket Masala

![Logo](docs/visual/logo-green.png)

## Informatie

- Wie : Charlotte Schröer, Maarten Görtz, Wito De Schrijver, Stef Van Impe en Juan Benjumea
- Concept : Ticketing, Case en Project Management met AI ondersteuning
- Technologieen: Fullstack .NET en Python

## Basis structuur

Light-weight beheersysteem met AI-integratie als rode draad in alle lagen.

Dashboard-gedreven ontwerp waarbij de gebruiker (klant, stafflid) per rol de relevante informatie ziet. Er zijn drie views: ticketing, case en project management.

![ERD-model](docs/architecture/erd-dark.drawio.png)

### Lagen

- Ticketing : toegangspoort, initiatie van cases, projecten en tickets

-	Case Management : een case bestaat uit een of meerdere tickets (parent, child), men kan hier de opvolging van tickets doen, de interactie in het dossier bijhouden en aan detailbeheer doen

-	Project Management : een project bestaat uit een of meerdere cases (indien meerdere klanten, meerdere cases die geen hierarchische verhouding hebben, hier volgt men het overzicht van de projecten, de deadlines en de mijlpalen

- AI helper : analyse, contextuele hulp en draft functionaliteit bij alle drie lagen

![Basis UI](docs/visual/basic-UI.png)

### Interconnectie van lagen

Ticketing → Case Management: elke ticket wordt automatisch een case met AI-aanbevelingen voor opvolging

Case Management → Project Management: cases worden geaggregeerd tot projectoverzicht

AI Helper → Alle lagen: real-time suggesties bij aanmaak, opvolging, planning en rapportage

## Roadmap

Algemeen
- [ ] Role based authentication
- [ ] Notificaties en berichten
- [ ] Discussies en comments

Ticketing interface
- [ ] Ticket aanmaakfunctie : form bij klant, medewerker, automatisch.
- [ ] Aanpassing ticket : individueel en batch
- [ ] Filter- en zoekfunctie
- [ ] Quick actions

Case management interface
- [ ] Case Detail view
- [ ] Linken, groeperen van tickets
- [ ] Notities
- [ ] Berichten
- [ ] Documentatie en bijlagen bij case
- [ ] Form / document generation
    
Project management interface  
- [ ] Fases en mijlpalen
- [ ] Teamleden en verantwoordelijkheden
- [ ] Kalender
- [ ] Analytics

AI Helper 
- [ ] Similariteitszoektocht en contextuele vergelijking eerdere cases
- [ ] AI suggestie automatische toewijzing
- [ ] Explain case en context generatie
- [ ] Generatie oplossingen, antwoorden, rapporten
- [ ] Alerts en insights
- [ ] Planning optimalisatie suggesties

## Functionele en technische vereisten

- [ ] Mobile first en responsive design
- [ ] Scalable en snel beheersysteem
- [ ] Frontend in Blazor (.NET compileren naar Javascript/HTML/CSS)
- [ ] Backend in .NET (framework?)
- [ ] AI-integratie met API (goedkoop of lokaal draaien van Ollama)
- [ ] Infra: Azure oplossing + docker containers?

