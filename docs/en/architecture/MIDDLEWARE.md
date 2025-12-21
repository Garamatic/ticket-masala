# Middleware Reference

Documentation for custom middleware in Ticket Masala.

## Middleware Pipeline

```
Request
    ↓
┌──────────────────────────────┐
│  Exception Handler          │
├──────────────────────────────┤
│  Request Logging             │
├──────────────────────────────┤
│  Tenant Resolution           │
├──────────────────────────────┤
│  Authentication              │
├──────────────────────────────┤
│  Authorization               │
├──────────────────────────────┤
│  Rate Limiting               │
├──────────────────────────────┤
│  Static Files                │
├──────────────────────────────┤
│  Routing → Controllers       │
└──────────────────────────────┘
    ↓
Response
```

---

## Custom Middleware

### TenantResolutionMiddleware

Resolves the current tenant from request context.

**Location:** `Middleware/TenantResolutionMiddleware.cs`

```csharp
public class TenantResolutionMiddleware
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        // Determine tenant from subdomain, header, or path
        var tenantId = ResolveTenantId(context);
        
        tenantContext.TenantId = tenantId;
        tenantContext.Configuration = LoadTenantConfig(tenantId);
        
        await _next(context);
    }
}
```

**Resolution strategies:**
1. Subdomain: `government.ticketmasala.com` → `government`
2. Header: `X-Tenant-Id: government`
3. Path: `/tenant/government/...`

---

### RequestLoggingMiddleware

Logs request/response details for debugging.

**Location:** `Middleware/RequestLoggingMiddleware.cs`

```csharp
public class RequestLoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation(
            "Request: {Method} {Path}",
            context.Request.Method,
            context.Request.Path);
        
        await _next(context);
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Response: {StatusCode} in {ElapsedMs}ms",
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}
```

---

### ExceptionHandlerMiddleware

Global exception handling and error responses.

**Location:** `Middleware/ExceptionHandlerMiddleware.cs`

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
            _logger.LogError(ex, "Unhandled exception");
            
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal Server Error",
                message = _env.IsDevelopment() ? ex.Message : "An error occurred"
            });
        }
    }
}
```

---

## Registration

Middleware is registered in `Extensions/MiddlewareExtensions.cs`:

```csharp
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseMasalaCore(
        this IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        // Exception handling first (catches all downstream errors)
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        
        // Logging
        app.UseMiddleware<RequestLoggingMiddleware>();
        
        // Tenant resolution
        app.UseMiddleware<TenantResolutionMiddleware>();
        
        // Standard middleware
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        
        return app;
    }
}
```

---

## Creating Custom Middleware

### Pattern 1: Inline Lambda

```csharp
app.Use(async (context, next) =>
{
    // Before request
    context.Response.Headers["X-Custom-Header"] = "value";
    
    await next();
    
    // After response
});
```

### Pattern 2: Class-Based

```csharp
public class CustomMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomMiddleware> _logger;

    public CustomMiddleware(RequestDelegate next, ILogger<CustomMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Before
        _logger.LogInformation("Processing request");
        
        await _next(context);
        
        // After
        _logger.LogInformation("Request completed");
    }
}

// Registration
app.UseMiddleware<CustomMiddleware>();
```

### Pattern 3: Factory-Based (with Scoped Services)

```csharp
public class ScopedMiddleware : IMiddleware
{
    private readonly IScopedService _service;

    public ScopedMiddleware(IScopedService service)
    {
        _service = service;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await _service.DoSomethingAsync();
        await next(context);
    }
}

// Registration
services.AddScoped<ScopedMiddleware>();
app.UseMiddleware<ScopedMiddleware>();
```

---

## Health Checks

Built-in health check endpoint.

**Location:** `Health/HealthCheckExtensions.cs`

```csharp
public static IServiceCollection AddMasalaMonitoring(this IServiceCollection services)
{
    services.AddHealthChecks()
        .AddDbContextCheck<MasalaDbContext>("database")
        .AddCheck("gerda", () => 
        {
            // Check GERDA service health
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

## Best Practices

1. **Order matters** - Exception handler first, authentication before authorization
2. **Short-circuit when possible** - Return early for invalid requests
3. **Avoid blocking** - Use async throughout
4. **Inject via constructor** - Only `RequestDelegate` and singletons
5. **Use scoped services via `InvokeAsync`** - DI resolves per request

---

## Further Reading

- [Development Guide](../guides/DEVELOPMENT.md) - Application startup
- [Architecture Overview](SUMMARY.md) - System design
