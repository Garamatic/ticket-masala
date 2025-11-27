namespace IT_Project2526.Services
{
    /// <summary>
    /// Interface for email sending operations
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a password reset email to the specified address
        /// </summary>
        Task SendPasswordResetEmailAsync(string email, string resetLink);
        
        /// <summary>
        /// Sends a welcome email to a new customer with temporary password
        /// </summary>
        Task SendWelcomeEmailAsync(string email, string firstName, string tempPassword);
        
        /// <summary>
        /// Sends a project assignment notification email
        /// </summary>
        Task SendProjectAssignmentEmailAsync(string email, string projectName);
        
        /// <summary>
        /// Sends a ticket notification email
        /// </summary>
        Task SendTicketNotificationAsync(string email, string ticketDescription);
    }
}
