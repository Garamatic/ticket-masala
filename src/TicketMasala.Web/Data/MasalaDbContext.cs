// Re-export MasalaDbContext from Domain project for backward compatibility.
// This allows existing code using TicketMasala.Web.Data.MasalaDbContext to continue working.
// New code should use: using TicketMasala.Domain.Data;

global using MasalaDbContext = TicketMasala.Domain.Data.MasalaDbContext;

namespace TicketMasala.Web.Data
{
    // Type forwarding - MasalaDbContext is now in TicketMasala.Domain.Data
    // This file exists for backward compatibility with existing usings.
}