# GERDA Implementation Roadmap

**Project:** Ticket Masala - GERDA AI Integration  
**Current Status:** Core ML.NET services implemented (G+E+R+D+A)  
**Target:** Production-ready TAX department operations cockpit  
**Timeline:** 8 weeks (Dec 2024 - Jan 2025)

---

## Executive Summary

GERDA's AI engine (5 ML.NET services) is **100% implemented**, but **operationele integratie ontbreekt**. Deze roadmap focust op:

1. **Data model extensions** voor tax-specifieke velden
2. **UI components** om GERDA zichtbaar te maken
3. **Controller integration** om AI automatisch te activeren
4. **Configuration enrichment** voor domain-specific rules
5. **Data ingestion** om legacy systems te koppelen

**MVP Delivery:** Week 4 (eind December)  
**Production Ready:** Week 8 (eind Januari)

---

## Current State Assessment

### ✅ What We Have (Sprint 5 Complete)

| Component | Status | Tech Stack |
|-----------|--------|------------|
| G - Grouping | ✅ Implemented | Rule-based LINQ, time-window detection |
| E - Estimating | ✅ Implemented | Category lookup table → Fibonacci points |
| R - Ranking | ✅ Implemented | WSJF algorithm, SLA-aware urgency |
| D - Dispatching | ✅ Implemented | ML.NET Matrix Factorization |
| A - Anticipation | ✅ Implemented | ML.NET Time Series SSA forecasting |
| Database | ✅ Migrated | EstimatedEffortPoints, PriorityScore, GerdaTags |
| DI Registration | ✅ Complete | All services in Program.cs |
| Configuration | ✅ Basic | masala_config.json with G/E/R/D/A settings |

### 🔴 Critical Gaps (Blocking Production Use)

| Gap | Impact | Priority |
|-----|--------|----------|
| **No UI components** | Users can't see GERDA insights | 🔥 P0 |
| **No controller integration** | GERDA never runs automatically | 🔥 P0 |
| **Missing Employee fields** | Dispatching can't match language/skills | 🔥 P0 |
| **Generic category config** | Ranking can't use tax-specific urgency | 🔥 P0 |
| **No data ingestion** | Can't import from Qlik/SAP | 🟡 P1 |
| **No metrics tracking** | Can't measure success | 🟡 P1 |

---

## Sprint Plan (8 Weeks)

### 🏃 **Sprint 6: Foundation & MVP (Week 1-2)**

**Goal:** Make GERDA visible and usable in the application

#### Week 1: Data Model & Config Extensions

**Tasks:**

1. **Extend Employee Model** (4 hours)
   ```csharp
   // Models/Users.cs
   public class Employee : ApplicationUser
   {
       public string Team { get; set; } = string.Empty;
       public int Level { get; set; }
       
       // GERDA Extensions
       public string? Language { get; set; }  // NL, FR, EN, DE
       public string? Specializations { get; set; }  // JSON array: ["NTOF","ENRM"]
       public int MaxCapacityPoints { get; set; } = 40;  // Fibonacci capacity
       public string? Region { get; set; }  // Brussels, Flanders, Wallonia
   }
   ```

2. **Database Migration** (1 hour)
   ```bash
   dotnet ef migrations add AddGerdaEmployeeFields
   dotnet ef database update
   ```

3. **Extend masala_config.json** (2 hours)
   ```json
   {
     "work_queues": [
       {
         "name": "Hotel Tax",
         "categories": ["NTOF", "ENRM", "Fine", "Declaration"],
         "sla_defaults": {
           "NTOF": 30,
           "Fine": 60,
           "ENRM": 90,
           "Declaration": 45
         },
         "urgency_multipliers": {
           "NTOF": 2.0,
           "Fine": 3.0,
           "ENRM": 2.5,
           "Declaration": 1.5
         }
       }
     ]
   }
   ```

4. **Update GerdaConfig.cs** (2 hours)
   - Add `WorkQueue` model class
   - Add `SlaDefaults` dictionary
   - Add `UrgencyMultipliers` dictionary

**Deliverable:** Extended data model + enriched config ✅

---

#### Week 2: Controller Integration & Basic UI

**Tasks:**

