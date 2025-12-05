namespace IT_Project2526.Models
{
    public enum Status
    {
        Pending,
        Rejected,
        Assigned,
        InProgress,
        //Postponed,
        Completed,
        Failed
    }
    public enum TicketType
    {
        Incident,
        ServiceRequest,
        ProjectRequest,
        Subtask
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
        CEO
    }

}
