# Troubleshooting Guide

Common issues and solutions for Ticket Masala.

---

## Database Issues

### "Unable to open database file"

**Symptom:** Application fails to start with SQLite error.

**Solutions:**
```bash
# Check file permissions
ls -la src/TicketMasala.Web/app.db

# Ensure directory is writable
chmod 755 src/TicketMasala.Web/

# Delete and recreate
rm -f src/TicketMasala.Web/app.db*
dotnet run --project src/TicketMasala.Web/
```

### "No such table"

**Symptom:** EF Core can't find expected tables.

**Solutions:**
```bash
# Apply pending migrations
dotnet ef database update --project src/TicketMasala.Web

# Or recreate database
rm -f src/TicketMasala.Web/app.db*
dotnet run --project src/TicketMasala.Web/
```

### "Database is locked"

**Symptom:** Concurrent access errors.

**Solutions:**
1. Close any database browsers (DB Browser for SQLite)
2. Stop other instances of the application
3. Check for zombie processes: `ps aux | grep dotnet`

---

## Build Issues

### "Target framework 'net10.0' not found"

**Solution:** Install .NET 10 SDK:
```bash
# Check installed versions
dotnet --list-sdks

# Download from: https://dotnet.microsoft.com/download
```

### "Package restore failed"

**Solutions:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore with verbose output
dotnet restore --verbosity detailed
```

### "The type or namespace could not be found"

**Solutions:**
1. Run `dotnet restore`
2. Close and reopen IDE
3. Delete `bin/` and `obj/` folders, then rebuild

---

## Configuration Issues

### "GERDA config not found"

**Symptom:** Application can't find `masala_config.json`.

**Solutions:**
```bash
# Check config path
echo $MASALA_CONFIG_PATH

# Verify file exists
ls -la config/masala_config.json

# Set path explicitly
export MASALA_CONFIG_PATH=$(pwd)/config
```

### "Domain configuration file not found"

**Symptom:** `masala_domains.yaml` not loading.

**Solutions:**
```bash
# Check YAML syntax
pip install yamllint
yamllint config/masala_domains.yaml

# Verify file exists in config directory
ls -la $MASALA_CONFIG_PATH/masala_domains.yaml
```

### "Strategy validation FAILED"

**Symptom:** Invalid AI strategy name in configuration.

**Valid strategies:**
- Ranking: `WSJF`, `SeasonalPriority`, `RiskScore`
- Dispatching: `MatrixFactorization`, `ZoneBased`
- Estimating: `CategoryLookup`, `LegalComplexity`

---

## Runtime Issues

### "Port already in use"

**Symptom:** Address already in use error.

**Solutions:**
```bash
# Find process using port
lsof -i :5054

# Kill process
kill -9 <PID>

# Or use different port
dotnet run --urls "http://localhost:5055"
```

### "Hot reload not working"

**Solutions:**
```bash
# Use watch command
dotnet watch run --project src/TicketMasala.Web/

# Check file watcher limits (Linux)
cat /proc/sys/fs/inotify/max_user_watches

# Increase if needed
echo fs.inotify.max_user_watches=524288 | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

### "Static files not loading"

**Solutions:**
1. Check `wwwroot/` directory exists
2. Verify `app.UseStaticFiles()` in middleware
3. Clear browser cache

---

## Authentication Issues

### "Login fails with valid credentials"

**Solutions:**
1. Reset database to get fresh seed data
2. Check password: Default is `Admin123!` / `Employee123!` / `Customer123!`
3. Verify Identity tables exist in database

### "Access denied after login"

**Symptom:** User authenticated but gets 403 errors.

**Solutions:**
1. Check user's role assignments in database
2. Verify `[Authorize(Roles = "...")]` attributes match

---

## GERDA AI Issues

### "GERDA not processing tickets"

**Solutions:**
1. Check if GERDA is enabled:
   ```json
   {
     "GerdaAI": {
       "IsEnabled": true
     }
   }
   ```
2. Review logs for GERDA errors
3. Ensure background services are running

### "Recommendations not appearing"

**Solutions:**
1. Check `MinHistoryForAffinityMatch` setting
2. Ensure enough historical data exists
3. Verify agent workload data is populated

---

## Docker Issues

### "Container exits immediately"

**Solutions:**
```bash
# Check logs
docker logs <container-id>

# Run interactively
docker run -it ticket-masala /bin/bash
```

### "Volume mount permissions"

**Solutions:**
```bash
# Fix permissions on Linux
sudo chown -R 1000:1000 ./tenants

# Or run container as current user
docker run --user $(id -u):$(id -g) ...
```

---

## Performance Issues

### "Slow page loads"

**Solutions:**
1. Check for N+1 queries in logs
2. Enable response caching
3. Review EF Core query generation

### "High memory usage"

**Solutions:**
1. Enable server garbage collection
2. Review `IMemoryCache` usage
3. Check for memory leaks with `dotnet-counters`

---

## Logging & Debugging

### Enable detailed logging

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### View SQL queries

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

---

## Getting Help

1. **Check logs:** Review console output and log files
2. **Search issues:** Check GitHub issues for similar problems
3. **Documentation:** Review relevant docs in `docs/` directory
4. **Debug mode:** Run with `ASPNETCORE_ENVIRONMENT=Development`

---

## Further Reading

- [Development Guide](DEVELOPMENT.md)
- [Configuration Guide](CONFIGURATION.md)
- [Testing Guide](TESTING.md)
