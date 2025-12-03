# GERDA — TAX Department Configuration

**Domain:** Brussels Hotel Tax Operations  
**Department:** Brussel Fiscaliteit  
**Product:** GERDA Pilot for Hotel Tax Compliance  
**Version:** 1.0  
**Date:** December 2, 2024

---

## Executive Summary

This document defines the **TAX-specific configuration** of GERDA for the Brussels Hotel Tax department. It extends the core GERDA architecture with domain rules, terminology, and workflow patterns unique to tax compliance operations.

**Pilot Scope:** Hotel Tax (later: PrI, TC, LEZ)

---

## 1. Domain Context

### Tax Department Operations

The Brussels Tax department manages compliance for:
- **Hotel Tax:** Tourist accommodations (hotels, Airbnb)
- **PrI (Parking Tax):** Commercial parking operators
- **TC (Terrace Tax):** Outdoor seating permits
- **LEZ (Low Emission Zone):** Environmental compliance fines

### Current Challenges

| Challenge | Hotel Tax Specific Impact |
|-----------|---------------------------|
| **Manual Case Distribution** | 200+ cases/week from Qlik exports manually assigned by coordinators |
| **No Prioritization** | High-value ENRM audits buried under NTOF reactions |
| **Agent Specialization** | Multilingual city: need NL/FR/EN matching; hotel tax expertise lost when agents rotate |
| **Revenue Risk** | SLA breaches on Fines → uncollectible revenue |
| **Seasonal Spikes** | Year-end declaration deadline → 400% volume increase in December |

### The Airbnb Pilot Proof

**Manual Analysis (2023):**  
- Tax coordinators identified **9,000 priority Airbnb cases**

**GERDA Prototype (2024):**  
- Automated analysis found **16,000 cases (+77%)**
- Additional €XXX,XXX in potential revenue recovery

---

## 2. TAX-Specific Terminology

### GERDA → TAX Mapping

| GERDA Generic Term | TAX Domain Term |
|--------------------|-----------------|
| Ticket | **Case** (tax dossier) |
| Customer | **Taxpayer** or **BP (Business Partner)** |
| Category | **Case Type** (NTOF, ENRM, Fine, Declaration) |
| Agent | **Tax Agent** (case handler) |
| Work Queue | **Product Queue** (Hotel Tax, PrI, TC) |
| SLA | **Legal Deadline** (statutory response time) |

### TAX Glossary

| Term | Definition |
|------|------------|
| **BP (Business Partner)** | Taxpayer entity in SAP (e.g., BP12345678) |
| **OC (Contract Object)** | Hotel/Airbnb establishment linked to BP |
| **NTOF** | *Notification de taxation d'office* — Automatic full taxation for missing declaration |
| **ENRM** | *Enrolment correctif* — Taxpayer-requested correction to tax bill |
| **Fine** | Late payment penalty or compliance fine |
| **Declaration** | Periodic tax return (monthly/quarterly) |
| **Rectification** | Administrative correction to tax record |
| **SCASEPS** | SAP inbox for taxpayer correspondence |
| **Yield** | Revenue recovered per unit of agent effort (€/hour) |

---

## 3. Case Categories & SLA Configuration

### Hotel Tax Case Types

| Case Type | Description | SLA (Days) | Urgency Multiplier | Avg Complexity |
|-----------|-------------|------------|-------------------|----------------|
| **NTOF** | Missing declaration → automatic taxation | 30 | 2.0 | M (3 points) |
| **ENRM** | Taxpayer-requested audit correction | 90 | 2.5 | L (8 points) |
| **Fine** | Late payment penalty or compliance fine | 60 | 3.0 | S (1 point) |
| **Declaration** | Monthly/quarterly tax return processing | 45 | 1.5 | M (3 points) |
| **Rectification** | Administrative data correction | 30 | 1.5 | S (1 point) |

### Why These Multipliers?

- **Fine = 3.0:** Revenue collection; time-sensitive (uncollectible after statute of limitations)
- **ENRM = 2.5:** Taxpayer-initiated (reputational risk if delayed); complex audits
- **NTOF = 2.0:** Legal obligation (automatic full taxation must be defended)
- **Declaration/Rectification = 1.5:** Standard workflow, less urgency

---

## 4. TAX-Specific GERDA Configuration

### `gerda_config_tax.json`

