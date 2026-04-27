 This is the project description for my uni course 'Integration Project'. I built Ticket Masala for the course "IT Project". I've gotten an ok to keep building Ticket Masala instead of the assigned project below, help me give me a backlog of stuff below i need to integrate into ticket masala to match the requirements


---

Opdrachtomschrijving


Concept integratieproject

We werken een grootschalig project uit waarbij elk systeem

verbonden is met elk ander systeem. De integratie van de data doorheen

het hele IT-landschap staat centraal. Het analyseren van de behoeften

van de klant en deze omzetten naar een werkbaar en werkend prototype is

het hoofddoel van dit opleidingsonderdeel.

De opdracht zal worden uitgewerkt door twee verschillende

groepen. Per groep zijn twee Project Managers die alles sturen. De rest

van de studenten wordt onderverdeeld in teams die elk verantwoordelijk

zijn voor een bepaald stukje software en de bijhorende integratie ervan.

Elk team heeft 1 teamlead en 2 developers/testers. De teamleads

communiceren onder elkaar en met de Project Manager om de integratie van

hun softwarepakketten tot een goed einde te brengen.

Integratie van software betekend dat als software X

bepaaldedata binnenkrijgt die data automatisch gekend is in software Y

waardoor er geen downtime is en elk aspect van het project op elkaar

inspeelt. Voor basiszaken zoals een front-end website,

facturatiesysteem, etc. gebruik je dus bestaande softwarepakketten die

na het project met elkaar moeten kunnen “praten”.

Naast bestaande software, gebruiken we tools en concepten die in het werkveld dagelijks gebruikt worden.

Er zijn twee formatieve demo-momenten: de week voor de

paasvakantie en de laatste sessie. Telkens presenteren beide teams hun

vorderingen aan de klant. Doorheen het project zijn er ook regelmatig

check-in momenten waarbij de vooruitgang wordt opgevolgd, zowel

technisch als functioneel. Tijdens de laatste weken worden er last

minute nog wijzigingen of toevoegingen aangebracht door de klant om de

flexibiliteit te testen van het opgebouwde geheel.


Beschrijving project

De Desideriushogeschool wenst een platform om hun events te beheren. Een voorbeeld hiervan is Shiftfestival

Links to an external site. van Multimedia en Creatieve Technologie. Dit event is een netwerking-aangelegenheid

met prijsuitreiking voor de beste eindwerken. Voorafgaand zijn er

verschillende workshops en sprekers (van bedrijven). Achteraf is er een receptie waar er drank en eten voorzien wordt. Alle gegevens van de deelnemers en bedrijven die aanwezig zijn worden bijgehouden om later verdere zakelijke relaties op te zetten.


Functionele vereisten

Een persoon of bedrijf moet zich kunnen inschrijven voor een

sessie op de website van de klant, waar alle sessies beschikbaar zijn.

Hou hierbij rekening met:Validatie van gebruikersgegevens

Sturen van bevestiging na inschrijving

Ingeschreven deelnemers moeten de mogelijkheid hebben om hun inschrijving aan de kassa van het event te betalen.

Het moet mogelijk zijn om consumpties te bestellen en te betalen aan de bar.

Het moet mogelijk zijn om facturen op te stellen voor elk bedrijf.

Personen die niet gelinkt zijn aan een bedrijf, moeten hun consumpties ter plaatse betalen (geen factuur).

We willen een planning kunnen maken van de verschillende

onderdelen van het event. Bijvoorbeeld de planning van de verschillende

sessies.

We willen alle gegevens van de ingeschreven personen kunnen beheren in ons CRM-systeem.

We willen een overzicht van alle systemen (dashboard) en of ze al dan niet online/offline zijn.

Het moet mogelijk zijn om mailinglijsten uit te sturen naar

alle gebruikers of de ingeschreven gebruikers van één bepaalde sessie.

Een mooie extra is dat elke ingeschreven persoon een fysieke

badge of QR-code krijgt aan de inkom waarmee ze alle consumpties kunnen

