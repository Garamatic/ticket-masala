namespace IT_Project2526.Services
{
    /// <summary>
    /// Stub implementation of email service for development.
    /// TODO: Replace with actual email provider (SendGrid, SMTP, etc.) in production
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        
        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        
        public Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            _logger.LogInformation(
                "[EMAIL STUB] Password reset email would be sent to {Email} with link {ResetLink}", 
                email, resetLink);
            
            // TODO: Implement actual email sending
            // Example with SendGrid:
            // var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
            // var msg = new SendGridMessage()
            // {
            //     From = new EmailAddress("noreply@ticketmasala.com", "Ticket Masala"),
            //     Subject = "Reset Your Password",
            //     HtmlContent = $"Click <a href='{resetLink}'>here</a> to reset your password"
            // };
            // msg.AddTo(new EmailAddress(email));
            // await client.SendEmailAsync(msg);
            
            return Task.CompletedTask;
        }
        
        public Task SendWelcomeEmailAsync(string email, string firstName, string tempPassword)
        {
            _logger.LogInformation(
                "[EMAIL STUB] Welcome email would be sent to {Email} for {FirstName} with temp password", 
                email, firstName);
            
            _logger.LogWarning(
                "SECURITY: Temporary password for {Email} is: {TempPassword} (This should only appear in dev environment)", 
                email, tempPassword);
            
            // TODO: In production, send password reset link instead of temp password
            // Subject: "Welcome to Ticket Masala"
            // Body: Include instructions to reset password on first login
            
            return Task.CompletedTask;
        }
        
        public Task SendProjectAssignmentEmailAsync(string email, string projectName)
        {
            _logger.LogInformation(
                "[EMAIL STUB] Project assignment email would be sent to {Email} for project {ProjectName}", 
                email, projectName);
            
            // TODO: Implement actual email sending
            // Subject: "You have been assigned to project: {projectName}"
            // Body: Include project details and link to project page
            
            return Task.CompletedTask;
        }
        
        public Task SendTicketNotificationAsync(string email, string ticketDescription)
        {
            _logger.LogInformation(
                "[EMAIL STUB] Ticket notification email would be sent to {Email} for ticket: {TicketDescription}", 
                email, ticketDescription);
            
            // TODO: Implement actual email sending
            // Subject: "New ticket assigned to you"
            // Body: Include ticket details and link to ticket page
            
            return Task.CompletedTask;
        }
    }
}