1. **Update TicketController.Create()** (3 hours)
   ```csharp
   [HttpPost]
   public async Task<IActionResult> Create(NewTicketViewModel model)
   {
       var ticket = MapViewModelToTicket(model);
       await _context.Tickets.AddAsync(ticket);
       await _context.SaveChangesAsync();
       
       // 🔥 GERDA Integration
       if (_gerdaService.IsEnabled)
       {
           try
           {
               await _gerdaService.ProcessTicketAsync(ticket.Guid);
               TempData["Success"] = "Ticket created and processed by GERDA AI";
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "GERDA processing failed for ticket {Guid}", ticket.Guid);
               TempData["Warning"] = "Ticket created but AI processing failed";
           }
       }
       
       return RedirectToAction("Details", new { id = ticket.Guid });
   }
   ```

2. **Create Ticket Details View Enhancements** (4 hours)
   ```html
   <!-- Views/Ticket/Details.cshtml -->
   <div class="card mb-3">
       <div class="card-header bg-primary text-white">
           <i class="bi bi-robot"></i> GERDA AI Insights
       </div>
       <div class="card-body">
           <div class="row">
               <div class="col-md-4">
                   <strong>Complexity:</strong>
                   <span class="badge bg-info">
                       @Model.EstimatedEffortPoints points
                   </span>
               </div>
               <div class="col-md-4">
                   <strong>Priority Score:</strong>
                   <span class="badge bg-warning">
                       @Model.PriorityScore.ToString("F2")
                   </span>
               </div>
               <div class="col-md-4">
                   <strong>GERDA Tags:</strong>
                   @if (!string.IsNullOrEmpty(Model.GerdaTags))
                   {
                       foreach (var tag in Model.GerdaTags.Split(','))
                       {
                           <span class="badge bg-secondary">@tag</span>
                       }
                   }
               </div>
           </div>
           
           @if (Model.RecommendedAgentId != null)
           {
               <div class="alert alert-success mt-3">
                   <i class="bi bi-person-check"></i>
                   <strong>Recommended Agent:</strong> @Model.RecommendedAgentName
                   <button class="btn btn-sm btn-success float-end" 
                           onclick="assignToRecommended()">
                       Assign to @Model.RecommendedAgentName
                   </button>
               </div>
           }
       </div>
   </div>
   ```

3. **Create Agent Queue View** (5 hours)
   ```csharp
   // Controllers/EmployeeController.cs
   [Authorize(Roles = "Employee,Admin")]
   public async Task<IActionResult> MyQueue()
   {
       var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
       
       // Get prioritized tickets assigned to this agent
       var assignedTickets = await _context.Tickets
           .Where(t => t.ResponsibleId == userId)
           .Where(t => t.TicketStatus != Status.Completed)
           .OrderByDescending(t => t.PriorityScore)
           .ToListAsync();
       
       // Get recommended tickets (not yet assigned)
       var recommendations = await _dispatchingService
           .GetRecommendedTicketsForAgentAsync(userId, count: 5);
       
       var viewModel = new AgentQueueViewModel
       {
           AssignedTickets = assignedTickets,
           RecommendedTickets = recommendations,
           CurrentWorkload = assignedTickets.Sum(t => t.EstimatedEffortPoints),
           MaxCapacity = GetAgentMaxCapacity(userId)
       };
       
       return View(viewModel);
   }
   ```

4. **Create ViewModels** (2 hours)
   - `AgentQueueViewModel`
   - `TicketDetailsViewModel` (extend existing)
   - `TeamDashboardViewModel`

**Deliverable:** GERDA auto-processes new tickets + agents see AI insights ✅

---

### 🚀 **Sprint 7: Enhanced Dispatching & UI (Week 3-4)**

**Goal:** Multi-factor agent matching + team coordinator dashboard

#### Week 3: Enhanced Dispatching Logic

**Tasks:**

