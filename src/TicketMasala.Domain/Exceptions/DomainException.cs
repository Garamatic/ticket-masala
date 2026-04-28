namespace TicketMasala.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain invariant is violated or business rule is broken.
/// These exceptions indicate that an operation cannot be performed due to domain logic,
/// not due to technical failures (which would use standard exceptions).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
