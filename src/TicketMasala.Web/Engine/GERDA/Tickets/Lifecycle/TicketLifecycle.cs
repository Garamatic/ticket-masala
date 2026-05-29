using System.Text.Json;
using System.Text.Json.Serialization;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Exceptions;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.Security;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

/// <summary>
/// Deep module for ticket lifecycle operations. All commands flow through:
/// load → mutate → queue persistence/audit/outbox → commit → notify observers.
/// Outbox messages are queued atomically within the same DbContext transaction.
/// </summary>
internal sealed class TicketLifecycle : ITicketLifecycle
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly IEnumerable<ITicketObserver> _ticketObservers;
    private readonly IEnumerable<ICommentObserver> _commentObservers;
    private readonly IPiiScrubberService _piiScrubber;
    private readonly ISystemClock _clock;
    private readonly ILogger<TicketLifecycle> _logger;

    private static readonly JsonSerializerOptions OutboxJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public TicketLifecycle(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IAuditService auditService,
        IEnumerable<ITicketObserver> ticketObservers,
        IEnumerable<ICommentObserver> commentObservers,
        IPiiScrubberService piiScrubber,
        ISystemClock clock,
        ILogger<TicketLifecycle> logger)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(ticketObservers);
        ArgumentNullException.ThrowIfNull(commentObservers);
        ArgumentNullException.ThrowIfNull(piiScrubber);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _auditService = auditService;
        _ticketObservers = ticketObservers;
        _commentObservers = commentObservers;
        _piiScrubber = piiScrubber;
        _clock = clock;
        _logger = logger;
    }

    public async Task<TicketResult> ExecuteAsync(
        ITicketCommand command,
        TicketContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return command switch
            {
                CreateTicketCommand c => await HandleCreateAsync(c, context, cancellationToken),
                ResolveTicketCommand c => await HandleResolveAsync(c, context, cancellationToken),
                AddCommentCommand c => await HandleAddCommentAsync(c, context, cancellationToken),
                LogTimeCommand c => await HandleLogTimeAsync(c, context, cancellationToken),
                UpdateTicketCommand c => await HandleUpdateAsync(c, context, cancellationToken),
                AssignTicketCommand c => await HandleAssignAsync(c, context, cancellationToken),
                RequestReviewCommand c => await HandleRequestReviewAsync(c, context, cancellationToken),
                SubmitReviewCommand c => await HandleSubmitReviewAsync(c, context, cancellationToken),
                TransitionStatusCommand c => await HandleTransitionStatusAsync(c, context, cancellationToken),
                BatchAssignCommand c => await HandleBatchAssignAsync(c, context, cancellationToken),
                _ => TicketResult.Fail($"Unknown command type: {command.GetType().Name}")
            };
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for {CommandType}", command.GetType().Name);
            return TicketResult.Fail(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing {CommandType}", command.GetType().Name);
            return TicketResult.Fail($"Internal error: {ex.Message}");
        }
    }

    // Command handlers

    private async Task<TicketResult> HandleCreateAsync(
        CreateTicketCommand cmd,
        TicketContext ctx,
        CancellationToken ct)
    {
        var description = _piiScrubber.Scrub(cmd.Description);
        var customer = await _userRepository.GetCustomerByIdAsync(cmd.CustomerId);
        if (customer == null)
            return TicketResult.Fail("Customer not found");

        Employee? responsible = null;
        if (!string.IsNullOrWhiteSpace(cmd.ResponsibleId))
            responsible = await _userRepository.GetEmployeeByIdAsync(cmd.ResponsibleId);

        var ticket = Ticket.CreateFromPortal(
            description,
            cmd.CustomerId,
            completionTarget: cmd.CompletionTarget ?? _clock.UtcNow.AddDays(14));
        if (responsible != null)
        {
            ticket.Responsible = responsible;
            ticket.ResponsibleId = responsible.Id;
            ticket.TicketStatus = Status.Assigned;
            ticket.SyncStatus();
        }

        await _unitOfWork.Tickets.AddAsync(ticket);

        if (responsible != null)
        {
            await QueueAssignedEventAsync(ticket, responsible, ctx.UserId, ct);
        }

        if (cmd.ProjectGuid.HasValue && cmd.ProjectGuid.Value != Guid.Empty)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(cmd.ProjectGuid.Value, includeRelations: true);
            if (project != null)
            {
                project.Tasks.Add(ticket);
                await _unitOfWork.Projects.UpdateAsync(project);
            }
        }

        await _auditService.LogActionAsync(ticket.Guid, "Created", ctx.UserId);
        await QueueCreatedEventAsync(ticket, ct);
        await _unitOfWork.CommitAsync(ct);
        await NotifyTicketObserversAsync(ticket, responsible, ct);

        return TicketResult.Ok(ticket);
    }

    private async Task<TicketResult> HandleResolveAsync(
        ResolveTicketCommand cmd,
        TicketContext ctx,
        CancellationToken ct)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(cmd.TicketGuid, includeRelations: true);
        if (ticket == null)
            return TicketResult.Fail("Ticket not found");

        ticket.Resolve(cmd.ResolutionNotes, cmd.BillableAmount, ctx.UserId);

        await _unitOfWork.Tickets.UpdateAsync(ticket);
        await _auditService.LogActionAsync(ticket.Guid, "Resolved", ctx.UserId,
            newValue: $"Amount: {cmd.BillableAmount}, Notes: {cmd.ResolutionNotes}");
        await QueueResolvedEventAsync(ticket, cmd.ResolutionNotes, ct);
        await _unitOfWork.CommitAsync(ct);
        await NotifyTicketObserversAsync(ticket, ct: ct);

        return TicketResult.Ok(ticket);
    }

    private async Task<TicketResult> HandleAddCommentAsync(
        AddCommentCommand cmd,
        TicketContext ctx,
        CancellationToken ct)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(cmd.TicketGuid, includeRelations: true);
        if (ticket == null)
            return TicketResult.Fail("Ticket not found");

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = cmd.TicketGuid,
            Body = cmd.Body,
            IsInternal = cmd.IsInternal,
            CreatedAt = _clock.UtcNow,
            AuthorId = ctx.UserId,
            Ticket = ticket
        };

        ticket.AddComment(comment);
        await _unitOfWork.AddCommentAsync(comment);
        await _auditService.LogActionAsync(ticket.Guid, "Commented", ctx.UserId,
            newValue: cmd.IsInternal ? "Internal Note" : "Public Reply");
        await _unitOfWork.CommitAsync(ct);
        await NotifyCommentObserversAsync(ticket, comment, ct);

        return TicketResult.Ok(comment);
    }

    private async Task<TicketResult> HandleLogTimeAsync(
        LogTimeCommand cmd,
        TicketContext ctx,
        CancellationToken ct)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(cmd.TicketGuid, includeRelations: false);
        if (ticket == null)
            return TicketResult.Fail("Ticket not found");

        if (cmd.Hours <= 0)
            return TicketResult.Fail("Hours must be greater than zero");
        if (cmd.Hours > 24)
            return TicketResult.Fail("Hours cannot exceed 24 in a single entry");

        var timeLog = new TimeLog
        {
            TicketId = cmd.TicketGuid,
            UserId = ctx.UserId,
            Hours = cmd.Hours,
            Date = cmd.Date,
            Description = cmd.Description,
            CreationDate = _clock.UtcNow
        };

        await _unitOfWork.AddTimeLogAsync(timeLog);
        await _auditService.LogActionAsync(ticket.Guid, "TimeLogged", ctx.UserId, newValue: $"{cmd.Hours} hours");
        await _unitOfWork.CommitAsync(ct);

        return TicketResult.Ok(timeLog);
    }

    private async Task<TicketResult> HandleUpdateAsync(
        UpdateTicketCommand cmd,
        TicketContext ctx,
        CancellationToken ct)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(cmd.TicketGuid, includeRelations: true);
        if (ticket == null)
            return TicketResult.Fail("Ticket not found");

        var description = _piiScrubber.Scrub(cmd.Description);
        ticket.UpdateDescription(description, ctx.UserId);

        if (cmd.NewStatus.HasValue)
            ticket.TransitionTo(cmd.NewStatus.Value, ctx.UserId);

        if (!string.IsNullOrWhiteSpace(cmd.ResponsibleId))
            ticket.AssignTo(cmd.ResponsibleId, ctx.UserId);

        if (cmd.ProjectGuid.HasValue)
            ticket.ProjectGuid = cmd.ProjectGuid.Value;

        await _unitOfWork.Tickets.UpdateAsync(ticket);
        await _auditService.LogActionAsync(ticket.Guid, "Updated", ctx.UserId);
        await _unitOfWork.CommitAsync(ct);
        await NotifyTicketObserversAsync(ticket, ct: ct);

        return TicketResult.Ok(ticket);
    }

    private async Task<TicketResult> HandleAssignAsync(
        AssignTicketCommand cmd,
        TicketContext ctx,
        CancellationToken ct)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(cmd.TicketGuid, includeRelations: true);
        if (ticket == null)
            return TicketResult.Fail("Ticket not found");

        Employee? assigned = null;
        if (!string.IsNullOrWhiteSpace(cmd.AgentId))
        {
            assigned = await _userRepository.GetEmployeeByIdAsync(cmd.AgentId);
            if (assigned != null)
                ticket.AssignTo(cmd.AgentId, ctx.UserId);
        }

        if (cmd.ProjectGuid.HasValue)
            ticket.ProjectGuid = cmd.ProjectGuid.Value;

        await _unitOfWork.Tickets.UpdateAsync(ticket);
        await _auditService.LogActionAsync(ticket.Guid, "Assigned", ctx.UserId,
            newValue: $"Agent: {cmd.AgentId}, Project: {cmd.ProjectGuid}");
        if (assigned != null)
        {
            await QueueAssignedEventAsync(ticket, assigned, ctx.UserId, ct);
        }
        await _unitOfWork.CommitAsync(ct);
        await NotifyTicketObserversAsync(ticket, assigned, ct);

        return TicketResult.Ok(ticket);
    }

    private async Task<TicketResult> HandleRequestReviewAsync(
        RequestReviewCommand cmd,
        TicketContext ctx,
        CancellationToken ct)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(cmd.TicketGuid, includeRelations: false);
        if (ticket == null)
            return TicketResult.Fail("Ticket not found");

        ticket.SetReviewStatus(ReviewStatus.Pending);

        await _unitOfWork.Tickets.UpdateAsync(ticket);
        await _auditService.LogActionAsync(ticket.Guid, "ReviewRequested", ctx.UserId);
        await _unitOfWork.CommitAsync(ct);

        return TicketResult.Ok(ticket);
    }

    private async Task<TicketResult> HandleSubmitReviewAsync(
        SubmitReviewCommand cmd,
        TicketContext ctx,
        CancellationToken ct)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(cmd.TicketGuid, includeRelations: false);
        if (ticket == null)
            return TicketResult.Fail("Ticket not found");

        ticket.SetReviewStatus(cmd.Approved ? ReviewStatus.Approved : ReviewStatus.Rejected);

        var review = new QualityReview
        {
            Id = Guid.NewGuid(),
            TicketId = cmd.TicketGuid,
            ReviewerId = ctx.UserId,
            Score = cmd.Score,
            Comments = cmd.Feedback,
            CreatedAt = _clock.UtcNow,
            IsApproved = cmd.Approved
        };

        await _unitOfWork.AddQualityReviewAsync(review);
        await _unitOfWork.Tickets.UpdateAsync(ticket);
        await _auditService.LogActionAsync(ticket.Guid, cmd.Approved ? "ReviewApproved" : "ReviewRejected", ctx.UserId,
            propertyName: "QualityReview", newValue: cmd.Feedback);
        await _unitOfWork.CommitAsync(ct);

        return TicketResult.Ok(ticket);
    }

    private async Task<TicketResult> HandleTransitionStatusAsync(
        TransitionStatusCommand cmd,
        TicketContext ctx,
        CancellationToken ct)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(cmd.TicketGuid, includeRelations: false);
        if (ticket == null)
            return TicketResult.Fail("Ticket not found");

        ticket.TransitionTo(cmd.NewStatus, ctx.UserId);

        await _unitOfWork.Tickets.UpdateAsync(ticket);
        await _auditService.LogActionAsync(ticket.Guid, "StatusChanged", ctx.UserId,
            propertyName: "TicketStatus", newValue: cmd.NewStatus.ToString());
        await _unitOfWork.CommitAsync(ct);
        await NotifyTicketObserversAsync(ticket, null, ct);

        return TicketResult.Ok(ticket);
    }

    private async Task<TicketResult> HandleBatchAssignAsync(
        BatchAssignCommand cmd,
        TicketContext ctx,
        CancellationToken ct)
    {
        // TODO: Each HandleAssignAsync commits independently. For true batch atomicity,
        // extract the core assignment logic into a non-committing helper and commit once here.
        var failures = new List<string>();
        int successCount = 0;

        foreach (var ticketGuid in cmd.TicketGuids)
        {
            var result = await HandleAssignAsync(
                new AssignTicketCommand(ticketGuid, cmd.AgentId, cmd.ProjectGuid),
                ctx, ct);

            if (result.Success)
                successCount++;
            else
                failures.Add($"{ticketGuid}: {result.ErrorMessage}");
        }

        if (successCount == 0)
            return TicketResult.Fail($"All {cmd.TicketGuids.Count} assignments failed: {string.Join("; ", failures)}");

        return new TicketResult
        {
            Success = true,
            Warnings = failures.Count > 0
                ? [$"{successCount}/{cmd.TicketGuids.Count} succeeded. Failures: {string.Join("; ", failures)}"]
                : []
        };
    }

    // Shared choreography

    private async Task NotifyTicketObserversAsync(Ticket ticket, Employee? assignee = null, CancellationToken ct = default)
    {
        foreach (var observer in _ticketObservers)
        {
            try
            {
                await observer.OnTicketUpdatedAsync(ticket);

                if (ticket.TicketStatus == Status.Completed)
                    await observer.OnTicketCompletedAsync(ticket);

                if (assignee != null)
                    await observer.OnTicketAssignedAsync(ticket, assignee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed", observer.GetType().Name);
            }
        }
    }

    private async Task NotifyCommentObserversAsync(Ticket ticket, TicketComment comment, CancellationToken ct = default)
    {
        foreach (var observer in _ticketObservers)
        {
            try
            {
                await observer.OnTicketCommentedAsync(comment);
                await observer.OnTicketUpdatedAsync(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TicketObserver {ObserverType} failed on comment", observer.GetType().Name);
            }
        }

        foreach (var observer in _commentObservers)
        {
            try
            {
                await observer.OnCommentAddedAsync(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommentObserver {ObserverType} failed on comment", observer.GetType().Name);
            }
        }
    }

    private static string MapPriorityScore(double score) => score switch
    {
        <= 0 => "medium",
        <= 5 => "low",
        <= 10 => "medium",
        <= 15 => "high",
        _ => "urgent"
    };

    private async Task QueueCreatedEventAsync(Ticket ticket, CancellationToken ct)
    {
        var evt = new TicketMasala.Web.Messaging.Events.TicketCreatedEvent
        {
            TicketId = ticket.Guid.ToString(),
            CustomerEmail = ticket.Customer?.Email ?? string.Empty,
            CustomerName = $"{ticket.Customer?.FirstName} {ticket.Customer?.LastName}".Trim(),
            TenantId = string.Empty,
            Description = ticket.Title,
            Priority = MapPriorityScore(ticket.PriorityScore),
            CreatedAt = ticket.CreationDate.ToString("O"),
            Timestamp = _clock.UtcNow.ToString("O"),
            Source = "ticket-masala"
        };

        await QueueOutboxMessageAsync(evt.EventType, evt.TicketId, "event.ticket.created", evt, ct);
    }

    private async Task QueueAssignedEventAsync(Ticket ticket, Employee? assignee, string assignedBy, CancellationToken ct)
    {
        var evt = new TicketMasala.Web.Messaging.Events.TicketAssignedEvent
        {
            TicketId = ticket.Guid.ToString(),
            CustomerEmail = ticket.Customer?.Email ?? string.Empty,
            CustomerName = $"{ticket.Customer?.FirstName} {ticket.Customer?.LastName}".Trim(),
            AssignedTo = assignee?.Id ?? ticket.ResponsibleId ?? string.Empty,
            AssignedBy = assignedBy,
            AssignedAt = _clock.UtcNow.ToString("O"),
            Timestamp = _clock.UtcNow.ToString("O"),
            Source = "ticket-masala"
        };

        await QueueOutboxMessageAsync(evt.EventType, evt.TicketId, "event.ticket.assigned", evt, ct);
    }

    private async Task QueueResolvedEventAsync(Ticket ticket, string originalResolutionNotes, CancellationToken ct)
    {
        var evt = new TicketMasala.Web.Messaging.Events.TicketResolvedEvent
        {
            TicketId = ticket.Guid.ToString(),
            CustomerEmail = ticket.Customer?.Email ?? string.Empty,
            CustomerName = $"{ticket.Customer?.FirstName} {ticket.Customer?.LastName}".Trim(),
            ServiceDescription = ticket.Title,
            Amount = ticket.BillableAmount ?? 0m,
            TenantId = string.Empty,
            ResolvedAt = ticket.CompletionDate ?? DateTime.UtcNow,
            ResolutionNotes = ticket.ResolutionNotes ?? originalResolutionNotes,
            Timestamp = _clock.UtcNow.ToString("O"),
            Source = "ticket-masala"
        };

        await QueueOutboxMessageAsync(evt.EventType, evt.TicketId, "event.ticket.resolved", evt, ct);
    }

    private async Task QueueOutboxMessageAsync<T>(string eventType, string ticketId, string routingKey, T evt, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(evt, OutboxJsonOptions);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            RoutingKey = routingKey,
            CreatedAt = _clock.UtcNow
        };

        await _unitOfWork.AddOutboxMessageAsync(outboxMessage, ct);

        _logger.LogDebug(
            "Queued {EventType} outbox message for {TicketId} (routing: {RoutingKey})",
            eventType, ticketId, routingKey);
    }
}