1. **Update DispatchingService with Multi-Factor Scoring** (6 hours)
   ```csharp
   private async Task<double> CalculateAffinityScore(
       Employee agent, 
       Ticket ticket, 
       double mlPrediction)
   {
       var scores = new Dictionary<string, double>();
       
       // Factor 1: ML-based past interaction (40%)
       scores["past_interaction"] = mlPrediction * 0.4;
       
       // Factor 2: Category expertise (30%)
       var specializations = JsonSerializer
           .Deserialize<List<string>>(agent.Specializations ?? "[]");
       var categoryMatch = specializations.Contains(ticket.Category) ? 1.0 : 0.3;
       scores["category_expertise"] = categoryMatch * 0.3;
       
       // Factor 3: Language match (20%)
       var customerLanguage = await GetCustomerLanguage(ticket.CustomerId);
       var languageMatch = agent.Language == customerLanguage ? 1.0 : 0.5;
       scores["language_match"] = languageMatch * 0.2;
       
       // Factor 4: Geographic proximity (10%)
       var geoMatch = await CheckGeographicMatch(agent, ticket);
       scores["geography"] = geoMatch * 0.1;
       
       var totalScore = scores.Values.Sum();
       
       _logger.LogDebug(
           "GERDA-D: Agent {AgentId} affinity for ticket {TicketGuid}: " +
           "Past={Past:F2}, Expertise={Exp:F2}, Language={Lang:F2}, Geo={Geo:F2}, Total={Total:F2}",
           agent.Id, ticket.Guid, 
           scores["past_interaction"], scores["category_expertise"],
           scores["language_match"], scores["geography"], totalScore);
       
       return totalScore;
   }
   ```

2. **Add IDispatchingService Extension Methods** (2 hours)
   ```csharp
   Task<List<Ticket>> GetRecommendedTicketsForAgentAsync(string agentId, int count);
   Task<Dictionary<string, int>> GetTeamWorkloadDistributionAsync();
   Task<bool> CheckAgentAvailabilityAsync(string agentId);
   ```

3. **Unit Tests for Dispatching** (3 hours)
   - Test multi-factor scoring
   - Test capacity constraints
   - Test language/specialization matching

**Deliverable:** Intelligent agent-ticket matching met explainable scoring ✅

---

#### Week 4: Team Dashboard & Metrics

**Tasks:**

1. **Create ManagerController.TeamDashboard()** (6 hours)
   ```csharp
   [Authorize(Roles = "Admin")]
   public async Task<IActionResult> TeamDashboard()
   {
       var workloadDistribution = await _dispatchingService
           .GetTeamWorkloadDistributionAsync();
       
       var capacityRisk = await _anticipationService
           .CheckCapacityRiskAsync();
       
       var slaStatus = await CalculateSlaComplianceAsync();
       
       var viewModel = new TeamDashboardViewModel
       {
           AgentWorkloads = workloadDistribution,
           CapacityRisk = capacityRisk,
           SlaBreachRate = slaStatus.BreachRate,
           AvgTimeToFirstAction = slaStatus.AvgTimeToFirstAction,
           TicketsByPriority = await GetTicketDistributionAsync(),
           ForecastChart = await GenerateForecastChartDataAsync()
       };
       
       return View(viewModel);
   }
   ```

2. **Create Dashboard View with Charts** (6 hours)
   - Chart.js for workload distribution
   - Capacity risk alerts
   - SLA compliance gauge
   - 30-day forecast visualization

3. **Add Background Job for Priority Recalculation** (4 hours)
   ```csharp
   // Services/GerdaBackgroundService.cs
   public class GerdaBackgroundService : BackgroundService
   {
       protected override async Task ExecuteAsync(CancellationToken ct)
       {
           while (!ct.IsCancellationRequested)
           {
               try
               {
                   // Recalculate priorities every 6 hours
                   await _rankingService.RecalculateAllPrioritiesAsync();
                   
                   // Retrain dispatching model daily
                   if (DateTime.Now.Hour == 2) // 2 AM
                   {
                       await _dispatchingService.RetrainModelAsync();
                   }
                   
                   await Task.Delay(TimeSpan.FromHours(6), ct);
               }
               catch (Exception ex)
               {
                   _logger.LogError(ex, "GERDA background job failed");
               }
           }
       }
   }
   ```

**Deliverable:** Manager dashboard met real-time insights + automated maintenance ✅

---

### 📊 **Sprint 8: Data Ingestion & Analytics (Week 5-6)**

**Goal:** Import legacy data + track KPIs