```json
{
  "application": {
    "name": "GERDA - Brussels Hotel Tax Pilot",
    "environment": "production",
    "department": "Brussel Fiscaliteit",
    "domain": "Tax Compliance"
  },
  
  "data_sources": {
    "ingestion_mode": "file_watcher",
    "file_watcher_path": "/data/qlik_exports/hotel_tax/",
    "file_pattern": "*.xlsx",
    "archive_path": "/data/processed/",
    "sap_connector": {
      "enabled": false,
      "endpoint": "sap.bf.brussels/api",
      "bapi_functions": ["BAPI_CASE_GETLIST", "BAPI_CASE_UPDATE"],
      "credentials_key": "SAP_CREDENTIALS"
    }
  },
  
  "work_queues": [
    {
      "id": 1,
      "name": "Hotel Tax",
      "description": "Tourist accommodation compliance (hotels, Airbnb, B&B)",
      "categories": ["NTOF", "ENRM", "Fine", "Declaration", "Rectification"],
      "sla_defaults": {
        "NTOF": 30,
        "ENRM": 90,
        "Fine": 60,
        "Declaration": 45,
        "Rectification": 30
      },
      "urgency_multipliers": {
        "NTOF": 2.0,
        "ENRM": 2.5,
        "Fine": 3.0,
        "Declaration": 1.5,
        "Rectification": 1.5
      },
      "active": true
    },
    {
      "id": 2,
      "name": "PrI (Parking Tax)",
      "description": "Commercial parking operator compliance",
      "categories": ["NTOF", "ENRM", "Fine", "Declaration"],
      "active": false,
      "comment": "Phase 2 rollout"
    }
  ],
  
  "gerda_modules": {
    "grouping": {
      "enabled": true,
      "algorithm": "k_means",
      "time_window_days": 7,
      "threshold": 3,
      "comment": "Bundle if same BP submits >3 cases in same category within 7 days"
    },
    
    "estimating": {
      "enabled": true,
      "model_type": "random_forest",
      "model_path": "/models/tax_complexity_classifier.pkl",
      "features": [
        "case_category",
        "taxpayer_history_length",
        "amount_disputed",
        "document_count",
        "is_first_offense"
      ],
      "training_schedule": "monthly",
      "fallback_complexity": {
        "NTOF": 3,
        "ENRM": 8,
        "Fine": 1,
        "Declaration": 3,
        "Rectification": 1
      }
    },
    
    "ranking": {
      "enabled": true,
      "algorithm": "wsjf",
      "comment": "Priority = (Urgency Multiplier / Days Until SLA) / Complexity Points"
    },
    
    "dispatching": {
      "enabled": true,
      "affinity_weights": {
        "past_interaction": 0.4,
        "category_expertise": 0.3,
        "language_match": 0.2,
        "geography": 0.1
      },
      "ml_model": "matrix_factorization",
      "max_assigned_tickets_per_agent": 10,
      "comment": "Preserve agent-taxpayer relationships; match language"
    },
    
    "anticipation": {
      "enabled": true,
      "forecast_horizon_days": 90,
      "model": "prophet",
      "alert_threshold_percentage": 0.3,
      "min_history_days": 90,
      "seasonal_events": [
        {
          "name": "Year-End Declaration Deadline",
          "month": 12,
          "volume_multiplier": 4.0
        },
        {
          "name": "Summer Tourism Spike",
          "months": [7, 8],
          "volume_multiplier": 1.8
        }
      ]
    }
  },
  
  "agents": [
    {
      "id": "AGT001",
      "name": "Marie Dubois",
      "language": "FR",
      "secondary_languages": ["NL", "EN"],
      "specializations": ["NTOF", "Declaration"],
      "max_capacity_points": 40,
      "region": "Brussels-Capital",
      "active": true
    },
    {
      "id": "AGT002",
      "name": "Jan Vermeulen",
      "language": "NL",
      "secondary_languages": ["FR"],
      "specializations": ["ENRM", "Fine"],
      "max_capacity_points": 35,
      "region": "Brussels-Capital",
      "active": true
    },
    {
      "id": "AGT003",
      "name": "Sarah Johnson",
      "language": "EN",
      "secondary_languages": ["FR"],
      "specializations": ["Hotel Tax", "Airbnb"],
      "max_capacity_points": 40,
      "region": "Brussels-Capital",
      "active": true,
      "comment": "Specialist for English-speaking Airbnb hosts"
    }
  ],
  
  "kpi_targets": {
    "sla_breach_rate": 0.05,
    "time_to_first_action_hours": 48,
    "yield_per_agent_hour_euros": 500,
    "recommendation_acceptance_rate": 0.85,
    "workload_balance_stddev": 5.0
  }
}
```

---

## 5. Data Sources & Integration

### Qlik Export Format (Excel)

