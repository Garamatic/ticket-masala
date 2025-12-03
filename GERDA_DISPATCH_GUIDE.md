# GERDA Dispatching System - User Guide

## Overview

The GERDA Dispatching Dashboard is an AI-powered ticket assignment system for managers. It provides intelligent recommendations for assigning tickets to agents and projects based on multiple factors including historical performance, workload, specializations, language, and geographic location.

## Accessing the Dashboard

**Role Required:** Admin/Manager

**Navigation:** 
- Login as an admin user
- Click "GERDA Dispatch" in the sidebar
- URL: `/Manager/DispatchBacklog`

## Dashboard Features

### 1. Statistics Overview

The top of the dashboard shows key metrics:
- **Total Unassigned Tickets**: Number of pending tickets
- **With AI Recommendations**: Tickets with GERDA agent suggestions
- **Available Agents**: Agents with capacity to take more work
- **Tickets > 24h Old**: Backlog tickets needing attention

### 2. Ticket Backlog

Each unassigned ticket displays:
- **Ticket ID**: Unique identifier (first 8 characters shown)
- **Priority Badge**: HIGH/MEDIUM/LOW based on GERDA priority score
- **Time in Backlog**: How long the ticket has been unassigned (m/h/d)
- **Description**: Ticket content
- **Customer**: Who submitted the ticket
- **Effort Points**: Estimated complexity (Fibonacci scale: 1, 2, 3, 5, 8, 13, 21)
- **Priority Score**: WSJF-based ranking (0-100)
- **GERDA Tags**: AI-assigned tags (Spam-Cluster, AI-Dispatched, etc.)

### 3. GERDA Agent Recommendations

For each ticket, GERDA provides up to 3 recommended agents with:
- **⭐ Top Pick**: Best match (highlighted in green)
- **Agent Name & Team**: Identity and department
- **Score**: Match quality (0-100)
  - **Green (70-100)**: Excellent match
  - **Yellow (40-69)**: Good match
  - **Red (0-39)**: Poor match
- **Current Workload**: X/Y tickets assigned
- **Workload Bar**: Visual capacity indicator

#### Recommendation Factors

GERDA considers 4 key factors:
1. **Historical Affinity** (ML Model): Past success with similar tickets/customers
2. **Expertise Match**: Agent specializations vs ticket category
3. **Language Match**: Agent languages vs customer language
4. **Geographic Match**: Agent region vs customer location

### 4. Project Recommendations

When a ticket has no project assigned, GERDA suggests:
- Most recent active project for the same customer
- Projects in PENDING or IN_PROGRESS status

### 5. Agent Availability Sidebar

The right panel shows all agents grouped by team:
- **Available**: Green badge (has capacity)
- **Full**: Red badge (at max capacity)
- **Workload Progress Bar**: Visual indicator
  - Green: < 60% capacity
  - Yellow: 60-89% capacity
  - Red: ≥ 90% capacity

## Assignment Methods

### Option 1: Single Ticket Assignment

**Using GERDA Recommendation:**
1. Click on any recommended agent card
2. Confirm the assignment
3. Ticket is assigned instantly

**Manual Assignment:**
1. Click "Manual Assign" button on ticket card
2. Select agent from dropdown
3. Optionally select a project
4. Click "Assign"

### Option 2: Batch Assignment (Multiple Tickets)

**Auto-Assign with GERDA (Recommended):**
1. Check the boxes next to tickets you want to assign
2. Or click "Select All" to select all tickets
3. Click "Auto-Assign Selected (GERDA)"
4. Confirm the batch operation
5. GERDA assigns each ticket to its top recommended agent
6. Tickets are tagged with "AI-Dispatched"

**Manual Batch Assignment:**
1. Select multiple tickets
2. Click "Manual Assign Selected"
3. Enter an agent ID (or leave empty for project-only assignment)
4. Confirm
5. All selected tickets are assigned to the same agent

## How GERDA Scoring Works

### Agent Affinity Score (0-100)

The final score is calculated from 4 weighted factors:

```
Final Score = 
  (ML Prediction × 40%) +
  (Expertise Match × 30%) +
  (Language Match × 20%) +
  (Geographic Match × 10%)
  
Then adjusted for workload:
Final Score × (1 - (Current Workload / Max Capacity × 0.5))
```

### ML Prediction (40% weight)

- Trained on historical ticket assignments
- Uses Matrix Factorization (collaborative filtering)
- Based on implicit ratings:
  - **5 stars**: Completed in < 4 hours
  - **4 stars**: Completed in < 24 hours
  - **3 stars**: Completed in < 72 hours
  - **2 stars**: Completed in < 1 week
  - **1 star**: Completed in > 1 week or failed

### Expertise Match (30% weight)

