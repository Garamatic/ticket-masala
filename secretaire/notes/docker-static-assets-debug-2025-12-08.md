## Docker / Static Assets Debug — 2025-12-08

Summary
- Performed debug steps to investigate missing CSS/static assets when running the app in Docker.

What I changed or verified
- Ensured published `wwwroot` is included in the image and fixed ownership (chown) so the runtime user can read static files.
- Investigated `~/css/bundle.css` usage in `Views/Shared/_Layout.cshtml` and confirmed the app uses WebOptimizer to produce `/css/bundle.css` from `css/design-system.css` and `css/site.css`.
- Applied defensive guards in `Program.cs` so optional GERDA services do not crash the app when configuration is absent.

Current state and important findings
- The publish output inside the image contains `wwwroot/css` and compressed assets; ownership is set so the runtime user can read them.
- The primary blocker for validating HTTP access to `/css/bundle.css` was application startup behavior:
  - The app attempted database seeding without a configured SQL connection string and failed during `DbSeeder.SeedAsync()`.
  - Running the published app with the wrong content root prevented static files from being found (the host logged `The WebRootPath was not found`).

Next steps (recommended)
- For quick verification: start the published app with the correct content root (or run the container with a valid DB connection string or with seeding disabled) and curl `/css/bundle.css`.
- Optionally: add an environment toggle to skip DB seeding in container runs, reducing friction when verifying static assets.

Files touched during this debugging session
- `Dockerfile` (copy + chown ordering to ensure ownership of `/app`)
- `src/TicketMasala.Web/Program.cs` (added guards to optional services and strategy validation)
- `.gitignore` (this commit adds `out/` and `deploy/linux-pilot/`)

Recorded-by: GitHub Copilot (paired with user)
