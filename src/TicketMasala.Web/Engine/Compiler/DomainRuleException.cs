// This file is deprecated. Use TicketMasala.Domain.Exceptions.DomainRuleException instead.
// Kept for backward compatibility during migration.

using TicketMasala.Domain.Exceptions;

namespace TicketMasala.Web.Engine.Compiler;

/// <summary>
/// DEPRECATED: Use TicketMasala.Domain.Exceptions.DomainRuleException instead.
/// This alias exists for backward compatibility during the migration period.
/// </summary>
[Obsolete("Use TicketMasala.Domain.Exceptions.DomainRuleException instead")]
public class DomainRuleException : TicketMasala.Domain.Exceptions.DomainRuleException
{
    public DomainRuleException(string message) : base(message)
    {
    }

    public DomainRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
