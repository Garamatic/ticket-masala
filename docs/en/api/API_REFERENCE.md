# Ticket Masala API Reference

Complete REST API documentation for Ticket Masala.

## Base URL

```
Development: http://localhost:5054/api/v1
Production:  https://your-domain.fly.dev/api/v1
```

## Authentication

Most endpoints require authentication via ASP.NET Identity cookies or Bearer tokens.

```bash
# Cookie-based (browser)
POST /Identity/Account/Login

# For API clients, use session cookies or implement token-based auth
```

**Roles:**
- `Customer` - Can create and view own tickets
- `Employee` - Can manage tickets, projects
- `Admin` - Full system access

---

## API Response Format

All API responses follow a standard wrapper format:

```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully",
  "errors": [],
  "timestamp": "2025-12-21T17:00:00Z"
}
```

**Error Response:**
```json
{
  "success": false,
  "data": null,
  "message": null,
  "errors": ["Error description"],
  "timestamp": "2025-12-21T17:00:00Z"
}
```

---

## Work Items (Tickets)

The Universal Entity Model (UEM) uses "WorkItem" for tickets. Both `/tickets` and `/work-items` routes are supported.

### List All Work Items

```http
GET /api/v1/work-items
Authorization: Required
```

**Response:** `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Network connectivity issue",
    "description": "Cannot connect to VPN",
    "domainId": "IT",
    "typeCode": "INCIDENT",
    "status": "New",
    "priorityScore": 85.5,
    "estimatedEffortPoints": 5,
    "customerId": "user-123",
    "customerName": "Alice Customer",
    "assignedHandlerId": "agent-456",
    "assignedHandlerName": "Bob Support",
    "containerId": "project-guid",
    "containerName": "Q4 Support",
    "createdAt": "2025-12-20T10:00:00Z",
    "completionTarget": "2025-12-27T10:00:00Z"
  }
]
```

---

### Get Work Item by ID

```http
GET /api/v1/work-items/{id}
Authorization: Required
```

**Parameters:**
| Name | Type | Description |
|------|------|-------------|
| `id` | GUID | Work item identifier |

**Response:** `200 OK` - WorkItem object  
**Response:** `404 Not Found` - If work item doesn't exist

---

### Create Work Item

```http
POST /api/v1/work-items
Authorization: Required
Content-Type: application/json
```

**Request Body:**
```json
{
  "title": "New printer needed",
  "description": "Request for new printer in office 3A",
  "domainId": "IT",
  "typeCode": "SERVICE_REQUEST",
  "customerId": "user-123",
  "completionTarget": "2025-12-30T17:00:00Z"
}
```

**Response:** `201 Created`
```json
{
  "id": "new-guid",
  "title": "New printer needed",
  ...
}
```

---

### Update Work Item

```http
PUT /api/v1/work-items/{id}
Authorization: Required
Content-Type: application/json
```

**Request Body:** Full WorkItem object with matching `id`

**Response:** `204 No Content`  
**Response:** `404 Not Found`

---

### Delete Work Item

```http
DELETE /api/v1/work-items/{id}
Authorization: Required
```

**Response:** `204 No Content`  
**Response:** `404 Not Found`

---

### External Ticket Submission

Submit tickets from external websites without authentication.

```http
POST /api/v1/tickets/external
Content-Type: application/json
```

**Request Body:**
```json
{
  "customerEmail": "customer@example.com",
  "customerName": "John Doe",
  "subject": "Help with order #12345",
  "description": "I need assistance with my recent order.",
  "sourceSite": "partner-company.com"
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "ticketId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "referenceNumber": "3FA85F64",
  "message": "Your request has been submitted successfully"
}
```

> [!NOTE]
> This endpoint creates a new customer account if the email doesn't exist.

---

## Projects (Work Containers)

### List All Projects

```http
GET /api/v1/projects
Authorization: Required (Employee, Admin)
```

**Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "guid": "project-guid",
      "name": "Website Redesign",
      "description": "Complete redesign of company website",
      "status": "InProgress",
      "customerName": "Client Corp",
      "projectManagerName": "Mike PM",
      "totalTickets": 15,
      "completedTickets": 8
    }
  ],
  "message": "Retrieved 5 projects"
}
```

---

### Get Project by ID

```http
GET /api/v1/projects/{id}
Authorization: Required
```

**Response:** `200 OK` - Full project details with tickets  
**Response:** `404 Not Found`

---

### Get Projects by Customer

```http
GET /api/v1/projects/customer/{customerId}
Authorization: Required
```

---

### Search Projects

```http
GET /api/v1/projects/search?query=website
Authorization: Required
```

**Query Parameters:**
| Name | Type | Description |
|------|------|-------------|
| `query` | string | Search term for project name |

---

### Create Project

```http
POST /api/v1/projects
Authorization: Required (Employee, Admin)
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "New Marketing Campaign",
  "description": "Q1 2026 marketing initiative",
  "customerId": "customer-guid",
  "completionTargetMonths": 3
}
```

**Response:** `201 Created`
```json
{
  "success": true,
  "data": "new-project-guid",
  "message": "Project created successfully"
}
```

---

### Update Project Status

```http
PATCH /api/v1/projects/{id}/status
Authorization: Required
Content-Type: application/json
```

**Request Body:**
```json
"InProgress"
```

**Valid Status Values:** `New`, `InProgress`, `OnHold`, `Completed`, `Cancelled`

---

### Assign Project Manager

```http
PATCH /api/v1/projects/{id}/assign-manager
Authorization: Required
Content-Type: application/json
```

**Request Body:**
```json
"manager-user-id"
```

---

### Generate AI Roadmap

```http
POST /api/v1/projects/generate-roadmap
Authorization: Required
Content-Type: application/json
```

**Request Body:**
```json
"Build a customer portal with login, dashboard, and reporting features"
```

**Response:** `200 OK`
```json
{
  "success": true,
  "data": "1. Design authentication flow\n2. Create dashboard wireframes\n3. ...",
  "message": "Roadmap generated successfully"
}
```

---

### Delete Project

```http
DELETE /api/v1/projects/{id}
Authorization: Required (Admin only)
```

**Response:** `200 OK`

---

## Project Statistics

```http
GET /api/v1/projects/statistics/{customerId}
Authorization: Required
```

**Response:** `200 OK`
```json
{
  "success": true,
  "data": {
    "totalProjects": 10,
    "activeProjects": 4,
    "completedProjects": 5,
    "pendingProjects": 1,
    "totalTasks": 150,
    "completedTasks": 95
  }
}
```

---

## Work Containers (V1)

Alternative UEM terminology endpoint for projects.

```http
GET /api/v1/work-containers
POST /api/v1/work-containers
GET /api/v1/work-containers/{id}
PUT /api/v1/work-containers/{id}
DELETE /api/v1/work-containers/{id}
```

Same functionality as `/projects` endpoints.

---

## Error Codes

| HTTP Code | Description |
|-----------|-------------|
| `200` | Success |
| `201` | Created |
| `204` | No Content (successful update/delete) |
| `400` | Bad Request - Invalid input |
| `401` | Unauthorized - Authentication required |
| `403` | Forbidden - Insufficient permissions |
| `404` | Not Found - Resource doesn't exist |
| `500` | Internal Server Error |

---

## Rate Limiting

Currently no rate limiting is enforced. For production deployments, consider implementing rate limiting via:
- Reverse proxy (nginx, Caddy)
- ASP.NET Core Rate Limiting middleware

---

## Swagger/OpenAPI

Interactive API documentation is available at:

```
http://localhost:5054/swagger
```

The Swagger UI provides:
- Endpoint exploration
- Request/response schemas
- Try-it-out functionality
- Authentication support

---

## Further Reading

- [Configuration Guide](../guides/CONFIGURATION.md) - API and domain configuration
- [Architecture Overview](../architecture/SUMMARY.md) - System design
- [Development Guide](../guides/DEVELOPMENT.md) - Local development setup
