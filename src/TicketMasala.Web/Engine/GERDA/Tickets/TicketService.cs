using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.Data;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Engine.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Engine.Security;
using TicketMasala.Web.Engine.GERDA.Configuration;

namespace TicketMasala.Web.Engine.GERDA.Tickets;



public class TicketService : ITicketService
{
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ILogger<TicketService> logger)
    {
        _logger = logger;
    }
}

