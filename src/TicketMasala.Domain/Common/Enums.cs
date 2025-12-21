namespace TicketMasala.Domain.Common;

/// <summary>
/// Common enumerations used across the domain.
/// </summary>
public enum Status
{
    Pending,
    Rejected,
    Assigned,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum TicketType
{
    Incident,
    ServiceRequest,
    ProjectRequest,
    Subtask,
    Task
}

public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

public enum ReviewStatus
{
    None,
    Pending,
    Approved,
    Rejected
}

public enum Category
{
    Unknown,
    General,
    Finance,
    Logistics,
    HR,
}

public enum SubCategory
{
    Unknown = 0,
    General,
}

public enum EmployeeType
{
    Admin,
    ProjectManager,
    Support,
    Finance,
    CEO,
    Developer
}