**File Naming Convention:**  
`Hotel_Tax_Cases_YYYYMMDD_HHMM.xlsx`

**Required Columns:**

| Column | Description | Example |
|--------|-------------|---------|
| `CaseNumber` | SAP case ID | `HT-2024-12345` |
| `BPNumber` | Taxpayer Business Partner ID | `BP12345678` |
| `OCNumber` | Contract Object (hotel/property) | `OC-HOTEL-001` |
| `CaseType` | Category | `NTOF`, `ENRM`, `Fine` |
| `SubmissionDate` | Case creation date | `2024-11-15` |
| `SLADeadline` | Legal deadline | `2024-12-15` |
| `AmountDisputed` | Tax amount in dispute (EUR) | `15000.00` |
| `DocumentCount` | Supporting documents attached | `3` |
| `TaxpayerLanguage` | Primary language | `NL`, `FR`, `EN`, `DE` |
| `Description` | Case details | Free text |

**File Watcher Implementation:**

```csharp
public class QlikFileWatcherService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken ct)
    {
        var watcher = new FileSystemWatcher("/data/qlik_exports/hotel_tax/")
        {
            Filter = "*.xlsx",
            EnableRaisingEvents = true
        };
        
        watcher.Created += async (sender, e) =>
        {
            _logger.LogInformation("New Qlik export detected: {Path}", e.FullPath);
            await ProcessQlikExport(e.FullPath);
            
            // Archive processed file
            var archivePath = Path.Combine("/data/processed/", Path.GetFileName(e.FullPath));
            File.Move(e.FullPath, archivePath);
        };
        
        return Task.CompletedTask;
    }
    
    private async Task ProcessQlikExport(string filePath)
    {
        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets[0];
        
        var tickets = new List<Ticket>();
        
        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            var ticket = new Ticket
            {
                Title = worksheet.Cells[row, 1].Text,  // CaseNumber
                CustomerId = worksheet.Cells[row, 2].Text,  // BPNumber
                Category = worksheet.Cells[row, 4].Text,  // CaseType
                CreationDate = DateTime.Parse(worksheet.Cells[row, 5].Text),
                SlaDeadline = DateTime.Parse(worksheet.Cells[row, 6].Text),
                Description = worksheet.Cells[row, 10].Text
            };
            
            tickets.Add(ticket);
        }
        
        await _context.Tickets.AddRangeAsync(tickets);
        await _context.SaveChangesAsync();
        
        // Process with GERDA
        foreach (var ticket in tickets)
        {
            await _gerdaService.ProcessTicketAsync(ticket.Guid);
        }
        
        _logger.LogInformation("Processed {Count} cases from Qlik export", tickets.Count);
    }
}
```

---

## 6. TAX-Specific Estimating Logic

### Feature Engineering for Complexity Prediction

```python
# Features for Random Forest Classifier

features = {
    "case_category": ticket.Category,  # NTOF, ENRM, Fine, etc.
    "taxpayer_history_length": len(GetPreviousCases(ticket.BPNumber)),
    "amount_disputed_eur": ticket.AmountDisputed,
    "document_count": ticket.DocumentCount,
    "is_first_offense": GetPreviousCases(ticket.BPNumber).count == 0,
    "taxpayer_type": GetTaxpayerType(ticket.BPNumber),  # Hotel, Airbnb, B&B
    "has_legal_representation": CheckLegalRep(ticket),
    "is_cross_border": ticket.TaxpayerLanguage not in ["NL", "FR"]
}

# Training Labels (from historical cycle times)
labels = {
    "S": ticket.ResolutionTime <= 4 hours,
    "M": 4 hours < ticket.ResolutionTime <= 24 hours,
    "L": 24 hours < ticket.ResolutionTime <= 72 hours,
    "XL": ticket.ResolutionTime > 72 hours
}
```

### Complexity → Revenue Yield Correlation

**Hypothesis:** Not all cases have equal revenue impact

| Case Type | Avg Amount (EUR) | Avg Effort (Hours) | Yield (EUR/Hour) |
|-----------|------------------|-------------------|------------------|
| Fine | 500 | 0.5 | 1,000 |
| NTOF | 3,000 | 3 | 1,000 |
| ENRM | 15,000 | 12 | 1,250 |
| Declaration | 5,000 | 4 | 1,250 |

**GERDA Optimization:** Prioritize high-yield cases when capacity is constrained

---

## 7. TAX-Specific Dispatching Rules

### Language Matching

