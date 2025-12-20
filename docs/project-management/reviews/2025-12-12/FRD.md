# Functional Requirements Document (FRD)
**Project Name:** Ticket Masala
**Version:** 1.0
**Date:** 2025-12-12

## 1. Introduction
This document outlines the specific functional behaviors of the Ticket Masala system.

## 2. Ticket Management
### 2.1 Creation
*   Users must be able to create tickets via a web interface or API.
*   Tickets must require a title, description, and domain ID.
*   Customers can optionally select a project to associate with the ticket.

### 2.2 Lifecycle
*   Supported Statuses: New, Assigned, InProgress, Completed, ReviewPending, Approved, Rejected, Closed.
*   The system must validate status transitions (e.g., a "New" ticket cannot act directly to "Closed" without resolution).

### 2.3 Assignment
*   **Manual:** Managers can manually assign tickets to agents.
*   **AI-Assisted:** The system can recommend agents based on workload and expertise.
*   **Batch:** Managers can select multiple tickets and assign them to a single agent or project in one action.

## 3. Project Management
*   Projects effectively group tickets.
*   Projects must have a manager, a customer, and a start/end date.
*   System must assist in tracking project progress based on the completion of underlying tickets.

## 4. AI & Automation (GERDA)
*   **Auto-Tagging:** New tickets should be analyzed to generate tags (e.g., "Hardware", "Urgent").
*   **Effort Estimation:** AI must predict "Effort Points" based on description complexity.
*   **Dispatching:** The dispatch engine must rank available agents for a given ticket.

## 5. User Roles & Permissions
*   **Customer:** Create tickets, view own tickets, add comments.
*   **Agent:** View assigned tickets, update status, log work, view team dashboard.
*   **Manager:** View all tickets, assign agents, manage projects, view reports.
*   **Admin:** Full system access.

## 6. Notification System
*   Users must receive email/in-app notifications for:
    *   Ticket Assignment.
    *   Status Changes.
    *   New Comments.
    *   Quality Review requests.

## 7. Search & Filtering
*   Global search bar for finding tickets by ID or keyword.
*   Saved filters for complex queries (e.g., "High priority tickets assigned to me").
