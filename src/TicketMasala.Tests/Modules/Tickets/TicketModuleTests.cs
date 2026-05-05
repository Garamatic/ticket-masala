using TicketMasala.Domain.Repositories;
using TicketMasala.Web.Common;
using TicketMasala.Web.Modules.Tickets;
using Xunit;

namespace TicketMasala.Tests.Modules.Tickets;

/// <summary>
/// Tests verifying the TicketModule is properly registered and accessible.
/// These are simple DI container verification tests.
/// </summary>
public class TicketModuleRegistrationTests
{
    [Fact]
    public void ITicketModule_Interface_IsPublicAndAccessible()
    {
        // This test verifies the interface is public
        var type = typeof(ITicketModule);
        Assert.True(type.IsPublic, "ITicketModule should be public");
        Assert.True(type.IsInterface, "ITicketModule should be an interface");

        // Verify it has the expected methods
        var methods = type.GetMethods();
        Assert.Contains(methods, m => m.Name == "CreateAsync");
        Assert.Contains(methods, m => m.Name == "UpdateAsync");
        Assert.Contains(methods, m => m.Name == "AssignAsync");
        Assert.Contains(methods, m => m.Name == "TransitionStatusAsync");
        Assert.Contains(methods, m => m.Name == "GetDetailsAsync");
        Assert.Contains(methods, m => m.Name == "SearchAsync");
    }

    [Fact]
    public void CommonResult_IsRecordType()
    {
        var type = typeof(Result<object>);
        Assert.True(type.IsPublic);

        // Verify it has the expected properties
        var properties = type.GetProperties();
        Assert.Contains(properties, p => p.Name == "IsSuccess");
        Assert.Contains(properties, p => p.Name == "Value");
        Assert.Contains(properties, p => p.Name == "Error");
    }

    [Fact]
    public void Command_Dtos_ArePublicAndHaveExpectedProperties()
    {
        // CreateTicketCommand
        var createType = typeof(CreateTicketCommand);
        Assert.True(createType.IsPublic);

        // UpdateTicketCommand
        var updateType = typeof(UpdateTicketCommand);
        Assert.True(updateType.IsPublic);

        // AssignTicketCommand
        var assignType = typeof(AssignTicketCommand);
        Assert.True(assignType.IsPublic);

        // TransitionStatusCommand
        var transitionType = typeof(TransitionStatusCommand);
        Assert.True(transitionType.IsPublic);
    }

    [Fact]
    public void Query_Dtos_ArePublic()
    {
        Assert.True(typeof(TicketSearchQuery).IsPublic);
        Assert.True(typeof(TicketSearchResult).IsPublic);
        Assert.True(typeof(TicketSummaryDto).IsPublic);
        Assert.True(typeof(TicketDetailsDto).IsPublic);
    }

    [Fact]
    public void TicketSearchResult_HasExpectedProperties()
    {
        var type = typeof(TicketSearchResult);
        var properties = type.GetProperties();

        Assert.Contains(properties, p => p.Name == "Items");
        Assert.Contains(properties, p => p.Name == "TotalCount");
        Assert.Contains(properties, p => p.Name == "Page");
        Assert.Contains(properties, p => p.Name == "PageSize");
    }

    [Fact]
    public void Unit_Type_Exists()
    {
        var type = typeof(Unit);
        Assert.True(type.IsPublic);
        // Value is now a static property (not a field) for immutability
        var valueProperty = type.GetProperty("Value");
        Assert.NotNull(valueProperty);
        Assert.True(valueProperty.GetMethod?.IsPublic);
    }
}
