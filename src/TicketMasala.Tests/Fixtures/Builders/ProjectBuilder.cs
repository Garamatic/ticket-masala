using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Tests.Fixtures.Builders;

/// <summary>
/// Fluent builder for creating Project test objects.
/// </summary>
public class ProjectBuilder
{
    private Guid _guid = Guid.NewGuid();
    private string _name = "Test Project";
    private string _description = "Test Project Description";
    private Status _status = Status.InProgress;
    private string? _customerId;
    private string? _projectManagerId;
    private DateTime _creationDate = DateTime.UtcNow;
    private DateTime? _completionTarget = DateTime.UtcNow.AddMonths(3);

    public ProjectBuilder WithGuid(Guid guid)
    {
        _guid = guid;
        return this;
    }

    public ProjectBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProjectBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ProjectBuilder WithStatus(Status status)
    {
        _status = status;
        return this;
    }

    public ProjectBuilder WithCustomer(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public ProjectBuilder WithProjectManager(string projectManagerId)
    {
        _projectManagerId = projectManagerId;
        return this;
    }

    public ProjectBuilder WithCreationDate(DateTime date)
    {
        _creationDate = date;
        return this;
    }

    public ProjectBuilder WithCompletionTarget(DateTime? target)
    {
        _completionTarget = target;
        return this;
    }

    public Project Build()
    {
        return new Project
        {
            Guid = _guid,
            Name = _name,
            Description = _description,
            Status = _status,
            CustomerId = _customerId,
            ProjectManagerId = _projectManagerId,
            CreationDate = _creationDate,
            CompletionTarget = _completionTarget
        };
    }
}