```csharp
private double CalculateLanguageMatchScore(Employee agent, Ticket ticket)
{
    var customerLanguage = GetCustomerLanguage(ticket.CustomerId);
    
    if (agent.Language == customerLanguage)
        return 1.0;  // Perfect match
    
    if (agent.SecondaryLanguages?.Contains(customerLanguage) == true)
        return 0.7;  // Secondary language competence
    
    return 0.3;  // No match, requires translation
}
```

### Taxpayer Relationship Preservation

```csharp
private double CalculatePastInteractionScore(Employee agent, Ticket ticket)
{
    var previousCases = _context.Tickets
        .Where(t => t.CustomerId == ticket.CustomerId)
        .Where(t => t.ResponsibleId == agent.Id)
        .Where(t => t.TicketStatus == Status.Completed)
        .ToList();
    
    if (previousCases.Count == 0)
        return 0.0;
    
    // Bonus for recent interactions (institutional knowledge)
    var recentCount = previousCases
        .Where(t => t.CompletionDate > DateTime.Now.AddMonths(-6))
        .Count();
    
    return Math.Min(1.0, (previousCases.Count * 0.2) + (recentCount * 0.3));
}
```

### Category Expertise

```csharp
private double CalculateCategoryExpertiseScore(Employee agent, Ticket ticket)
{
    var specializations = JsonSerializer
        .Deserialize<List<string>>(agent.Specializations ?? "[]");
    
    if (specializations.Contains(ticket.Category))
        return 1.0;  // Specialist
    
    // Check if agent has successfully handled this category before
    var pastSuccessRate = _context.Tickets
        .Where(t => t.ResponsibleId == agent.Id)
        .Where(t => t.Category == ticket.Category)
        .Where(t => t.TicketStatus == Status.Completed)
        .Average(t => t.ResolutionTime <= t.SlaDeadline ? 1.0 : 0.0);
    
    return pastSuccessRate;
}
```

---

## 8. Seasonal Forecasting Patterns

### Hotel Tax Seasonality

**Peak Periods:**
- **December (Week 48-52):** Year-end declaration deadline → 400% volume spike
- **July-August:** Summer tourism → 180% volume increase
- **May:** Tax audit season → 150% ENRM cases

**Low Periods:**
- **January-February:** Post-holiday slowdown → 60% normal volume
- **April:** Easter break → 70% normal volume

### Prophet Model Configuration

```python
from fbprophet import Prophet

# Load historical case volume data
df = pd.DataFrame({
    'ds': case_dates,  # Date column
    'y': case_counts   # Volume column
})

# Add Belgian holidays (suppress volume)
holidays = pd.DataFrame({
    'holiday': 'Belgian Holiday',
    'ds': pd.to_datetime(['2024-12-25', '2024-01-01', '2024-07-21']),
    'lower_window': 0,
    'upper_window': 1
})

model = Prophet(
    yearly_seasonality=True,
    weekly_seasonality=True,
    holidays=holidays,
    changepoint_prior_scale=0.05
)

model.add_seasonality(
    name='year_end_rush',
    period=365,
    fourier_order=10,
    prior_scale=15
)

model.fit(df)

# Forecast next 90 days
future = model.make_future_dataframe(periods=90)
forecast = model.predict(future)
```

---

## 9. TAX-Specific KPIs

### Financial Metrics

| KPI | Definition | Target |
|-----|------------|--------|
| **Yield per Agent Hour** | Revenue collected / Agent hours spent | €500/hour |
| **High-Value Case Coverage** | % of cases >€10K handled within SLA | >90% |
| **Revenue at Risk** | Total € in cases approaching SLA breach | <€50K |

### Compliance Metrics

| KPI | Definition | Target |
|-----|------------|--------|
| **SLA Breach Rate** | % of cases exceeding legal deadline | <5% |
| **Time to First Action** | Days from case creation to first agent action | <2 days |
| **NTOF Response Rate** | % of NTOF reactions processed on time | >95% |

### Operational Metrics

| KPI | Definition | Target |
|-----|------------|--------|
| **Workload Balance (StdDev)** | Std deviation of agent Fibonacci points | <5 points |
| **Recommendation Acceptance** | % of GERDA assignments accepted by agents | >85% |
| **Forecast Accuracy (MAE)** | Mean absolute error of volume predictions | <15% |

---

## 10. Success Stories & Proof Points

### Airbnb Pilot (2023-2024)

**Context:** Brussels mandates tourist tax for Airbnb rentals; manual compliance checks identified 9,000 priority cases