#### Week 5: File Watcher Implementation

**Tasks:**

1. **Create FileWatcherService** (6 hours)
   ```csharp
   public class QlikFileWatcherService : BackgroundService
   {
       private readonly string _watchPath;
       private readonly IServiceProvider _serviceProvider;
       
       protected override Task ExecuteAsync(CancellationToken ct)
       {
           var watcher = new FileSystemWatcher(_watchPath)
           {
               Filter = "*.xlsx",
               EnableRaisingEvents = true
           };
           
           watcher.Created += async (sender, e) =>
           {
               _logger.LogInformation("New Qlik export detected: {Path}", e.FullPath);
               await ProcessQlikExport(e.FullPath);
           };
           
           return Task.CompletedTask;
       }
       
       private async Task ProcessQlikExport(string filePath)
       {
           using var scope = _serviceProvider.CreateScope();
           var context = scope.ServiceProvider.GetRequiredService<ITProjectDB>();
           
           using var package = new ExcelPackage(new FileInfo(filePath));
           var worksheet = package.Workbook.Worksheets[0];
           
           var tickets = new List<Ticket>();
           
           for (int row = 2; row <= worksheet.Dimension.Rows; row++)
           {
               var ticket = new Ticket
               {
                   // Map Excel columns to Ticket properties
                   Description = worksheet.Cells[row, 1].Text,
                   CustomerId = worksheet.Cells[row, 2].Text,
                   Category = worksheet.Cells[row, 3].Text,
                   CreationDate = DateTime.Parse(worksheet.Cells[row, 4].Text),
                   // ... more mappings
               };
               
               tickets.Add(ticket);
           }
           
           await context.Tickets.AddRangeAsync(tickets);
           await context.SaveChangesAsync();
           
           // Process with GERDA
           foreach (var ticket in tickets)
           {
               await _gerdaService.ProcessTicketAsync(ticket.Guid);
           }
           
           _logger.LogInformation("Processed {Count} tickets from Qlik export", tickets.Count);
       }
   }
   ```

2. **Add EPPlus NuGet Package** (1 hour)
   ```bash
   dotnet add package EPPlus
   ```

3. **Configure File Watcher in appsettings.json** (1 hour)
   ```json
   {
     "GerdaFileWatcher": {
       "Enabled": true,
       "WatchPath": "/data/qlik_exports",
       "ArchivePath": "/data/processed"
     }
   }
   ```

**Deliverable:** Automated import from Qlik exports ✅

---

#### Week 6: Metrics & KPI Tracking

**Tasks:**

1. **Create GerdaMetrics Table** (2 hours)
   ```csharp
   public class GerdaMetric
   {
       public int Id { get; set; }
       public DateTime Timestamp { get; set; }
       public string MetricName { get; set; }  // SlaBreachRate, AvgPriority, etc.
       public double Value { get; set; }
       public string? Category { get; set; }
       public string? AgentId { get; set; }
   }
   ```

2. **Implement MetricsService** (6 hours)
   ```csharp
   public class GerdaMetricsService
   {
       public async Task RecordMetric(string name, double value, string? category = null);
       public async Task<List<MetricDataPoint>> GetMetricHistory(string name, int days);
       public async Task<Dictionary<string, double>> GetCurrentKPIs();
       
       // Specific KPI calculators
       public async Task<double> CalculateSlaBreachRate();
       public async Task<TimeSpan> CalculateAvgTimeToFirstAction();
       public async Task<double> CalculateAgentWorkloadStdDev();
       public async Task<double> CalculateRecommendationAcceptanceRate();
   }
   ```

3. **Add Metrics Dashboard Tab** (4 hours)
   - Time series charts for KPIs
   - Comparison: Manual vs. GERDA performance
   - Export to CSV for reporting

4. **Integration Tests** (3 hours)
   - Test full GERDA workflow end-to-end
   - Test file watcher import
   - Test metrics calculation

**Deliverable:** Comprehensive KPI tracking + reporting ✅

---

### 🎯 **Sprint 9: Production Hardening (Week 7-8)**

**Goal:** Security, performance, documentation

#### Week 7: Performance & Optimization

**Tasks:**

