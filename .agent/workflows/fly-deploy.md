---
description: Deploy and scale Ticket Masala on Fly.io with auto-scaling
---

# Deploy and Scale on Fly.io

This workflow guides you through deploying Ticket Masala to Fly.io with the "Sleeping Giant" configuration for cost-effective operation.

## Prerequisites

- Fly.io CLI installed (`fly` command available)
- Authenticated with Fly.io (`fly auth login`)
- Application already created (`fly apps create ticket-masala`) or existing

## Steps

### 1. Deploy the Updated Configuration

Deploy the application with the new auto-scaling configuration:

```bash
fly deploy
```

This will:
- Build the Docker image
- Deploy with the updated `fly.toml` settings
- Enable auto-start and auto-stop machines
- Set minimum running machines to 0

### 2. Scale RAM to 2GB

// turbo
Upgrade the machine to 2GB RAM for better performance:

```bash
fly scale vm shared-cpu-1x --memory 2048
```

**Cost**: ~$0.01/hour when running, but $0.00 when stopped (auto-scales to zero)

### 3. Verify Auto-Scaling Works

Check the current status:

```bash
fly status
```

You should see the machine running initially.

### 4. Test Auto-Stop

Wait 5-10 minutes without accessing the application, then check status again:

```bash
fly status
```

The machine should show as "stopped" if auto-scaling is working correctly.

### 5. Test Auto-Start (Cold Start)

Visit your application URL. The first request will take 3-5 seconds as the machine wakes up. Subsequent requests will be fast.

### 6. Monitor Costs

Check your billing dashboard:

```bash
fly dashboard
```

Navigate to the billing section to ensure costs remain minimal.

## Important Warnings

> [!WARNING]
> **Avoid External Monitoring**: Do not use services like UptimeRobot or similar to ping your application. They will keep the machine running 24/7, costing ~$13/month instead of pennies.

> [!CAUTION]
> **Health Checks**: The internal Fly.io health check at `/health` is configured correctly and won't prevent auto-stop. External monitoring is the issue.

## Troubleshooting

### Machine Won't Stop

1. Check for background workers or scheduled tasks
2. Verify no external monitors are pinging the app
3. Review logs: `fly logs`

### Machine Won't Start

1. Check deployment logs: `fly logs`
2. Verify the health check is passing: `fly checks list`
3. Ensure the internal port (8080) is correct

### High Costs

If your bill is higher than expected:

```bash
fly scale show
```

Check how long machines have been running:

```bash
fly machine list
```

## Additional Commands

- **View logs**: `fly logs`
- **SSH into machine**: `fly ssh console`
- **View machine status**: `fly machine list`
- **Restart machine**: `fly machine restart <machine-id>`
