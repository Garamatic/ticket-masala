namespace TicketMasala.Web.Models;
    public enum Status
    {
        Pending,
        Rejected,
        Assigned,
        InProgress,
        //Postponed,
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
        Task // Added Task as it was used in TemplateTicket default
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
        CEO
    }
