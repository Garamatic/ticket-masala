using NetArchTest.Rules;
using TicketMasala.Domain.Entities;
using Xunit;

namespace TicketMasala.Tests.Architecture;

public class NamespaceTests
{
    [Fact]
    public void Domain_Entities_Should_Be_In_TicketMasala_Domain_Entities_Namespace()
    {
        // Assemble
        var assembly = typeof(Ticket).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .Inherit(typeof(TicketMasala.Domain.Common.BaseModel))
            .Should()
            .ResideInNamespace("TicketMasala.Domain.Entities")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Domain entities should be in TicketMasala.Domain.Entities namespace");
    }

    [Fact]
    public void Domain_Layer_Should_Not_Have_Dependency_On_Web()
    {
        var result = Types.InAssembly(typeof(Ticket).Assembly)
            .ShouldNot()
            .HaveDependencyOn("TicketMasala.Web")
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain layer should not depend on Web layer");
    }
}
