using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Tests.UnitTests.Gerda;

public class GerdaStageProviderTests
{
    [Fact]
    public void DefaultProvider_OrdersStagesInCanonicalGerdaOrder()
    {
        // Arrange
        var stages = new IGerdaExecutionStage[]
        {
            new FakeStage(GerdaStage.Knowledge),
            new FakeStage(GerdaStage.Grouping),
            new FakeStage(GerdaStage.Dispatching),
            new FakeStage(GerdaStage.Ranking),
            new FakeStage(GerdaStage.Estimating)
        };

        // Act
        var provider = new DefaultGerdaStageProvider(stages);
        var ordered = provider.GetStages().Select(stage => stage.Stage).ToArray();

        // Assert
        Assert.Equal(
            new[]
            {
                GerdaStage.Grouping,
                GerdaStage.Estimating,
                GerdaStage.Ranking,
                GerdaStage.Dispatching,
                GerdaStage.Knowledge
            },
            ordered);
    }

    [Fact]
    public async Task ProcessAsync_ExecutesProviderStagesInOrder_AndBuildsOutcome()
    {
        // Arrange
        var executionOrder = new List<GerdaStage>();
        var ticketGuid = Guid.NewGuid();

        var stages = new IGerdaExecutionStage[]
        {
            new FakeStage(GerdaStage.Grouping, context =>
            {
                executionOrder.Add(GerdaStage.Grouping);
                context.ParentGuid = Guid.NewGuid();
            }),
            new FakeStage(GerdaStage.Estimating, context =>
            {
                executionOrder.Add(GerdaStage.Estimating);
                context.EffortPoints = 8;
            }),
            new FakeStage(GerdaStage.Ranking, context =>
            {
                executionOrder.Add(GerdaStage.Ranking);
                context.PriorityScore = 13.5;
            }),
            new FakeStage(GerdaStage.Dispatching, context =>
            {
                executionOrder.Add(GerdaStage.Dispatching);
                context.RecommendedAgent = Guid.NewGuid();
            }),
            new FakeStage(GerdaStage.Knowledge, context =>
            {
                executionOrder.Add(GerdaStage.Knowledge);
                context.SuggestedArticles.Add(Guid.NewGuid());
                context.SuggestedArticles.Add(Guid.NewGuid());
            })
        };

        var provider = new DefaultGerdaStageProvider(stages);
        var engine = new GerdaEngine(
            new GerdaConfig
            {
                GerdaAI = new GerdaAISettings { IsEnabled = true }
            },
            Mock.Of<ILogger<GerdaEngine>>(),
            provider);

        // Act
        var outcome = await engine.ProcessAsync(ticketGuid);

        // Assert
        Assert.Equal(
            new[]
            {
                GerdaStage.Grouping,
                GerdaStage.Estimating,
                GerdaStage.Ranking,
                GerdaStage.Dispatching,
                GerdaStage.Knowledge
            },
            executionOrder);

        Assert.Equal(ticketGuid, outcome.TicketGuid);
        Assert.True(outcome.WasGrouped);
        Assert.Equal(8, outcome.EstimatedEffort);
        Assert.Equal(13.5, outcome.PriorityScore);
        Assert.NotNull(outcome.SuggestedAgentId);
        Assert.Equal(2, outcome.RelatedArticles.Count);
    }

    private sealed class FakeStage : IGerdaExecutionStage
    {
        private readonly Action<GerdaExecutionContext>? _mutate;

        public FakeStage(GerdaStage stage, Action<GerdaExecutionContext>? mutate = null)
        {
            Stage = stage;
            _mutate = mutate;
        }

        public GerdaStage Stage { get; }
        public bool IsEnabled => true;

        public Task ExecuteAsync(Guid ticketGuid, GerdaExecutionContext context)
        {
            _mutate?.Invoke(context);
            return Task.CompletedTask;
        }
    }
}