**GERDA Intervention:**
- Automated clustering detected 16,000 cases (+77%)
- Grouping module bundled repeat offenders (1 taxpayer = 15 separate cases → 1 parent case)
- Ranking prioritized high-revenue properties first
- Forecasting predicted seasonal compliance patterns

**Results:**
- €XXX,XXX additional revenue detected
- Coordinator assignment time: 12 hours/week → 1 hour/week
- SLA breach rate: 18% → 6%

### Expected Pilot Results (Hotel Tax)

| Metric | Baseline (Manual) | Pilot Target | Actual (TBD) |
|--------|-------------------|--------------|--------------|
| Time to First Action | 5-7 days | <2 days | |
| SLA Breach Rate | 15% | <8% | |
| Yield per Agent Hour | €350 | €500 | |
| Coordinator Hours/Week | 8 | <2 | |

---

## 11. Change Management & Adoption

### Key User Personas

1. **Tax Agent (Marie):**
   - Needs: Clear prioritization, context on taxpayer history
   - Concern: "Will AI replace my judgment?"
   - Mitigation: Human-in-the-loop; agents can override GERDA

2. **Team Coordinator (Didier):**
   - Needs: Workload visibility, early bottleneck warnings
   - Concern: "Can I trust the AI recommendations?"
   - Mitigation: Transparency (show affinity scores); weekly review sessions

3. **Director (Katleen):**
   - Needs: KPI dashboard, capacity planning
   - Concern: "ROI justification for IT investment"
   - Mitigation: Quarterly financial reports (yield improvement)

### Training Plan

| Week | Audience | Topic | Format |
|------|----------|-------|--------|
| 1 | Coordinators | GERDA overview, demo | 2-hour workshop |
| 2 | Agents | Agent queue walkthrough | 1-hour hands-on |
| 4 | All | Human-in-the-loop: how to override | 30-min video |
| 8 | Directors | KPI dashboard interpretation | 1-hour presentation |

---

## 12. Deployment Roadmap

### Phase 1: Hotel Tax Pilot (Weeks 1-12)

**Scope:** File watcher → GERDA → Basic UI

| Week | Deliverable |
|------|-------------|
| 1-2 | Qlik file watcher deployed; cases ingested |
| 3-4 | G+E+R modules active; priority queue visible |
| 5-6 | D module (dispatching) with language matching |
| 7-8 | A module (forecasting) with December spike alert |
| 9-10 | Metrics dashboard; Key User validation |
| 11-12 | Go/No-Go decision for Phase 2 |

### Phase 2: SAP API Integration (Weeks 13-24)

**Scope:** Real-time sync, multi-queue (PrI, TC)

- SAP .NET Connector for live case sync
- Extend to Parking Tax (PrI) and Terrace Tax (TC)
- Advanced analytics: yield forecasting by category

### Phase 3: Full Production (Weeks 25+)

**Scope:** Scale to all tax products, mobile access

- LEZ (Low Emission Zone) queue added
- Mobile app for agents
- Automated model retraining pipeline

---

## 13. Risk Mitigation (TAX-Specific)

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Agents reject AI recommendations** | Low adoption | Pilot with champion agents first; show success stories |
| **Data quality issues in Qlik exports** | Inaccurate scoring | Data validation layer; weekly data quality audits |
| **Legal challenge to AI-driven taxation** | Reputational damage | Human-in-the-loop; all final decisions by agents; audit trail |
| **Model bias (e.g., language discrimination)** | Fairness concerns | Fairness metrics (demographic parity); monthly bias audits |
| **Seasonal forecast overfits** | False alarms | Use ensemble (Prophet + SSA); validate on holdout period |

---

## 14. Next Steps (Hotel Tax Pilot)

### This Week
1. ✅ Finalize TAX configuration document
2. ⏳ Obtain sample Qlik export (.xlsx) for Hotel Tax
3. ⏳ Map Excel columns to GERDA Ticket entity
4. ⏳ Set up file watcher directory on server

### Weeks 1-2
1. Deploy file watcher service
2. Seed agent profiles (Marie, Jan, Sarah) with language/specializations
3. Configure `gerda_config_tax.json` with Hotel Tax rules
4. Test end-to-end: Qlik export → GERDA processing → Agent queue

### Weeks 3-4
1. Demo to coordinators (Didier)
2. Collect feedback on priority rankings
3. Tune urgency multipliers based on real case data
4. Weekly review: acceptance rate, SLA compliance

---

**Document Status:** TAX Configuration v1.0  
**Owner:** Juan Benjumea (Product Owner)  
**Stakeholders:** Didier (Coordinator), Katleen (Director), Hotel Tax Team  
**Next Review:** End of Pilot Week 4
