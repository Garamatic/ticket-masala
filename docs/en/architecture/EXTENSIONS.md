# Extensions Reference

Documentation for dependency injection extensions in Ticket Masala.

## Overview

Extension methods in `Extensions/` organize DI registration into logical groups, keeping `Program.cs` clean.

```csharp
// Program.cs
builder.Services.AddMasalaConfiguration(builder.Configuration);
builder.Services.AddMasalaDatabase(builder.Configuration, builder.Environment);
builder.Services.AddMasalaIdentity();
builder.Services.AddRepositories();
builder.Services.AddObservers();
builder.Services.AddCoreServices();
builder.Services.AddGerdaServices(builder.Environment, configPath);
```

---

## Extension Methods

### AddMasalaConfiguration

Registers strongly-typed configuration.

```csharp
public static IServiceCollection AddMasalaConfiguration(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<MasalaOptions>(configuration.GetSection("Masala"));
    services.Configure<GerdaOptions>(configuration.GetSection("GerdaAI"));
    
    return services;
}
```

---

### AddMasalaDatabase

Configures EF Core and database connection.

```csharp
public static IServiceCollection AddMasalaDatabase(
    this IServiceCollection services,
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=app.db";
    
    services.AddDbContext<MasalaDbContext>(options =>
    {
        options.UseSqlite(connectionString);
        
        if (environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });
    
    return services;
}
```

---

### AddMasalaIdentity

Configures ASP.NET Identity.

```csharp
public static IServiceCollection AddMasalaIdentity(this IServiceCollection services)
{
    services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        
        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<MasalaDbContext>()
    .AddDefaultTokenProviders();
    
    return services;
}
```

---

### AddRepositories

Registers data access repositories.

```csharp
public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    services.AddScoped<ITicketRepository, TicketRepository>();
    services.AddScoped<IProjectRepository, ProjectRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<INotificationRepository, NotificationRepository>();
    
    return services;
}
```

---

### AddObservers

Registers event observers.

```csharp
public static IServiceCollection AddObservers(this IServiceCollection services)
{
    // Ticket observers
    services.AddScoped<ITicketObserver, GerdaTicketObserver>();
    services.AddScoped<ITicketObserver, NotificationTicketObserver>();
    services.AddScoped<ITicketObserver, LoggingTicketObserver>();
    
    // Project observers
    services.AddScoped<IProjectObserver, LoggingProjectObserver>();
    services.AddScoped<IProjectObserver, NotificationProjectObserver>();
    
    // Comment observers
    services.AddScoped<ICommentObserver, CommentObservers>();
    
    return services;
}
```

---

### AddCoreServices

Registers business logic services.

```csharp
public static IServiceCollection AddCoreServices(this IServiceCollection services)
{
    // Ticket services
    services.AddScoped<ITicketService, TicketService>();
    services.AddScoped<ITicketFactory, TicketFactory>();
    services.AddScoped<ITicketQueryService, TicketQueryService>();
    services.AddScoped<ITicketCommandService, TicketCommandService>();
    
    // Project services
    services.AddScoped<IProjectService, ProjectService>();
    
    // Notification services
    services.AddScoped<INotificationService, NotificationService>();
    
    // Import services
    services.AddScoped<ICsvImportService, CsvImportService>();
    services.AddScoped<IEmailIngestionService, EmailIngestionService>();
    
    return services;
}
```

---

### AddGerdaServices

Registers GERDA AI services.

```csharp
public static IServiceCollection AddGerdaServices(
    this IServiceCollection services,
    IWebHostEnvironment environment,
    string configBasePath)
{
    // Load GERDA configuration
    var configPath = Path.Combine(configBasePath, "masala_config.json");
    var gerdaConfig = GerdaConfig.LoadFromFile(configPath);
    services.AddSingleton(gerdaConfig);
    
    if (gerdaConfig.GerdaAI.IsEnabled)
    {
        // Core GERDA services
        services.AddScoped<IGerdaService, GerdaService>();
        services.AddScoped<IGroupingService, GroupingService>();
        services.AddScoped<IEstimatingService, EstimatingService>();
        services.AddScoped<IRankingService, WsjfRankingService>();
        services.AddScoped<IDispatchingService, DispatchingService>();
        services.AddScoped<IAnticipationService, AnticipationService>();
        
        // Background jobs
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<GerdaBackgroundJobService>();
    }
    else
    {
        services.AddScoped<IGerdaService, NoOpGerdaService>();
    }
    
    return services;
}
```

---

### AddBackgroundServices

Registers hosted services.

```csharp
public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
{
    services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
    services.AddHostedService<QueuedHostedService>();
    
    return services;
}
```

---

### AddMasalaApi

Configures API features.

```csharp
public static IServiceCollection AddMasalaApi(this IServiceCollection services)
{
    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    
    services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });
    
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
    
    return services;
}
```

---

### AddMasalaFrontend

Configures MVC and Razor views.

```csharp
public static IServiceCollection AddMasalaFrontend(this IServiceCollection services)
{
    services.AddControllersWithViews()
        .AddViewLocalization()
        .AddDataAnnotationsLocalization();
    
    services.AddRazorPages();
    
    return services;
}
```

---

## Extension Directory

```
Extensions/
├── ConfigurationExtensions.cs  # AddMasalaConfiguration
├── DatabaseExtensions.cs       # AddMasalaDatabase
├── IdentityExtensions.cs       # AddMasalaIdentity
├── RepositoryExtensions.cs     # AddRepositories
├── ObserverExtensions.cs       # AddObservers
├── ServiceExtensions.cs        # AddCoreServices
├── GerdaExtensions.cs          # AddGerdaServices
├── ApiExtensions.cs            # AddMasalaApi
├── FrontendExtensions.cs       # AddMasalaFrontend
├── MiddlewareExtensions.cs     # UseMasalaCore
└── TicketExtensions.cs         # Entity mapping helpers
```

---

## Creating Custom Extensions

```csharp
public static class CustomExtensions
{
    public static IServiceCollection AddCustomFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register options
        services.Configure<CustomOptions>(configuration.GetSection("Custom"));
        
        // Register services
        services.AddScoped<ICustomService, CustomService>();
        
        // Register validators
        services.AddScoped<IValidator<CustomModel>, CustomValidator>();
        
        return services;
    }
}

// Usage in Program.cs
builder.Services.AddCustomFeature(builder.Configuration);
```

---

## Further Reading

- [Development Guide](../guides/DEVELOPMENT.md) - Application startup
- [Architecture Overview](SUMMARY.md) - System design
