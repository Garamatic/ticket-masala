# TODO — Ticket Masala

Date: 2025-12-08

This file records the current task list and statuses from the debugging session.

- [x] Add Docker docs note — Completed
- [x] Ignore `out` and `deploy/linux-pilot` — Completed
- [x] Commit & push changes — Completed
- [x] Merge `feature/configuration-extensibility` → `dev` — Completed
- [x] Merge `dev` → `main` — Completed
- [-] Start published app and test CSS endpoints — In progress
  - Notes: container runs attempted; DB seeding performed (SQLite) and seed file missing; app crashed earlier due to GERDA DI activation but defensive guards were added.
- [ ] Record results of curl tests — Not started

**Next actions (pick one):**

- Re-run the container with mounted `data` and verify `/css/bundle.css`:

```fish
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_URLS="http://+:8080" \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/ticketmasala.db" \
  -v $PWD/data:/app/data \
  ticketmasala:local
# then in another shell
curl -I http://localhost:8080/css/bundle.css
```

- Or skip DB seeding temporarily (fast): set `SKIP_DB_SEED=true` and add an env-check around `DbSeeder.SeedAsync()` in `Program.cs`.

- If you want, I can implement the `SKIP_DB_SEED` toggle and re-run the container so we can immediately validate static assets.

---

Created by GitHub Copilot (assistant). If you'd like a different format or location for this file, tell me where to put it.