bestellen. Op die manier kunnen we gemakkelijk later de factuur

doormailen.

Aandachtspunten

Alle systemen/microservices zijn loosely coupled. Als een

systeem down gaat (facturatie, CRM...), dient de rest te blijven werken.

Admins dienen meteen verwittigd te worden zodra een systeem down gaat.

Denk aan uitzonderingssituaties:Wat als een spreker een half uur vertraging heeft en daardoor andere sessies verzet moeten worden?

Wat als een privépersoon toch een factuur wil?

Wat met conflicterende gegevens uit verschillende systemen?

Probeer zelf “sad paths” te bedenken of dingen die kunnen fout

lopen. Op geen punt mag er een “computer says no”-scenario voorkomen.

Fases

Fase 1 — Analyse en theoretische opbouw

Analyse van de softwarepakketten en in kaart brengen welke data

nodig is. Uitdenken flows. Basisstructuur van de messages en

heartbeats. Gekoppeld aan technische startup (servers, accounts,

processen, ...).

Aan de hand van sessies met externen worden de verschillende

aspecten behandeld die aan bod komen bij een project van deze grootte en

wat de best practices zijn in het werkveld. We behandelen wat flows en user stories betekenen als concept. De tools voor groepswerk en projectaanpak worden toegelicht en voorzien.


Fase 2 — Technische implementatie

Tijdens fase 1 gaan we bepaalde technische aspecten vastleggen

die we hier gaan uitwerken. Er is een overlap met Fase 1 voor de

technische startup. De hoofdbrok in fase 2 bestaat uit development en de

flows uitwerken die opgesteld zijn in fase 1.


Fase 3 — Demoweken

De laatste fase van het project. Hierin bereiden we een

professionele pitch voor naar de klant. Verder kijken we welke

functionaliteiten zijn opgeleverd en welke bijsturing er nodig is. Ook

kunnen er vragen van de klant komen om last minute nog zaken aan te

passen. Het team krijgt dan telkens 1 week de tijd om de nodige

wijzigingen aan de brengen.


Evaluatie

Punten

Zie Informatiepagina onder hoofding Evaluatie.


Waarschuwingen

Zie Informatiepagina onder hoofding Waarschuwingen.


Systeemopbouw

De verdeling van de systemen gebeurt nadat de teams gemaakt

zijn. Elk team krijgt een softwarepakket toegewezen dat ze dienen op te

zetten. Hieronder volgt een samenvatting van de belangrijkste zaken.

Meer uitleg over deze use cases met de technische uitwerking en

vereisten, worden gegeven tijdens het project.

Elk systeem/team dient de volgende zaken op te zetten:


Docker image/container met de software.

Code die berichten van en naar de queue stuurt. Dit noemen we de senders en receivers.Senders en receivers valideren berichten die binnenkomen en buitengaan. Indien foutief worden er errors gelogd.

Alle code dient zich te bevinden in een GIT-repo met de nodige branches: main, dev, prod en feature branches.

Elk systeem heeft een ‘Heartbeat’ van 1 seconde.“In computer science, a heartbeat is a periodic signal generated by hardware or software to indicate normal operation or to synchronize other parts of a computer system. [...] Usually a heartbeat is sent between machines at a regular interval in the order of seconds; a heartbeat message. If

the endpoint does not receive a heartbeat for a time—usually a few

heartbeat intervals—the machine that should have sent the heartbeat is