1. **Add Caching for GERDA Predictions** (4 hours)
   ```csharp
   public class CachedDispatchingService : IDispatchingService
   {
       private readonly IMemoryCache _cache;
       private readonly DispatchingService _inner;
       
       public async Task<string?> GetRecommendedAgentAsync(Guid ticketGuid)
       {
           var cacheKey = $"dispatch:{ticketGuid}";
           
           if (_cache.TryGetValue(cacheKey, out string? cachedAgent))
           {
               return cachedAgent;
           }
           
           var agent = await _inner.GetRecommendedAgentAsync(ticketGuid);
           
           _cache.Set(cacheKey, agent, TimeSpan.FromMinutes(30));
           
           return agent;
       }
   }
   ```

2. **Database Indexing** (2 hours)
   ```csharp
   // In ITProjectDB.OnModelCreating()
   modelBuilder.Entity<Ticket>()
       .HasIndex(t => t.PriorityScore)
       .IsDescending();
   
   modelBuilder.Entity<Ticket>()
       .HasIndex(t => new { t.ResponsibleId, t.TicketStatus });
   ```

3. **Batch Processing Optimization** (3 hours)
   - Use `ProcessAllOpenTicketsAsync()` with parallelization
   - Implement rate limiting for ML predictions

4. **Load Testing** (3 hours)
   - Test with 10,000 tickets
   - Measure GERDA processing time
   - Optimize bottlenecks

**Deliverable:** Production-ready performance ✅

---

#### Week 8: Documentation & Deployment

**Tasks:**

1. **User Documentation** (4 hours)
   - Agent guide: "How to use your AI-powered queue"
   - Manager guide: "Understanding GERDA insights"
   - Admin guide: "Configuring GERDA rules"

2. **Technical Documentation** (4 hours)
   - API documentation for GERDA services
   - Architecture diagrams (update with actual implementation)
   - Deployment guide

3. **Configuration Templates** (2 hours)
   - `masala_config.template.json` with all options
   - `.env.example` for sensitive settings
   - Docker Compose for local development

4. **Deployment to Fly.io** (3 hours)
   - Update Dockerfile with ML.NET dependencies
   - Configure persistent storage for ML models
   - Set up monitoring alerts

5. **Final Demo & Handover** (2 hours)
   - Demo to stakeholders
   - Training session for Key Users
   - Go-live checklist

**Deliverable:** Production deployment + user training ✅

---

## Success Metrics (Track Weekly)

| Week | Key Metric | Target |
|------|------------|--------|
| 2 | GERDA processes new tickets automatically | 100% |
| 4 | Agents see priority scores in UI | 100% |
| 4 | Manager dashboard deployed | ✅ |
| 6 | File watcher imports Qlik exports | ✅ |
| 6 | KPIs tracked in database | 5+ metrics |
| 8 | Recommendation acceptance rate | >80% |
| 8 | Time to first action | <2 days (from 5-7) |
| 8 | SLA breach rate | <8% (from 15%) |

---

## Risk Mitigation

| Risk | Mitigation Strategy |
|------|---------------------|
| **Agents reject AI recommendations** | Human-in-the-loop design; agents can override; show explanation for each recommendation |
| **ML model quality degrades** | Weekly monitoring of prediction accuracy; automated retraining; fallback to rule-based when confidence low |
| **Performance issues with large datasets** | Implement caching; batch processing; database indexing; pagination in UI |
| **Qlik export format changes** | Flexible column mapping in config; validation layer; alerts for parsing errors |
| **Insufficient training data** | Use synthetic data generator (Bogus) for testing; graceful degradation to simpler algorithms |

---

## Resource Requirements

| Role | Allocation | Weeks |
|------|-----------|-------|
| **Backend Developer (You)** | 100% | 1-8 |
| **Frontend Developer** | 50% | 2-4 (UI components) |
| **Data Scientist (optional)** | 20% | 6-7 (model validation) |
| **QA Tester** | 30% | 4-8 (integration testing) |
| **Key Users (Hotel Team)** | 10% | Weekly feedback sessions |

---

## Definition of Done (Per Sprint)

