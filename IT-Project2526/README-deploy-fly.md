# Deploy IT-Project2526 to Fly.io

## 1. Prerequisites

- Install Fly CLI: [Installation Guide](https://fly.io/docs/flyctl/install/)
- Login: `fly auth login`
- Ensure Docker is available locally (Fly can also build remotely).

## 2. App Name & Region

Edit `fly.toml` if you need a different app name than `ticket-masala`. Choose a primary region near your users (e.g. `ord`, `iad`, `fra`).

## 3. Secrets (Connection String)

The production SQL Server connection string was removed from `appsettings.json`.
Set it as a Fly secret so EF Core can access it:

```fish
fly secrets set ConnectionStrings__DefaultConnection="Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=TicketMasalaDB;Persist Security Info=False;User ID=USERNAME;Password=STRONG_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

Server=tcp:itproject2526.database.windows.net,1433;Initial Catalog=TicketMasalaDB;Persist Security Info=False;User ID=itprojectadmin;Password=Eendompasswoord1!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;

ASP.NET configuration binding will read this environment variable instead of the placeholder.

## 4. First Launch (if not already created)

If you did not manually create `fly.toml` use `fly launch` in the project folder. Since we already have one:

```fish
cd IT-Project2526
fly deploy
```

## 5. Health Check

`Program.cs` maps `GET /health`. The Fly config defines an HTTP check hitting `/health` every 15s. This helps Fly decide machine health.

## 6. Subsequent Deploys

After code changes:

```fish
fly deploy
```

View status & logs:

```fish
fly status
fly logs
```

Open the app in a browser:

```fish
fly apps open
```

## 7. Scaling / Machines

Keep at least one machine running (`min_machines_running = 1`). Adjust concurrency or add regions using:

```fish
fly scale count 2
fly regions add fra
```

## 8. Common Maintenance

- Rotate secrets: re-run `fly secrets set ...`
- Release rollback: `fly releases list` then `fly releases revert <version>`

## 9. Troubleshooting

| Symptom | Action |
|---------|--------|
| 502 Bad Gateway | Check logs (`fly logs`); verify port 8080 exposed & `ASPNETCORE_URLS`. |
| DB connection failures | Ensure secret set; test via `fly ssh console`; `printenv \| grep ConnectionStrings`. |
| Health check failing | Hit `https://<app>.fly.dev/health`; confirm `Program.cs` has `MapHealthChecks`. |
| Out of memory | Increase machine size: `fly machine update <id> --memory 1024`. |

## 10. External SQL Server Notes

Fly does not offer managed SQL Server; using Azure SQL is fine. Latency depends on region pairing—ideally deploy in a Fly region geographically close to the Azure SQL region.

## 11. Local Test of Image

(Optional) build locally before deploy:

```fish
docker build -t ticket-masala:test .
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development ticket-masala:test
```

Then browse `http://localhost:8080`.

---
Deployment files:

- `Dockerfile`
- `.dockerignore`
- `fly.toml`

Secrets are never committed—validate by listing releases or using `fly secrets list`.
