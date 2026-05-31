namespace MailingService.Services;

public class EmailTemplateService
{
    public string BuildTicketCreatedTemplate(
        string customerName,
        string ticketId,
        string priority,
        string description)
    {
        return $@"
        <html>
        <body style='font-family: Arial; background-color:#f4f4f4; padding:20px;'>

            <div style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:10px;'>

                <h1 style='color:#2563eb;'>
                    Garamatic Support
                </h1>

                <h2>
                    Your ticket has been created
                </h2>

                <p>Hello {customerName},</p>

                <p>
                    Your support ticket has been successfully created.
                </p>

                <div style='background:#f3f4f6; padding:15px; border-radius:8px;'>

                    <p><strong>Ticket ID:</strong> {ticketId}</p>
                    <p><strong>Priority:</strong> {priority}</p>
                    <p><strong>Description:</strong> {description}</p>

                </div>

                <p style='margin-top:20px;'>
                    Our support team will contact you soon.
                </p>

                <hr />

                <p style='font-size:12px; color:gray;'>
                    Garamatic Support Team
                </p>

            </div>

        </body>
        </html>";
    }

    public string BuildTicketAssignedTemplate(
    string ticketId,
    string assignedTo,
    string assignedBy,
    DateTime assignedAt)
    {
        return $@"
        <h2>Ticket Assigned</h2>
        <p><strong>Ticket:</strong> {ticketId}</p>
        <p><strong>Assigned To:</strong> {assignedTo}</p>
        <p><strong>Assigned By:</strong> {assignedBy}</p>
        <p><strong>Date:</strong> {assignedAt:yyyy-MM-dd HH:mm}</p>
    ";
    }

    public string BuildTicketResolvedTemplate(
    string customerName,
    string ticketId,
    string serviceDescription,
    decimal amount,
    DateTime resolvedAt,
    string? resolutionNotes)
    {
        return $@"
        <h2>Ticket Resolved</h2>

        <p>Hello {customerName},</p>

        <p>Great news! Your support ticket has been resolved.</p>

        <p><strong>Ticket ID:</strong> {ticketId}</p>
        <p><strong>Service:</strong> {serviceDescription}</p>
        <p><strong>Amount:</strong> €{amount:F2}</p>
        <p><strong>Resolved At:</strong> {resolvedAt:yyyy-MM-dd HH:mm}</p>

        {(string.IsNullOrEmpty(resolutionNotes)
                ? ""
                : $"<p><strong>Resolution Notes:</strong> {resolutionNotes}</p>")}

        <br/>
        <p>Thank you for choosing Garamatic!</p>

        <p>Best regards,<br/>Garamatic Support Team</p>
    ";
    }

    public string BuildInvoiceCreatedTemplate(
    string invoiceId,
    string odooInvoiceId,
    string ticketId,
    decimal amount,
    string currency,
    string status,
    DateTime createdAt)
    {
        return $@"
        <h2>Your Invoice is Ready</h2>

        <p>Your invoice has been generated and is ready for payment.</p>

        <p><strong>Invoice ID:</strong> {invoiceId}</p>
        <p><strong>Odoo Invoice ID:</strong> {odooInvoiceId}</p>
        <p><strong>Ticket ID:</strong> {ticketId}</p>
        <p><strong>Amount:</strong> {amount:F2} {currency}</p>
        <p><strong>Status:</strong> {status}</p>
        <p><strong>Created:</strong> {createdAt:yyyy-MM-dd HH:mm}</p>

        <br/>
        <p>Please proceed with payment at your earliest convenience.</p>

        <p>Best regards,<br/>Garamatic Support Team</p>
    ";
    }

    public string BuildInvoiceOverdueTemplate(
    string invoiceId,
    string odooInvoiceId,
    decimal amount,
    int daysOverdue)
    {
        return $@"
        <h2>Payment Reminder: Invoice Overdue</h2>

        <p>This is a friendly reminder that your invoice is overdue.</p>

        <p><strong>Invoice ID:</strong> {invoiceId}</p>
        <p><strong>Odoo Invoice ID:</strong> {odooInvoiceId}</p>
        <p><strong>Amount:</strong> €{amount:F2}</p>
        <p><strong>Days Overdue:</strong> {daysOverdue}</p>

        <br/>
        <p>Please arrange payment as soon as possible to avoid service interruption.</p>

        <p>If you have already paid, please disregard this message.</p>

        <p>Best regards,<br/>Garamatic Support Team</p>
    ";
    }

    public string BuildPaymentReceivedTemplate(
    string invoiceId,
    string odooInvoiceId,
    decimal amount,
    string paymentMethod,
    DateTime paidAt)
    {
        return $@"
        <h2>Payment Received - Thank You!</h2>

        <p>Thank you! We have received your payment.</p>

        <p><strong>Invoice ID:</strong> {invoiceId}</p>
        <p><strong>Odoo Invoice ID:</strong> {odooInvoiceId}</p>
        <p><strong>Amount Paid:</strong> €{amount:F2}</p>
        <p><strong>Payment Method:</strong> {paymentMethod}</p>
        <p><strong>Payment Date:</strong> {paidAt:yyyy-MM-dd HH:mm}</p>

        <br/>
        <p>Your invoice has been marked as paid.</p>

        <p>Best regards,<br/>Garamatic Support Team</p>
    ";
    }

    public string BuildUserCreatedTemplate(
    string name,
    string userId,
    string email,
    string role,
    DateTime createdAt)
    {
        return $@"
        <h2>Welcome to Garamatic!</h2>

        <p>Hello {name},</p>

        <p>Welcome to Garamatic! Your account has been successfully created.</p>

        <p><strong>User ID:</strong> {userId}</p>
        <p><strong>Email:</strong> {email}</p>
        <p><strong>Role:</strong> {role}</p>
        <p><strong>Created:</strong> {createdAt:yyyy-MM-dd HH:mm}</p>

        <br/>
        <p>You can now log in and start using our services.</p>

        <p>Best regards,<br/>Garamatic Support Team</p>
    ";
    }
}