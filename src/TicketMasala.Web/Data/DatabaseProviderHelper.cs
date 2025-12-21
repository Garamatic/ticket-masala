// Re-export DatabaseProviderHelper from Domain project for backward compatibility.
// New code should use: using TicketMasala.Domain.Data;

global using DatabaseProviderHelper = TicketMasala.Domain.Data.DatabaseProviderHelper;

namespace TicketMasala.Web.Data
{
    // Type forwarding - DatabaseProviderHelper is now in TicketMasala.Domain.Data
}
