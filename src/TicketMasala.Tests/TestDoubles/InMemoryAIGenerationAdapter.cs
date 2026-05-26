using TicketMasala.Domain.Ports;

namespace TicketMasala.Tests.TestDoubles;

/// <summary>
/// Test double for <see cref="IAIGenerationPort"/>.
/// Maps operation names to fixture strings for deterministic unit tests.
/// </summary>
public sealed class InMemoryAIGenerationAdapter : IAIGenerationPort
{
    private readonly IReadOnlyDictionary<string, string> _fixtures;

    public InMemoryAIGenerationAdapter(IReadOnlyDictionary<string, string> fixtures)
    {
        _fixtures = fixtures;
    }

    public Task<AICompletion> CompleteAsync(
        AICompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var text = _fixtures.TryGetValue(request.Operation, out var fixture)
            ? fixture
            : $"[TEST_STUB: {request.Operation}]";

        return Task.FromResult(new AICompletion
        {
            Text = text,
            FromCache = false,
            Diagnostics = new Dictionary<string, object>
            {
                ["provider"] = "InMemoryStub",
                ["operation"] = request.Operation,
            }
        });
    }
}
