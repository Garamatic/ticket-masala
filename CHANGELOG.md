# Changelog

## Unreleased

- Add `SKIP_DB_SEED` toggle (feature branch `feature/skip-db-seed-toggle`) to optionally skip database seeding at startup. This was used during Docker debugging to allow the app to start quickly and verify runtime-generated static assets.
- Docker/static-assets debugging: verified that WebOptimizer generates `/css/bundle.css` at runtime in the container. Verified `HEAD /css/bundle.css` and `HEAD /css/site.css` return 200 OK when the app runs in Docker (see debugging notes in `todo.md`).


<!--
Guidelines: keep Unreleased section for ongoing work.
Move entries into versioned headings when releasing.
-->