using System.Security.Claims;
using Moq;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Configuration;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Engine.GERDA.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace TicketMasala.Tests.Services.GERDA;

public class RuleEngineServiceTests
{
    private readonly Mock<IDomainConfigurationService> _domainConfigMock;
    private readonly Mock<ILogger<RuleEngineService>> _loggerMock;
    private readonly RuleCompilerService _compiler;
    private readonly RuleEngineService _service;

    public RuleEngineServiceTests()
    {
        _domainConfigMock = new Mock<IDomainConfigurationService>();
        _loggerMock = new Mock<ILogger<RuleEngineService>>();
        
        // Use real compiler for integration testing of the engine+compiler pair
        var compilerLogger = new Mock<ILogger<RuleCompilerService>>();
        _compiler = new RuleCompilerService(compilerLogger.Object);

        _service = new RuleEngineService(
            _domainConfigMock.Object,
            _compiler,
            _loggerMock.Object
        );
    }

    [Fact]
    public void CanTransition_ShouldAllow_WhenTransitionIsValid_AndNoRules()
    {
        // Arrange
        var ticket = new Ticket { DomainId = "IT", TicketStatus = Status.Pending };
        var user = new ClaimsPrincipal();

        _domainConfigMock.Setup(x => x.GetValidTransitions("IT", "Pending"))
            .Returns(new List<string> { "Assigned" });
        
        // Mock domain without specific rules
        _domainConfigMock.Setup(x => x.GetDomain("IT"))
            .Returns(new DomainConfig());

        // Act
        var result = _service.CanTransition(ticket, Status.Assigned, user);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanTransition_ShouldDeny_WhenTransitionIsNotInValidList()
    {
        // Arrange
        var ticket = new Ticket { DomainId = "IT", TicketStatus = Status.Pending };
        var user = new ClaimsPrincipal();

        _domainConfigMock.Setup(x => x.GetValidTransitions("IT", "Pending"))
            .Returns(new List<string> { "Assigned" });

        // Act
        var result = _service.CanTransition(ticket, Status.Completed, user);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanTransition_ShouldDeny_WhenRuleConditionFails()
    {
        // Arrange
        var ticket = new Ticket 
        { 
            DomainId = "IT", 
            TicketStatus = Status.Pending,
            CustomFieldsJson = "{\"vip\": false}" 
        };
        var user = new ClaimsPrincipal();

        _domainConfigMock.Setup(x => x.GetValidTransitions("IT", "Pending"))
            .Returns(new List<string> { "Assigned" });

        var domainConfig = new DomainConfig
        {
            Workflow = new WorkflowConfig
            {
                TransitionRules = new List<TransitionRule>
                {
                    new TransitionRule
                    {
                        From = "Pending",
                        To = "Assigned",
                        Conditions = new List<TransitionCondition>
                        {
                            new TransitionCondition
                            {
                                Field = "vip",
                                Operator = "==",
                                Value = "true"
                            }
                        }
                    }
                }
            }
        };

        _domainConfigMock.Setup(x => x.GetDomain("IT"))
            .Returns(domainConfig);

        // Act
        var result = _service.CanTransition(ticket, Status.Assigned, user);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanTransition_ShouldAllow_WhenRuleConditionPasses()
    {
        // Arrange
        var ticket = new Ticket 
        { 
            DomainId = "IT", 
            TicketStatus = Status.Pending,
            CustomFieldsJson = "{\"vip\": true}" 
        };
        var user = new ClaimsPrincipal();

        _domainConfigMock.Setup(x => x.GetValidTransitions("IT", "Pending"))
            .Returns(new List<string> { "Assigned" });

        var domainConfig = new DomainConfig
        {
            Workflow = new WorkflowConfig
            {
                TransitionRules = new List<TransitionRule>
                {
                    new TransitionRule
                    {
                        From = "Pending",
                        To = "Assigned",
                        Conditions = new List<TransitionCondition>
                        {
                            new TransitionCondition
                            {
                                Field = "vip",
                                Operator = "==",
                                Value = "true"
                            }
                        }
                    }
                }
            }
        };

        _domainConfigMock.Setup(x => x.GetDomain("IT"))
            .Returns(domainConfig);

        // Act
        var result = _service.CanTransition(ticket, Status.Assigned, user);

        // Assert
        Assert.True(result);
    }
}
