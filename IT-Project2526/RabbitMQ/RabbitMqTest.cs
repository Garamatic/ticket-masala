using RabbitMqConnector;
using RabbitMqConnector.Interfaces;
namespace IT_Project2526.RabbitMQ
{

    [RabbitExchange("test-exchange", "topic")]
    public class TestMessage : IProducer
    {
        public string Content { get; set; } = "TEST MESSAGE";
        public DateTime SentAt { get; set; }
    }
    [RabbitQueue("test-queue", RoutingKeys.Test)]
    public class TestHandler : IConsumer<TestMessage>
    {
        public Task HandleMessage(TestMessage message)
        {
            Console.WriteLine($"[x] Received Message: {message.Content} at {message.SentAt}");
            return Task.CompletedTask;
        }

    }
}