# Gatekeeper API

External ingestion API for Ticket Masala - provides a scalable ingestion layer for third-party integrations.

## Purpose

The GatekeeperApi is the **external ingestion gateway** for Ticket Masala. It allows external systems (webhooks, external applications, integrations) to submit tickets without directly accessing the main Web application.

## Architecture

```
External Systems → GatekeeperApi → Channel Queue → TicketMasala.Web
                   (Minimal API)   (async)         (Processing)
```

### Key Features

- **Minimal API** - Lightweight, high-performance endpoint
- **Async Processing** - Uses `System.Threading.Channels` for producer/consumer pattern
- **Decoupled** - External traffic doesn't hit main application directly
- **Transformation** - Supports Scriban templates for payload mapping

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/ingest` | Enqueue data for background processing |

## Usage

### Running Standalone

```bash
dotnet run --project src/GatekeeperApi
```

### Example Request

```bash
curl -X POST http://localhost:5000/api/ingest \
  -H "Content-Type: application/json" \
  -d '{"source": "github", "title": "Bug report", "body": "..."}'
```

## Relationship to Main Application

GatekeeperApi is designed to scale independently from the main Ticket Masala Web application:

- **Horizontal scaling**: Deploy multiple GatekeeperApi instances behind a load balancer
- **Rate limiting**: Apply external rate limits without affecting internal users
- **Security boundary**: Isolate external traffic from authenticated sessions

## Configuration

Configure the main application connection in `appsettings.json`:

```json
{
  "MasalaConnection": {
    "BaseUrl": "http://localhost:5080",
    "ApiKey": "your-api-key"
  }
}
```

## Future Enhancements

- [ ] Add authentication (API keys, OAuth)
- [ ] Implement webhook signature verification
- [ ] Add retry logic for failed deliveries
- [ ] Support batch ingestion