### Sprint 6 (Week 1-2)
- [ ] Employee model has Language, Specializations, MaxCapacityPoints
- [ ] Database migration applied successfully
- [ ] masala_config.json has work_queues with sla_defaults and urgency_multipliers
- [ ] TicketController.Create() calls GERDA automatically
- [ ] Ticket Details view shows EstimatedEffortPoints, PriorityScore, GerdaTags
- [ ] No regression in existing functionality
- [ ] Build passes, no compiler warnings

### Sprint 7 (Week 3-4)
- [ ] DispatchingService uses 4-factor affinity scoring
- [ ] AgentQueue view shows prioritized tickets + recommendations
- [ ] Manager TeamDashboard deployed with workload chart
- [ ] Background service recalculates priorities every 6 hours
- [ ] Unit tests for dispatching logic (>80% coverage)
- [ ] UI is responsive (mobile-friendly)

### Sprint 8 (Week 5-6)
- [ ] File watcher processes .xlsx files automatically
- [ ] GerdaMetrics table tracks 5+ KPIs
- [ ] Metrics dashboard shows trends over time
- [ ] Integration tests pass for full workflow
- [ ] No data corruption in Excel import
- [ ] Processed files archived to separate folder

### Sprint 9 (Week 7-8)
- [ ] Response time <500ms for ticket list view
- [ ] Database queries optimized with indexes
- [ ] Caching reduces ML prediction calls by 60%
- [ ] Load test with 10K tickets passes
- [ ] User documentation complete (3 guides)
- [ ] Deployed to production (Fly.io)
- [ ] Monitoring alerts configured
- [ ] Stakeholder demo completed ✅

---

## Immediate Next Steps (This Week)

1. **Commit current GERDA services** ✅
   ```bash
   git add .
   git commit -m "feat(gerda): implement ML.NET services (R+D+A)"
   git push origin feature/gerda-ai
   ```

2. **Create feature branch for Sprint 6**
   ```bash
   git checkout -b feature/gerda-ui-integration
   ```

3. **Start Week 1 Task 1: Extend Employee Model**
   - Modify `Models/Users.cs`
   - Create migration
   - Update seed data

4. **Schedule Key User feedback session** (Friday Week 2)

---

## Appendix: File Structure (Post-Implementation)

```
IT-Project2526/
├── Controllers/
│   ├── TicketController.cs          [GERDA integration ✅]
│   ├── EmployeeController.cs        [Agent queue view 🆕]
│   └── ManagerController.cs         [Team dashboard 🆕]
├── Services/
│   ├── GERDA/
│   │   ├── Grouping/               [✅ Implemented]
│   │   ├── Estimating/             [✅ Implemented]
│   │   ├── Ranking/                [✅ Implemented]
│   │   ├── Dispatching/            [✅ Enhanced multi-factor]
│   │   ├── Anticipation/           [✅ Implemented]
│   │   └── GerdaService.cs         [✅ Orchestrator]
│   ├── GerdaBackgroundService.cs   [🆕 Auto-recalculation]
│   ├── QlikFileWatcherService.cs   [🆕 Excel import]
│   └── GerdaMetricsService.cs      [🆕 KPI tracking]
├── ViewModels/
│   ├── AgentQueueViewModel.cs      [🆕]
│   ├── TeamDashboardViewModel.cs   [🆕]
│   └── TicketDetailsViewModel.cs   [Enhanced ✅]
├── Views/
│   ├── Ticket/
│   │   └── Details.cshtml          [GERDA insights 🆕]
│   ├── Employee/
│   │   └── MyQueue.cshtml          [🆕 Prioritized queue]
│   └── Manager/
│       ├── TeamDashboard.cshtml    [🆕 Analytics]
│       └── Metrics.cshtml          [🆕 KPI charts]
├── Models/
│   ├── Users.cs                    [Employee extended 🆕]
│   └── GerdaMetric.cs              [🆕 Metrics table]
├── masala_config.json              [Enhanced with work_queues 🆕]
└── docs/
    └── AI Notes/
        ├── GERDA_Specifications.md [✅ Original spec]
        ├── README.md               [✅ Architecture]
        └── IMPLEMENTATION_ROADMAP.md [🆕 This file]
```

---

**Version:** 1.0  
**Last Updated:** December 2, 2024  
**Next Review:** End of Week 2 (Sprint 6 retrospective)
