# Ticket Masala API Referentie

Volledige REST API documentatie voor Ticket Masala.

## Basis URL

```
Ontwikkeling: http://localhost:5054/api/v1
Productie:   https://uw-domein.fly.dev/api/v1
```

## Authenticatie

De meeste endpoints vereisen authenticatie via ASP.NET Identity-cookies of Bearer-tokens.

```bash
# Op cookies gebaseerd (browser)
POST /Identity/Account/Login

# Voor API-clients: gebruik sessiecookies of implementeer op tokens gebaseerde authenticatie
```

**Rollen:**
- `Customer` (Klant) - Kan eigen tickets aanmaken en bekijken
- `Employee` (Medewerker) - Kan tickets en projecten beheren
- `Admin` - Volledige systeemtoegang

---

## API Respons-formaat

Alle API-antwoorden volgen een standaard wrapper-formaat:

```json
{
  "success": true,
  "data": { ... },
  "message": "Operatie succesvol voltooid",
  "errors": [],
  "timestamp": "2025-12-21T17:00:00Z"
}
```

**Foutrespons:**
```json
{
  "success": false,
  "data": null,
  "message": null,
  "errors": ["Beschrijving van de fout"],
  "timestamp": "2025-12-21T17:00:00Z"
}
```

---

## Werkitems (Tickets)

Het Universal Entity Model (UEM) gebruikt "WorkItem" voor tickets. Zowel de routes `/tickets` als `/work-items` worden ondersteund.

### Lijst van alle werkitems

```http
GET /api/v1/work-items
Authorization: Vereist
```

**Respons:** `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Netwerkverbinding probleem",
    "description": "Kan geen verbinding maken met VPN",
    "domainId": "IT",
    "typeCode": "INCIDENT",
    "status": "Nieuw",
    "priorityScore": 85.5,
    "estimatedEffortPoints": 5,
    "customerId": "user-123",
    "customerName": "Alice Klant",
    "assignedHandlerId": "agent-456",
    "assignedHandlerName": "Bob Support",
    "containerId": "project-guid",
    "containerName": "Q4 Support",
    "createdAt": "2025-12-20T10:00:00Z",
    "completionTarget": "2025-12-27T10:00:00Z"
  }
]
```

---

### Werkitem ophalen via ID

```http
GET /api/v1/work-items/{id}
Authorization: Vereist
```

**Parameters:**
| Naam | Type | Beschrijving |
|------|------|--------------|
| `id` | GUID | Identificatie van het werkitem |

**Respons:** `200 OK` - WorkItem object  
**Respons:** `404 Not Found` - Als het werkitem niet bestaat

---

### Werkitem aanmaken

```http
POST /api/v1/work-items
Authorization: Vereist
Content-Type: application/json
```

**Verzoek (Body):**
```json
{
  "title": "Nieuwe printer nodig",
  "description": "Verzoek voor een nieuwe printer in kantoor 3A",
  "domainId": "IT",
  "typeCode": "SERVICE_REQUEST",
  "customerId": "user-123",
  "completionTarget": "2025-12-30T17:00:00Z"
}
```

**Respons:** `201 Created`
```json
{
  "id": "nieuw-guid",
  "title": "Nieuwe printer nodig",
  ...
}
```

---

### Werkitem bijwerken

```http
PUT /api/v1/work-items/{id}
Authorization: Vereist
Content-Type: application/json
```

**Verzoek (Body):** Volledig WorkItem object met overeenkomend `id`

**Respons:** `204 No Content`  
**Respons:** `404 Not Found`

---

### Werkitem verwijderen

```http
DELETE /api/v1/work-items/{id}
Authorization: Vereist
```

**Respons:** `204 No Content`  
**Respons:** `404 Not Found`

---

### Externe Ticket-indiening

Tickets indienen vanaf externe websites zonder authenticatie.

```http
POST /api/v1/tickets/external
Content-Type: application/json
```

**Verzoek (Body):**
```json
{
  "customerEmail": "klant@voorbeeld.nl",
  "customerName": "Jan Jansen",
  "subject": "Hulp bij bestelling #12345",
  "description": "Ik heb hulp nodig bij mijn recente bestelling.",
  "sourceSite": "partner-bedrijf.nl"
}
```

**Respons:** `200 OK`
```json
{
  "success": true,
  "ticketId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "referenceNumber": "3FA85F64",
  "message": "Uw verzoek is succesvol ingediend"
}
```

> [!NOTE]
> Dit endpoint maakt een nieuw klantaccount aan als het e-mailadres nog niet bestaat.

---

## Projecten (Werkcontainers)

### Lijst van alle projecten

```http
GET /api/v1/projects
Authorization: Vereist (Medewerker, Admin)
```

