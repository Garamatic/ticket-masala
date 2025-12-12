# Configurable Database Seeding

As of **Task 8 (DbSeeder Refactoring)**, the application uses a data-driven approach to populate the database with initial users, projects, and work items.

## Overview

- **Goal**: Decouple seed data from C# code to allow easier modification by non-developers or for different environments.
- **Configuration File**: `config/seed_data.json`
- **Terminology**: The configuration file uses **Universal Entity Model (UEM)** terminology (`WorkContainer`, `WorkItem`) as per [ADR-001](../ADR-001-uem-terminology.md), while the application maps these to internal `Project` and `Ticket` entities.

## Configuration Structure (`seed_data.json`)

The JSON file is structured as follows:

```json
{
  "Admins": [ ... ],
  "Employees": [ ... ],
  "Customers": [ ... ],
  "WorkContainers": [
    {
      "Name": "Project Name",
      "WorkItems": [
        {
          "Description": "Task Description",
          "Type": "Subtask"
        }
      ]
    }
  ],
  "UnassignedWorkItems": [ ... ]
}
```

## Loading Logic

The `DbSeeder` looks for the `seed_data.json` file in the following order of precedence:

1. **Runtime Root**: `ContentRootPath/seed_data.json` (Production/Docker scenarios).
2. **Config Directory**: `../../config/seed_data.json` (Development environment, relative to `src/TicketMasala.Web`).
3. **Data Directory**: `ContentRootPath/Data/seed_data.json` (Legacy/Fallback).

## Modifying Seed Data

To add new users or projects::

1. Edit `config/seed_data.json`.
2. Ensure valid JSON structure.
3. Restart the application. If the database is empty, the new data will be seeded.
4. **Note**: If the database already has users, the seeder will **skip** data creation to preserve existing state. You must drop the database to re-seed.
