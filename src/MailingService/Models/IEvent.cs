namespace MailingService.Models;

public interface IEvent
{
	string EventType { get; set; }
}