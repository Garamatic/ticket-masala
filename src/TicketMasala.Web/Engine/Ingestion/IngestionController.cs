using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;
using TicketMasala.Web.Models;

namespace TicketMasala.Web.Engine.Ingestion;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly Channel<WorkItem> _channel;

    public IngestionController(Channel<WorkItem> channel)
    {
        _channel = channel;
    }

    [HttpPost]
    public IActionResult Post([FromBody] WorkItem workItem)
    {
        if (!_channel.Writer.TryWrite(workItem))
        {
            return StatusCode(429, "Too Many Requests");
        }

        return Accepted();
    }
}