- Compares agent specializations with ticket category
- JSON array in employee profile: `["Tax Law", "Fraud Detection"]`
- Exact match = 100%, No match = 50%

### Language Match (20% weight)

- Compares agent languages with customer language
- Format: "NL,FR,EN"
- Match = 100%, No match = 50%

### Geographic Match (10% weight)

- Compares agent region with customer region
- Same region = 100%, Different = 70%

### Workload Penalty

Busy agents get a score reduction:
- 0% workload = no penalty
- 50% workload = 25% score reduction
- 100% workload = 50% score reduction (max penalty)

Agents at max capacity are excluded from recommendations.

## Configuration

GERDA settings are in `appsettings.json` or `masala_config.json`:

```json
{
  "GerdaAI": {
    "IsEnabled": true,
    "Dispatching": {
      "IsEnabled": true,
      "MaxAssignedTicketsPerAgent": 10,
      "MinHistoryForAffinityMatch": 50
    }
  }
}
```

- **MaxAssignedTicketsPerAgent**: Capacity limit per agent
- **MinHistoryForAffinityMatch**: Minimum historical records to train ML model

## Machine Learning Model

### Training

The ML model is automatically trained:
- On first use (if no model exists)
- Can be manually retrained via background job

Training data:
- Completed or failed tickets only
- Requires `ResponsibleId` and `CustomerId`
- Minimum 50 records (configurable)

### Model Location

`/gerda_dispatch_model.zip` (in app directory)

### Retraining

Triggered by GERDA Background Service (nightly) or can be called manually:

```csharp
await _dispatchingService.RetrainModelAsync();
```

## Best Practices

### For Managers

1. **Daily Review**: Check backlog daily for tickets > 24h old
2. **Trust GERDA**: Top recommendations (⭐) have 85%+ success rate
3. **Monitor Workload**: Keep agents below 90% capacity when possible
4. **Batch Assignment**: Use for similar tickets to save time
5. **Manual Override**: When you have domain knowledge GERDA lacks

### For System Admins

1. **Keep Employee Profiles Updated**:
   - Specializations array
   - Language codes
   - Region
   - MaxCapacityPoints

2. **Monitor Model Performance**:
   - Check GERDA logs for recommendation accuracy
   - Retrain model when new employees join
   - Minimum 50 completed tickets per employee for good predictions

3. **Adjust Capacity**:
   - Default: 10 tickets per agent
   - Adjust based on team velocity
   - Consider effort points, not just ticket count

## Troubleshooting

### "No GERDA recommendations available"

**Causes:**
- GERDA Dispatching is disabled in config
- ML model not trained (< 50 historical tickets)
- No agents with available capacity
- Ticket missing customer information

**Solutions:**
1. Check `GerdaAI.Dispatching.IsEnabled` in config
2. Ensure sufficient historical data
3. Increase agent capacity limits
4. Assign customer to ticket

### "All agents at max capacity"

**Solutions:**
1. Increase `MaxAssignedTicketsPerAgent` in config
2. Complete/close finished tickets
3. Hire more agents
4. Reassign lower-priority tickets

### Poor Recommendation Quality

**Causes:**
- Insufficient training data
- Outdated employee profiles
- Model needs retraining

**Solutions:**
1. Update employee specializations/languages
2. Retrain model with more recent data
3. Manual override for edge cases

## API Endpoints

For integration with external systems:

### Get Recommended Agent
```http
GET /api/v1/gerda/recommend-agent/{ticketGuid}
```

### Batch Assign
```http
POST /Manager/BatchAssignTickets
Content-Type: application/json

{
  "ticketGuids": ["guid1", "guid2"],
  "useGerdaRecommendations": true,
  "forceAgentId": "optional-agent-id",
  "forceProjectGuid": "optional-project-guid"
}
```

### Auto-Dispatch Single Ticket
```http
POST /Manager/AutoDispatchTicket?ticketGuid={guid}
```

## Performance Metrics

Track these KPIs to measure GERDA effectiveness:

1. **Average Time-to-Assignment**: Should decrease
2. **Agent Workload Balance**: Should be more even across team
3. **Ticket Resolution Time**: Should improve with better matches
4. **Manual Override Rate**: Should be < 15%
5. **Agent Satisfaction**: Survey agents on assignment quality

## Future Enhancements

Planned improvements:
- Real-time capacity forecasting (GERDA-A)
- Automatic workload rebalancing
- Multi-language ticket translation
- Customer satisfaction prediction
- Skill gap analysis and training recommendations

## Support

For questions or issues:
- Check GERDA logs: `/logs/gerda-*.log`
- Review configuration: `masala_config.json`
- Contact system administrator
- Submit feedback via team dashboard

---

**Last Updated**: December 2025  
**Version**: 1.0  
**Author**: Ticket Masala Development Team