**Respons:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "guid": "project-guid",
      "name": "Website Herontwerp",
      "description": "Volledig herontwerp van de bedrijfswebsite",
      "status": "InProgress",
      "customerName": "Client Corp",
      "projectManagerName": "Mike PM",
      "totalTickets": 15,
      "completedTickets": 8
    }
  ],
  "message": "5 projecten opgehaald"
}
```

---

### Project ophalen via ID

```http
GET /api/v1/projects/{id}
Authorization: Vereist
```

**Respons:** `200 OK` - Volledige projectdetails met tickets  
**Respons:** `404 Not Found`

---

### Projecten per klant ophalen

```http
GET /api/v1/projects/customer/{customerId}
Authorization: Vereist
```

---

### Projecten zoeken

```http
GET /api/v1/projects/search?query=website
Authorization: Vereist
```

**Query Parameters:**
| Naam | Type | Beschrijving |
|------|------|--------------|
| `query` | string | Zoekterm voor projectnaam |

---

### Project aanmaken

```http
POST /api/v1/projects
Authorization: Vereist (Medewerker, Admin)
Content-Type: application/json
```

**Verzoek (Body):**
```json
{
  "name": "Nieuwe Marketingcampagne",
  "description": "Q1 2026 marketing initiatief",
  "customerId": "customer-guid",
  "completionTargetMonths": 3
}
```

**Respons:** `201 Created`
```json
{
  "success": true,
  "data": "nieuw-project-guid",
  "message": "Project succesvol aangemaakt"
}
```

---

### Projectstatus bijwerken

```http
PATCH /api/v1/projects/{id}/status
Authorization: Vereist
Content-Type: application/json
```

**Verzoek (Body):**
```json
"InProgress"
```

**Geldige Statuswaarden:** `New`, `InProgress`, `OnHold`, `Completed`, `Cancelled`

---

### Projectmanager toewijzen

```http
PATCH /api/v1/projects/{id}/assign-manager
Authorization: Vereist
Content-Type: application/json
```

**Verzoek (Body):**
```json
"manager-user-id"
```

---

### AI Roadmap genereren

```http
POST /api/v1/projects/generate-roadmap
Authorization: Vereist
Content-Type: application/json
```

**Verzoek (Body):**
```json
"Bouw een klantenportaal met login, dashboard en rapportagefuncties"
```

**Respons:** `200 OK`
```json
{
  "success": true,
  "data": "1. Ontwerp authenticatieflow\n2. Maak dashboard wireframes\n3. ...",
  "message": "Roadmap succesvol gegenereerd"
}
```

---

### Project verwijderen

```http
DELETE /api/v1/projects/{id}
Authorization: Vereist (Alleen Admin)
```

**Respons:** `200 OK`

---

## Projectstatistieken

```http
GET /api/v1/projects/statistics/{customerId}
Authorization: Vereist
```

**Respons:** `200 OK`
```json
{
  "success": true,
  "data": {
    "totalProjects": 10,
    "activeProjects": 4,
    "completedProjects": 5,
    "pendingProjects": 1,
    "totalTasks": 150,
    "completedTasks": 95
  }
}
```

---

## Werkcontainers (V1)

Alternatief UEM-terminologie endpoint voor projecten.

```http
GET /api/v1/work-containers
POST /api/v1/work-containers
GET /api/v1/work-containers/{id}
PUT /api/v1/work-containers/{id}
DELETE /api/v1/work-containers/{id}
```

Zelfde functionaliteit als de `/projects` endpoints.

---

## Foutcodes

| HTTP Code | Beschrijving |
|-----------|--------------|
| `200` | Succes |
| `201` | Aangemaakt |
| `204` | Geen inhoud (succesvolle update/verwijdering) |
| `400` | Bad Request - Ongeldige invoer |
| `401` | Unauthorized - Authenticatie vereist |
| `403` | Forbidden - Onvoldoende rechten |
| `404` | Not Found - Bron bestaat niet |
| `500` | Interne serverfout |

---

## Rate Limiting

Momenteel wordt er geen rate limiting afgedwongen. Voor productie-implementaties kunt u overwegen dit te implementeren via:
- Reverse proxy (nginx, Caddy)
- ASP.NET Core Rate Limiting middleware

---

## Swagger/OpenAPI

Interactieve API-documentatie is beschikbaar op:

```
http://localhost:5054/swagger
```

De Swagger UI biedt:
- Verkenning van endpoints
- Request/respons schema's
- "Try-it-out" functionaliteit
- Ondersteuning voor authenticatie

---

## Verdere Informatie

- [Configuratiegids](../guides/CONFIGURATION.md) - API- en domeinconfiguratie
- [Architectuuroverzicht](../architecture/SUMMARY.md) - Systeemontwerp
- [Ontwikkelingsgids](../guides/DEVELOPMENT.md) - Lokale ontwikkelingsinstellingen
