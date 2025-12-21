using Xunit;
using FluentAssertions;
using TicketMasala.Domain.Data;

namespace TicketMasala.Web.Tests.UnitTests.Data;

public class DatabaseProviderTests
{
    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore.Sqlite", "SQLite")]
    [InlineData("Microsoft.EntityFrameworkCore.SqlServer", "SQL Server")]
    [InlineData("Microsoft.EntityFrameworkCore.PostgreSQL", "PostgreSQL")]
    [InlineData("Npgsql.EntityFrameworkCore.PostgreSQL", "PostgreSQL")]
    [InlineData("Unknown.Provider", "Unknown.Provider")]
    public void GetProviderDisplayName_ReturnsCorrectName(string providerName, string expectedDisplayName)
    {
        // Act
        var result = DatabaseProviderHelper.GetProviderDisplayName(providerName);

        // Assert
        result.Should().Be(expectedDisplayName);
    }

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore.Sqlite", true)]
    [InlineData("Microsoft.EntityFrameworkCore.SqlServer", true)]
    [InlineData("Microsoft.EntityFrameworkCore.PostgreSQL", true)]
    [InlineData("Npgsql.EntityFrameworkCore.PostgreSQL", true)]
    [InlineData("Unknown.Provider", false)]
    public void SupportsComputedColumns_ReturnsCorrectSupport(string providerName, bool expectedSupport)
    {
        // Act
        var result = DatabaseProviderHelper.SupportsComputedColumns(providerName);

        // Assert
        result.Should().Be(expectedSupport);
    }
}
