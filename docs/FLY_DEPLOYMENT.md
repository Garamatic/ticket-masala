# Fly.io Deployment Guide for Ticket Masala

## Overview

Ticket Masala is configured to use Fly.io's **"Sleeping Giant"** strategy:
- **2GB RAM** for smooth performance when active
- **Auto-scales to zero** when idle to minimize costs
- **Cold start**: 3-5 seconds on first request after sleep

## Cost Structure

| Scenario | Monthly Cost |
|----------|--------------|
| **Ideal (Demo)**: 5 hours/month usage | ~$0.05 |
| **Light**: 50 hours/month usage | ~$0.50 |
| **Heavy**: 24/7 running (2GB) | ~$13.00 |

## Configuration

The `fly.toml` configures:

```toml
[http_service]
  internal_port = 8080
  force_https = true
  auto_stop_machines = true      # Stops when idle
  auto_start_machines = true     # Starts on request
  min_machines_running = 0       # Allows complete shutdown
```

## Deployment

See the workflow: `/fly-deploy` or view `.agent/workflows/fly-deploy.md`

Quick start:
```bash
fly deploy
fly scale vm shared-cpu-1x --memory 2048
```

## Critical Cost Warning

⚠️ **DO NOT use external uptime monitors** (UptimeRobot, Pingdom, etc.)

These services will ping your app constantly, preventing it from sleeping and causing 24/7 billing at ~$13/month.

The internal Fly.io health check is configured correctly and won't prevent auto-stop.

## Verifying Auto-Scale Works

1. Deploy and access your app
2. Wait 10 minutes without accessing it
3. Run `fly status` - should show machine stopped
4. Access the app again - first request takes 3-5s (cold start)
5. Check billing dashboard after 24-48 hours

## Alternative: Oracle Cloud

The `fly-refactor.md` document also mentions Oracle Cloud's free tier (24GB RAM, free forever), but it comes with risks:
- Instances can be reclaimed if idle for too long
- Very limited availability in most regions
- More complex setup and management

For a demo app, **Fly.io's sleeping strategy is recommended**.

## Support

- Fly.io docs: https://fly.io/docs/
- View deployment workflow: `cat .agent/workflows/fly-deploy.md`
- Check app status: `fly status`
- View logs: `fly logs`
