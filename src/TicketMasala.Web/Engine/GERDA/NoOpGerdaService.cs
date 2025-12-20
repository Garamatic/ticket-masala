using System.Threading.Tasks;

namespace TicketMasala.Web.Engine.GERDA
{
    public class NoOpGerdaService : IGerdaService
    {
        public Task ProcessTicketAsync(Guid ticketGuid) => Task.CompletedTask;
        public Task ProcessAllOpenTicketsAsync() => Task.CompletedTask;
        public bool IsEnabled => false;
    }
}
