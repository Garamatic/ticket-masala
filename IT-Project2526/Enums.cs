namespace IT_Project2526
{
    public enum Status
    {
        Pending,
        Rejected,
        Assigned,
        InProgress,
        Postponed,
        Completed,
        Failed
    }
    public enum TicketType
    {
        Unknown,
        ProjectRequest,
        Subtask
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
}
