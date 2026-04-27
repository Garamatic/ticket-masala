namespace TicketMasala.Domain.Enums;

/// <summary>
/// Represents the type of activity in the ticket lifecycle.
/// Centralized enum to replace scattered string literals and switch statements.
/// </summary>
public enum ActivityType
{
    /// <summary>
    /// Ticket was created
    /// </summary>
    Created,

    /// <summary>
    /// Ticket was assigned to an agent
    /// </summary>
    Assigned,

    /// <summary>
    /// Ticket status was updated
    /// </summary>
    StatusChanged,

    /// <summary>
    /// Comment was added to ticket
    /// </summary>
    CommentAdded,

    /// <summary>
    /// Ticket was completed/closed
    /// </summary>
    Completed,

    /// <summary>
    /// Other/unspecified activity
    /// </summary>
    Other
}

/// <summary>
/// Extension methods for ActivityType enum to centralize CSS class and display logic.
/// Eliminates duplicated switch statements across view models and views.
/// </summary>
public static class ActivityTypeExtensions
{
    /// <summary>
    /// Gets the Bootstrap CSS class for this activity type.
    /// Used for badge/label styling in the UI.
    /// </summary>
    public static string GetCssClass(this ActivityType type)
    {
        return type switch
        {
            ActivityType.Created => "primary",
            ActivityType.Assigned => "info",
            ActivityType.StatusChanged => "warning",
            ActivityType.CommentAdded => "secondary",
            ActivityType.Completed => "success",
            ActivityType.Other => "secondary",
            _ => "secondary"
        };
    }

    /// <summary>
    /// Gets the display icon for this activity type.
    /// </summary>
    public static string GetIcon(this ActivityType type)
    {
        return type switch
        {
            ActivityType.Created => "bi-plus-circle",
            ActivityType.Assigned => "bi-person-check",
            ActivityType.StatusChanged => "bi-arrow-repeat",
            ActivityType.CommentAdded => "bi-chat-dots",
            ActivityType.Completed => "bi-check-circle",
            ActivityType.Other => "bi-dot",
            _ => "bi-dot"
        };
    }

    /// <summary>
    /// Gets the display text for this activity type.
    /// </summary>
    public static string GetDisplayText(this ActivityType type)
    {
        return type switch
        {
            ActivityType.Created => "Created",
            ActivityType.Assigned => "Assigned",
            ActivityType.StatusChanged => "Status Changed",
            ActivityType.CommentAdded => "Comment Added",
            ActivityType.Completed => "Completed",
            ActivityType.Other => "Activity",
            _ => "Activity"
        };
    }

    /// <summary>
    /// Parses a string value into an ActivityType enum.
    /// </summary>
    public static ActivityType Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ActivityType.Other;

        return value.ToLowerInvariant() switch
        {
            "created" => ActivityType.Created,
            "assigned" => ActivityType.Assigned,
            "statuschanged" or "status_changed" or "status changed" => ActivityType.StatusChanged,
            "commentadded" or "comment_added" or "comment added" => ActivityType.CommentAdded,
            "completed" or "closed" => ActivityType.Completed,
            _ => ActivityType.Other
        };
    }
}
