Since your app is just a demo and doesn't need to be up 24/7, you are in a perfect position to "cheat" the system. You can absolutely run a high-RAM app on Fly.io for pennies if it sleeps when no one is looking.

Here is how to set up your **"Sleeping Giant"** on Fly.io, and the lowdown on the Oracle beast.

### 1\. The Fly.io "Sleeping Giant" Strategy

You can boost your RAM to 2GB (or even 4GB) and simply tell Fly to turn it off when you aren't using it.

  * **The Mechanic:** Fly.io machines are like lightbulbs. You only pay when they are lit. A 2GB RAM machine costs about **$0.01 per hour** while running. If you only show your demo for 5 hours a month, your bill is **$0.05**.
  * **The Trade-off:** "Cold Starts." When you open your demo after a break, the first request will take 3-5 seconds to load while the machine wakes up. For a demo, this is usually acceptable.

**How to do it:**

1.  **Scale up the RAM:** Run this command in your terminal to give your app 2GB of RAM.
    ```bash
    fly scale vm shared-cpu-1x --memory 2048
    ```
2.  **Configure "Scale to Zero":** Update your `fly.toml` file to ensure it shuts down aggressively when idle.

<!-- end list -->

```toml
[http_service]
  internal_port = 8080
  force_https = true
  auto_stop_machines = true
  auto_start_machines = true
  min_machines_running = 0
  processes = ["app"]
```

  * `min_machines_running = 0` is the magic switch. It allows the last light to be turned off.
  * `auto_stop_machines = true` tells Fly to kill the machine after a few minutes of no traffic.

**Important Cost Warning:**
If you leave this running 24/7 by accident (e.g., you have a background worker or a health check pinging it constantly), a 2GB machine will cost \~$13/month. Ensure you don't have uptime monitors (like UptimeRobot) pinging it, or it will never sleep\!

-----

### 2\. The Oracle Cloud "Beast" (The 24GB Free Tier)

You asked about Oracle. It is the "too good to be true" option that is actually true, but comes with a massive asterisk.

**The Offer:**

  * **4 ARM vCPUs**
  * **24 GB of RAM** (Yes, twenty-four)
  * **200 GB Storage**
  * **Price:** €0.00 / Forever.

**The Catch (The "Use It or Lose It" Rule):**
Oracle is aggressive about reclaiming "idle" resources. If your CPU or RAM usage stays under 10-15% for a week, they might delete your instance to give the space to someone else. Since you want a "demo" app that sits idle, **this is dangerous for you.**

**The Workaround:**

1.  **Pay-As-You-Go Trick:** Upgrade your account to "Pay As You Go." You still get the free tier limits (first 4 cores/24GB are free), but because you have a credit card on file and a "paid" status, they typically do not reclaim your idle instances.
2.  **Availability:** The biggest issue right now is finding a region with capacity. People hoard these free servers. If you try to create one in `Frankfurt` or `Amsterdam`, you might get an "Out of Capacity" error for weeks.

### Summary Comparison

| Feature | Fly.io (Sleeping Strategy) | Oracle Cloud (Free Tier) |
| :--- | :--- | :--- |
| **RAM** | 2GB+ (Pay per second) | **24GB** (Always on) |
| **Cost** | \< €1.00 (if low traffic) | **€0.00** |
| **Speed** | 3-5s delay on first load | Instant (Always on) |
| **Setup** | Easy (`fly deploy`) | Hard (Manual VPS setup) |
| **Risk** | Accidental bill if it doesn't sleep | Server deleted if idle |

**My Advice:**
Stick with **Fly.io** for now.

1.  Run `fly scale vm shared-cpu-1x --memory 2048`.
2.  Set `min_machines_running = 0`.
3.  Accept the 5-second startup time.

This solves your "slowness" (lack of RAM) and your "billing" (it costs nothing when off) without needing to learn how to manage an Oracle Linux server.

**Would you like me to explain how to verify if your Fly app is actually going to sleep so you don't get a surprise bill?**