# Wito Retest - Items Requiring User Testing & Clarification

This document contains feedback items that need user testing to verify fixes, or require clarification before implementation.

## Items Requiring User Testing

### Localization
- [ ] **Landing page text switches language** - Fixed parameterized keys in resource files. Please test by switching between EN/FR/NL and verify dashboard stats subtext translates.

### Role-Based Access Control
- [ ] **Customer edit restrictions work** - Hidden Status/CompletionTarget/Responsible fields from customers in ticket edit view. Test as customer role.

### Data Display
- [ ] **Customer no longer shows as "Unknown"** - Fixed Customer/CustomerId assignment in CreateTicketAsync. Test by creating a new ticket and viewing details.

---

## Items Needing Clarification

### Public Toggle on Attachments
- **Issue**: There's a "Public" toggle on attachments but its purpose is unclear.
- **Question**: Should this be removed, or does it serve a specific function (e.g., public vs internal-only attachments)?
- **Location**: Likely in attachment upload form

### Recent Activity Section
- **Status**: Implemented.
- **Action**: Please verify that the "Recent Activity" section on the Home Dashboard now shows real tickets and updates correctly.

### Back Button Navigation
- **Issue**: Clicking "Cancel" on an Edit page returns to the Edit page instead of the Index.
- **Investigation**: Likely browser history behavior vs explicit redirect.
- **Action**: Please retest. If issue persists, we will enforce explicit redirects.

### Customer Responsible Dropdown
- **Issue**: Responsible dropdown for customer shows only themselves.
- **Question**: Is this intended? Should customers be able to assign tickets to others (e.g. colleagues)?

### Project Linkage
- **Issue**: Projects are not linked from Customer details view.
- **Question**: Should we add a "Projects" tab/list to the Customer Details page?

### Critical: Tickets Not Saving/Visible
- **Issue**: Reports of tickets not saving or Admin not seeing tickets.
- **Action**: Please test with the latest build. "Customer Unknown" issue is fixed, which might have caused save failures. Verify Admin view filters.

---

### Bulk Actions & Saved Filters
- **Issue**: User reports bulk actions not working and inability to remove saved filters.
- **Investigation**: Code looks correct (DeleteFilter action exists).
- **Possible Cause**: Javascript error or browser caching issue preventing form submission.
- **Action**: Please test again after clear cache. If fails, provide browser console errors.

### Project Type vs Template Mismatch
- **Issue**: "New Project" dropdown has renovation types but template dropdown has different items.
- **Cause**: Content mismatch between code (enums) and database (template records).
- **Question**: Should we update the enum to match templates, or seed new templates?

### Tickets Without Project
- **Issue**: Tickets can currently exist without a project.
- **Question**: Is this valid? Should we enforce project assignment?
- **Current State**: System allows it (Standalone tickets).

### Standalone Ticket Identification
- **Issue**: No way to visually distinguish standalone tickets from project tickets in the list.
- **Question**: How should they be marked significantly? (e.g. icon, color, separate text?)

---

## Testing Checklist for User

After deploying the latest changes, please verify:

1. **Language Switching**: Change language and verify all dashboard text changes
2. **Customer View**: Log in as customer and verify:
   - No bulk actions visible in ticket list
   - No customer filter visible
   - No "Responsible" column in ticket list
   - Only description field editable in ticket edit
3. **New Ticket Creation**: Create a ticket and verify customer name displays correctly
4. **Project Management**: Verify Edit/Delete buttons appear on project details (Admin/Employee only)
5. **Project Managers**: Verify "Project Manager" dropdowns now only show users with PM role.
