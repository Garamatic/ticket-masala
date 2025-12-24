# Blueprint: Data Seeding & Simulation

This guide explains how to populate Ticket Masala with initial data, synthetic customers, and mock tickets for development and testing.

---

## The Seeding Doctrine

Ticket Masala uses a **JSON-based Seeding Engine** to ensure that all developers start with a consistent environment.

- **Role-Awareness:** Seeds Admins, Employees, and Customers with their respective identity roles.
- **Domain-Awareness:** Links seeded items to valid `DomainIds` defined in the YAML configuration.
- **Repeatable State:** Seeding is idempotent—it only runs if the database is detected as empty at startup.

---

## Seed Structure (`seed_data.json`)

```json
{
  "Admins": [
    { "Email": "admin@ticketmasala.com", "Name": "System Admin" }
  ],
  "Employees": [
    { "Email": "agent1@ticketmasala.com", "Roles": ["Admin", "Support"] }
  ],
  "WorkContainers": [
    { "Name": "Global IT Support", "DomainId": "IT" }
  ]
}
```

---

## Synthetic Data Generation (Bogus)

For large-scale testing (1,000+ items), we use the **Bogus** library during the seeding phase.

- **Realistic Distributions:** 80% Routine, 15% Normal, 5% Urgent.
- **Domain Specialization:** Generates "Tax Audit" descriptions for the Tax domain and "Irrigation Issue" for Gardening.
- **Historical Back-Dating:** Creates data with timestamps across the last 12 months to test analytics and trends.

---

## Execution

1. **Manual Trigger:** Delete `masala.db` and restart the application. The `DbSeeder` will detect the missing DB and re-populate from JSON.
2. **Environment Override:** You can point to a special "load-test" seeding file via `MASALA_SEED_PATH`.

---

## Success Criteria

1. **Zero Drift:** All developers see the same initial state.
2. **Role Coverage:** Every security role has at least one functional test user.
3. **Speed:** Seed 1,000 items in <2 seconds.

---

## References
- **[Development Blueprint](development.md)**
- **[System Overview](../SYSTEM_OVERVIEW.md)**
