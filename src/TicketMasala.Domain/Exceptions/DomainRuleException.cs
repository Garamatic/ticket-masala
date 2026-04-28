namespace TicketMasala.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain rule or business rule is violated.
/// This is distinct from DomainException (which is for invariants) - DomainRuleException
/// is for configurable business rules that may vary by domain/customer.
/// </summary>
public class DomainRuleException : Exception
{
    public DomainRuleException(string message) : base(message)
    {
    }

    public DomainRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
