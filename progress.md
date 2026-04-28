# Progress

## Status
In Progress

## Tasks

### Issue #10: Deepen Domain Layer - Rich Domain Model (RFC)

#### Phase 1: Domain Events Infrastructure ✅ COMPLETE
- [x] Create `IDomainEvent` interface
- [x] Create `IHasDomainEvents` interface for aggregate roots
- [x] Create `IAggregateRoot` marker interface
- [x] Update `BaseModel` to support new domain event system (backward compatible)
- [x] Add domain event raise methods to `Ticket` entity
- [x] Create domain events: `TicketCreatedEvent`, `TicketAssignedEvent`, `TicketStatusChangedEvent`, `TicketUpdatedEvent`
- [x] Create `DomainException` for domain invariant violations
- [x] Create `IDomainEventHandler<T>` interface
- [x] Create `DomainEventDispatcher` with DI-based handler resolution
- [x] Create `DomainEventDispatchingInterceptor` for EF Core
- [x] Register domain events infrastructure in DI container
- [x] Create sample handlers (`TicketCreatedNotificationHandler`, `TicketAssignedLogHandler`)
- [x] All 311 tests passing

#### Phase 2: Property Encapsulation ✅ COMPLETE
- [x] Add `LastModified` property to `BaseModel`
- [x] Add factory methods: `Ticket.Create()`, `Ticket.CreateFromPortal()`
- [x] Add update methods with validation: `UpdateDescription()`, `UpdateTitle()`, `AssignTo()`, `Unassign()`
- [x] Add state machine: `TransitionTo()` with `IsValidTransition()` validation
- [x] Add query methods: `CanEditInCurrentState()`, `CanBeEditedBy()`, `CanBeAssigned()`, `IsOverdue()`
- [x] Add setter methods for domain services: `SetPriorityScore()`, `SetContentHash()`, `SetAiSummary()`, etc.
- [x] Add helper methods: `AddGerdaTag()`, `AddComment()`, `AddSubTicket()`, `SyncStatus()`
- [x] Update seeding to use factory methods
- [x] Update `PortalsApiController` to use `CreateFromPortal()`

#### Phase 3: Move Validation to Domain ✅ COMPLETE
- [x] Move `DomainRuleException` to Domain layer
- [x] Add `ValidateCanEdit()` - combines role and state authorization
- [x] Add `ValidateCanChangeStatus()` - checks role and transition validity
- [x] Add `ValidateCanAssign()` - checks role and assignable state
- [x] Add `ValidateCanView()` / `CanBeViewedBy()` - view authorization
- [x] Add `ValidateRequiredFieldsForCurrentState()` - field validation
- [x] Add `GetStateSummary()` - debugging/logging helper
- [x] Update `TicketOrchestrator.UpdateTicketAsync()` to use domain validation
- [x] Simplify `TicketWorkflowService.UpdateTicketAsync()` - validation moved to orchestrator
- [x] Update `GlobalExceptionHandler` to use Domain namespace

#### Phase 4: Domain Services (Pending)
- [ ] Create domain services for cross-aggregate operations
- [ ] Migrate complex logic from orchestrators

## Files Changed
- 18 new/modified files for Phase 1

## Notes
- Issue #11 closed as duplicate of #10
- Non-breaking changes - backward compatible with existing code
- EF Core interceptor automatically dispatches events after SaveChanges
