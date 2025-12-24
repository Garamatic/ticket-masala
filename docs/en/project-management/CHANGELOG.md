# Changelog

All notable changes to the Ticket Masala project will be documented in this file.

## [3.1.0] - 2025-12-24
### Added
- **Configuration Versioning**: Immutable snapshots of domain configuration stored in database with SHA256 hashing.
- **Scalable Ingestion (Gatekeeper)**: High-performance ingestion API with Scriban templating and background queuing.
- **Multi-Domain Branding**: Domain-specific UI elements (labels, icons, CSS themes) controlled via configuration.
- **DevOps Optimization**: Migration to .NET 10 Chiseled images for minimal attack surface and faster deployments.
- **Monitoring Stack**: Prometheus `/metrics` export and integrated Grafana dashboard configuration.

## [3.0.0] - 2025-12-21
### Added
- **Customer Portal**: Dedicated self-service portal with strict data isolation per customer.
- **GERDA AI Enhancements**: Capacity forecasting, visual dispatch recommendations, and team performance metrics.
- **Workflow Engine**: Finalized logic for state transitions and rule-based assignment.
- **Production Deployment**: Automated CI/CD pipelines targeting Fly.io.

## [2.1.0] - 2025-12-06

### Added

- **External Ticket Submission API**: New endpoint `POST /api/v1/tickets/external` allows third-party applications to submit tickets.
  - Supports CORS for cross-origin requests.
  - Automatically creates "Customer" accounts for new email addresses.
  - Tags tickets with `External-Request` and source site.
- **Landscaping Demo Integration**: A sample static website (`landscaping-demo`) demonstrating how to integrate with the Ticket Masala API.
  - Features real-time quote submission form.
  - Maps external project types to ticket subjects.
- **Create Project from Ticket Workflow**:
  - New "Create from Ticket" button in the Projects view (`/Projects`).
  - New "Create Project" button in Ticket Details view (for unassigned tickets).
  - Pre-fills project creation form with ticket details (Description, Customer, etc.).
- **CORS Configuration**: Enabled Global CORS policy (ALLOW ALL) in `Program.cs` development environment to support local demos.

### Changed

- **ProjectsController**: Added `CreateFromTicket` action to handle project creation validation and view logic.
- **Ticket Views**: Enhanced `Ticket/Detail.cshtml` to contextually show "Create Project" or "View Project" buttons.
- **Architecture**: Validated Modular Monolith structure with new distinct Integration Layer (API Controllers).
