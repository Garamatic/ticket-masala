# Middleware Referentie

Documentatie voor aangepaste middleware in Ticket Masala.

## Middleware Pipeline

```
Verzoek (Request)
    ↓
┌──────────────────────────────┐
│  Exception Handler          │
├──────────────────────────────┤
│  Request Logging             │
├──────────────────────────────┤
│  Tenant-resolutie            │
├──────────────────────────────┤
│  Authenticatie               │
├──────────────────────────────┤
│  Autorisatie                 │
├──────────────────────────────┤
│  Rate Limiting               │
├──────────────────────────────┤
│  Statische bestanden         │
├──────────────────────────────┤
│  Routing → Controllers       │
└──────────────────────────────┘
    ↓
Respons (Response)
```

---

## Aangepaste Middleware

### TenantResolutionMiddleware

Resolvert de huidige tenant uit de context van het verzoek.

**Locatie:** `Middleware/TenantResolutionMiddleware.cs`

```csharp
public class TenantResolutionMiddleware
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        // Bepaal tenant op basis van subdomein, header of pad
        var tenantId = ResolveTenantId(context);
        
        tenantContext.TenantId = tenantId;
        tenantContext.Configuration = LoadTenantConfig(tenantId);
        
        await _next(context);
    }
}
```

**Resolutiestrategieën:**
1. Subdomein: `government.ticketmasala.com` → `government`
2. Header: `X-Tenant-Id: government`
3. Pad: `/tenant/government/...`

---

### RequestLoggingMiddleware

Logt details van verzoek en respons voor foutopsporing (debugging).

**Locatie:** `Middleware/RequestLoggingMiddleware.cs`

```csharp
public class RequestLoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation(
            "Verzoek: {Method} {Path}",
            context.Request.Method,
            context.Request.Path);
        
        await _next(context);
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Respons: {StatusCode} in {ElapsedMs}ms",
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}
```

---

### ExceptionHandlerMiddleware

Globale foutafhandeling en foutantwoorden.

**Locatie:** `Middleware/ExceptionHandlerMiddleware.cs`

```csharp
public class ExceptionHandlerMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onbehandelde uitzondering");
            
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal Server Error",
                message = _env.IsDevelopment() ? ex.Message : "Er is een fout opgetreden"
            });
        }
    }
}
```

---

## Registratie

Middleware wordt geregistreerd in `Extensions/MiddlewareExtensions.cs`:

```csharp
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseMasalaCore(
        this IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        // Foutafhandeling eerst (vangt alle fouten in de pijplijn op)
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        
        // Logging
        app.UseMiddleware<RequestLoggingMiddleware>();
        
        // Tenant-resolutie
        app.UseMiddleware<TenantResolutionMiddleware>();
        
        // Standaard middleware
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        
        return app;
    }
}
```

---

## Health Checks

Ingebouwd endpoint voor statuscontrole.

**Locatie:** `Health/HealthCheckExtensions.cs`

```csharp
public static IServiceCollection AddMasalaMonitoring(this IServiceCollection services)
{
    services.AddHealthChecks()
        .AddDbContextCheck<MasalaDbContext>("database")
        .AddCheck("gerda", () => 
        {
            // Controleer status van de GERDA-service
            return HealthCheckResult.Healthy();
        });
    
    return services;
}
```

**Endpoint:** `/health`

---

## Rate Limiting

ASP.NET Core rate limiting middleware.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

app.UseRateLimiter();
```

---

## Beste Praktijken (Best Practices)

1. **Volgorde is belangrijk** - Foutafhandeling eerst, authenticatie vóór autorisatie.
2. **Kies voor 'short-circuit' waar mogelijk** - Beëindig vroeg bij ongeldige verzoeken.
3. **Vermijd blokkerende code** - Gebruik overal async.
4. **Injecteer via constructor** - Alleen `RequestDelegate` en singletons.
5. **Gebruik scoped services via `InvokeAsync`** - DI wordt per verzoek geresolved.

---

## Verdere Informatie

- [Ontwikkelingsgids](../guides/DEVELOPMENT.md) - Applicatie opstarten
- [Architectuuroverzicht](SUMMARY.md) - Systeemontwerp