assumed to have failed” (Wikipedia contributors. (2025, August 26). Heartbeat (computing). In Wikipedia, The Free Encyclopedia. Retrieved Februari 2, 2026, from https://en.wikipedia.org/w/index.php?title=Heartbeat_(computing)&oldid=1307896016)

De control room heeft een dashboard met de status van elk systeem.

Elk systeem heeft een volledige geautomatiseerde pipeline voor

het deployen van nieuwe versies van de software. Dit gebeurt allemaal

op basis van het GIT-repo

Teams

Team Controlroom/monitoring: https://www.elastic.co/elastic-stack

Links to an external site.Controle van uptime en downtime systemen.

Dashboard met statistieken over inschrijvingen, sprekers … Nuttig om mee uit te pakken op sociale media.

Team Facturatie: https://fossbilling.org/

Links to an external site.

Team CRM: https://www.salesforce.com/

Links to an external site.Bijhouden van klantdata/bedrijfsrelaties https://developer.salesforce.com/free-trials

Links to an external site.

Links to an external site.

BELANGRIJK: maak een dummy emailadres aan om jouw account bij

Salesforce mee te creëren. Salesforce stuurt een confirmation link die

maar 1x geopend mag worden, maar EhB doet automatische check op malafide

URL's in jouw emails, waardoor de URL zogezegd al 1x werd geopend

vooraleer jij die te zien krijgt. Daardoor is de link steeds ongeldig

als je die zelf openklikt.

Team Planning: Office365 (Outlook)

Team Frontend: T.B.A.

Team Kassa: https://www.odoo.com/

Links to an external site. (enkel POS)

Team Infra: beheer VMs, containers, pipelines, security

Team Mailing: https://sendgrid.com/

Links to an external site.

Extra: AI: https://modelcontextprotocol.io/

Links to an external site.Agent die op basis van de data in het systeem vragen kan beantwoorden en inzage kan geven.

Overkoepelend voor alle teams.

IoT: Badge scanner met Raspberry Pi voor kassabetalingen en inkom (min 1 doosje voor elk)https://www.home-assistant.io/

Links to an external site.

Toegangscontrole + aantal mensen in lokaal (opkomst per spreker)

Messaging Architectuur

Naast alle aparte softwareproducten, gebruiken we een

queueingsysteem dat alle berichten van en naar de aparte softwaretools

dient te sturen. Hiervoor gebruiken we Rabbit MQ (https://www.rabbitmq.com/

Links to an external site.).

Voorbeeld: Als systeem X moet communiceren met systeem Y,

plaatst X een bericht met een vooraf bepaalde structuur op een queue

binnen Rabbit MQ. Systeem Y luistert naar deze queue, haalt het bericht

binnen en gebruikt de data in het bericht om een actie uit te voeren in

eigen software.


Rollen

Er zijn in dit integratieproject 3 rollen beschikbaar.


Project Manager

Leidinggevende over alle teams.

Stellen planning samen voor alle teams

Lossen geschillen op tussen teams en hakken knopen door.

Zorgen voor efficiënte communicatie en kennisdeling. Voorbeeld: Het wijzigen van het formaat van berichten op de

queue heeft impact op elk systeem. Hoe voorzie je versiebeheer zodat elk

team op de hoogte is.

Werken de technische analyse uit in documenten zodat dit duidelijk is voor alle teams.

Hebben rechtstreeks contact met de klant en zoeken

verduidelijking over de opdracht. Communiceren voornamelijk met de team

leads.

Zijn verantwoordelijk voor de correcte uitrol van de demo en bereiden deze tot in de puntjes voor.

Team lead/integration designer

Verantwoordelijk voor de integratie met andere systemen.

Analyseert en werkt elk scenario technisch uit voor hun softwarepakket.

Verdeelt taken onder teamleden.

Helpt technisch met opzet van systemen en DevOps-taken.

Communiceert met de project manager voor teamoverschreidende zaken zoals flows en kennisdeling.

Zorgt dat eigen teamleden alle kennis bezitten om hun functie uit te voeren.

Is verantwoordelijk voor de oplevering van de integratie voor hun softwarepakket.

Developer/Tester

Ontwikkelt de Receivers en Senders.

Spreken API aan van softwarepakket om updates door te voeren.

Zetten de volledige pipeline op voor CI/CD en onderhouden deze.

Schrijven automatische testen die bij elke release uitgevoerd worden.

Communiceren met de team lead om overzicht van taken te krijgen.

Is verantwoordelijk voor kwalitatieve code met correcte foutafhandeling bij problemen.


