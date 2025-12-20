# Gatekeeper API

This is a Minimal API for scalable ingestion (Phase 8).

- POST `/api/ingest` to enqueue data for background processing.
- Uses `System.Threading.Channels` for async producer/consumer.
- Add mapping logic (e.g., Scriban) in the worker for transformation.

To run:

```sh
dotnet run --project src/GatekeeperApi
```
