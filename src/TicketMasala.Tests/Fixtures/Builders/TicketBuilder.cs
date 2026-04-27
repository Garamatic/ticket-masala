using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Tests.Fixtures.Builders;

/// <summary>
/// Fluent builder for creating Ticket test objects.
/// </summary>
public class TicketBuilder
{
    private Guid _guid = Guid.NewGuid();
    private string _title = "Test Ticket";
    private string _description = "Test Description";
    private string _domainId = "IT";
    private string _status = "New";
    private Status _ticketStatus = Status.Pending;
    private string? _customerId;
    private string? _responsibleId;
    private Guid? _projectGuid;
    private int _estimatedEffortPoints = 5;
    private double _priorityScore = 50.0;
    private string? _workItemTypeCode = "INCIDENT";
    private DateTime _creationDate = DateTime.UtcNow;
    private DateTime? _completionTarget;

    public TicketBuilder WithGuid(Guid guid)
    {
        _guid = guid;
        return this;
    }

    public TicketBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public TicketBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TicketBuilder WithDomain(string domainId)
    {
        _domainId = domainId;
        return this;
    }

    public TicketBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public TicketBuilder WithTicketStatus(Status status)
    {
        _ticketStatus = status;
        return this;
    }

    public TicketBuilder WithCustomer(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public TicketBuilder WithResponsible(string responsibleId)
    {
        _responsibleId = responsibleId;
        return this;
    }

    public TicketBuilder WithProject(Guid projectGuid)
    {
        _projectGuid = projectGuid;
        return this;
    }

    public TicketBuilder WithEffortPoints(int points)
    {
        _estimatedEffortPoints = points;
        return this;
    }

    public TicketBuilder WithPriorityScore(double score)
    {
        _priorityScore = score;
        return this;
    }

    public TicketBuilder WithWorkItemType(string typeCode)
    {
        _workItemTypeCode = typeCode;
        return this;
    }

    public TicketBuilder WithCompletionTarget(DateTime target)
    {
        _completionTarget = target;
        return this;
    }

    public TicketBuilder WithCreationDate(DateTime date)
    {
        _creationDate = date;
        return this;
    }

    public Ticket Build()
    {
        return new Ticket
        {
            Guid = _guid,
            Title = _title,
            Description = _description,
            DomainId = _domainId,
            Status = _status,
            TicketStatus = _ticketStatus,
            CustomerId = _customerId,
            ResponsibleId = _responsibleId,
            ProjectGuid = _projectGuid,
            EstimatedEffortPoints = _estimatedEffortPoints,
            PriorityScore = _priorityScore,
            WorkItemTypeCode = _workItemTypeCode,
            CreationDate = _creationDate,
            CompletionTarget = _completionTarget,
            CustomFieldsJson = "{}"
        };
    }
}
