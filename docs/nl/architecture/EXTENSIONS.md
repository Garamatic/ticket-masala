# Extensies Referentie

Documentatie voor Dependency Injection (DI) extensies in Ticket Masala.

## Overzicht

Extensiemethoden in `Extensions/` organiseren de DI-registratie in logische groepen, waardoor `Program.cs` overzichtelijk blijft.

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

## Extensiemethoden

### AddMasalaConfiguration

Registreert sterk getypeerde configuratie.

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

Configureert EF Core en de databaseverbinding.

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

Configureert ASP.NET Identity.

```csharp
public static IServiceCollection AddMasalaIdentity(this IServiceCollection services)
{
    services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Wachtwoordinstellingen
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        
        // Lockout-instellingen
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

Registreert repositories voor gegevenstoegang.

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

Registreert event-observers.

```csharp
public static IServiceCollection AddObservers(this IServiceCollection services)
{
    // Ticket-observers
    services.AddScoped<ITicketObserver, GerdaTicketObserver>();
    services.AddScoped<ITicketObserver, NotificationTicketObserver>();
    services.AddScoped<ITicketObserver, LoggingTicketObserver>();
    
    // Project-observers
    services.AddScoped<IProjectObserver, LoggingProjectObserver>();
    services.AddScoped<IProjectObserver, NotificationProjectObserver>();
    
    // Reactie-observers
    services.AddScoped<ICommentObserver, CommentObservers>();
    
    return services;
}
```

---

### AddCoreServices

Registreert services voor bedrijfslogica.

```csharp
public static IServiceCollection AddCoreServices(this IServiceCollection services)
{
    // Ticket-services
    services.AddScoped<ITicketService, TicketService>();
    services.AddScoped<ITicketFactory, TicketFactory>();
    services.AddScoped<ITicketQueryService, TicketQueryService>();
    services.AddScoped<ITicketCommandService, TicketCommandService>();
    
    // Project-services
    services.AddScoped<IProjectService, ProjectService>();
    
    // Meldingsservices
    services.AddScoped<INotificationService, NotificationService>();
    
    // Import-services
    services.AddScoped<ICsvImportService, CsvImportService>();
    services.AddScoped<IEmailIngestionService, EmailIngestionService>();
    
    return services;
}
```

---

### AddGerdaServices

Registreert GERDA AI-services.

```csharp
public static IServiceCollection AddGerdaServices(
    this IServiceCollection services,
    IWebHostEnvironment environment,
    string configBasePath)
{
    // Laad GERDA-configuratie
    var configPath = Path.Combine(configBasePath, "masala_config.json");
    var gerdaConfig = GerdaConfig.LoadFromFile(configPath);
    services.AddSingleton(gerdaConfig);
    
    if (gerdaConfig.GerdaAI.IsEnabled)
    {
        // Core GERDA-services
        services.AddScoped<IGerdaService, GerdaService>();
        services.AddScoped<IGroupingService, GroupingService>();
        services.AddScoped<IEstimatingService, EstimatingService>();
        services.AddScoped<IRankingService, WsjfRankingService>();
        services.AddScoped<IDispatchingService, DispatchingService>();
        services.AddScoped<IAnticipationService, AnticipationService>();
        
        // Achtergrondtaken
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

### AddMasalaApi

Configureert API-functies.

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

## Extensies Mapstructuur

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
└── TicketExtensions.cs         # Helpers voor entiteitsmapping
```

---

## Verdere Informatie

- [Ontwikkelingsgids](../guides/DEVELOPMENT.md) - Applicatie opstarten
- [Architectuuroverzicht](SUMMARY.md) - Systeemontwerp
