using Moq;
using TicketMasala.Web.Engine.GERDA;

namespace TicketMasala.Tests.UnitTests.Gerda;

public class GroupingExecutionStageTests
{
    [Fact]
    public async Task ExecuteAsync_WhenGroupingFindsParent_UpdatesContext()
    {
        // Arrange
        var ticketGuid = Guid.NewGuid();
        var parentGuid = Guid.NewGuid();
        var groupingEngine = new Mock<IGroupingEngine>();
        groupingEngine.SetupGet(engine => engine.IsEnabled).Returns(true);
        groupingEngine
            .Setup(engine => engine.CheckAndGroupAsync(ticketGuid))
            .ReturnsAsync(parentGuid);

        var stage = new GroupingExecutionStage(groupingEngine.Object);
        var context = new GerdaExecutionContext();

        // Act
        await stage.ExecuteAsync(ticketGuid, context);

        // Assert
        Assert.Equal(parentGuid, context.ParentGuid);
        groupingEngine.Verify(engine => engine.CheckAndGroupAsync(ticketGuid), Times.Once);
    }
}
