# Ticket Masala - API Usage Examples

While the **Swagger UI** (`/swagger`) provides interactive documentation, these examples demonstrate common workflows using `curl`.

## Authentication

Identify serves are used to get an access token.

**Login:**

```bash
curl -X POST "http://localhost:5054/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "gustave@desgoffe.gov",
    "password": "Admin123!"
  }'
```

## Ticket Management

### 1. List Tickets (Grid)

Retrieve a paginated list of tickets.

```bash
curl -X GET "http://localhost:5054/api/v1/ticket?page=1&pageSize=10" \
  -H "Authorization: Bearer <YOUR_TOKEN>"
```

### 2. Create a Ticket

Submit a new ticket. The system will automatically classify it via GERDA.

```bash
curl -X POST "http://localhost:5054/api/v1/ticket" \
  -H "Authorization: Bearer <YOUR_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Pothole on Main Street",
    "description": "There is a large pothole damaging cars.",
    "priority": "Medium"
  }'
```

## GERDA AI Operations

### 1. Get Dispatch Recommendations

Retrieve AI-suggested agents for a specific ticket.

```bash
curl -X GET "http://localhost:5054/api/v1/gerda/recommendations/{ticketId}" \
  -H "Authorization: Bearer <YOUR_TOKEN>"
```

### 2. Trigger Sentiment Analysis

Manually trigger sentiment analysis for a ticket description.

```bash
curl -X POST "http://localhost:5054/api/v1/gerda/analyze" \
  -H "Authorization: Bearer <YOUR_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "I am extremely frustrated with the service delay!"
  }'
```

---

_Note: Check `masala_domains.yaml` to valid domain configurations._
