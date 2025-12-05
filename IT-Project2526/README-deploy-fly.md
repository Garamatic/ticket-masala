# Deploy IT-Project2526 to Fly.io

## 1. Prerequisites

- Install Fly CLI: [Installation Guide](https://fly.io/docs/flyctl/install/)
- Login: `fly auth login`
- Ensure Docker is available locally (Fly can also build remotely).

## 2. App Name & Region

Edit `fly.toml` if you need a different app name than `ticket-masala`. Choose a primary region near your users (e.g. `ord`, `iad`, `fra`).

## 3. Data Persistence (SQLite)
The application is configured to use **SQLite** in production (see `Program.cs`).
A volume mapped to `/data` ensures the database is persisted across restarts.

No external SQL Server connection string is required.


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

## 10. Database Notes
Fly.io volumes provide fast, persistent storage for SQLite.
If you need to scale horizontally (multiple machines), you should migrate to Postgres or an external SQL Server, as SQLite does not support multiple concurrent writers across different machines easily.

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
