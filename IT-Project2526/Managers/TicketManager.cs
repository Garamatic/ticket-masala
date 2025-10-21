using IT_Project2526.Models;

namespace IT_Project2526.Managers
{
    public class TicketManager(ITProjectDB db)
    {
        private ITProjectDB db = db;

        public Ticket FetchTicket(Guid ticketGuid) 
        {
            return null;
        }
       
    }
}
