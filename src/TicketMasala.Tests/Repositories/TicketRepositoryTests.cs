using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Domain.Data;
using TicketMasala.Web.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Repositories.Queries;
using TicketMasala.Web.ViewModels.Tickets;
using Xunit;

namespace TicketMasala.Tests.Repositories;

public class TicketRepositoryTests
{
    private readonly MasalaDbContext _context;
    private readonly EfCoreTicketRepository _repository;

    public TicketRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MasalaDbContext(options);
        _repository = new EfCoreTicketRepository(_context, Mock.Of<ILogger<EfCoreTicketRepository>>());
    }

    [Fact]
    public async Task SearchAsync_FiltersByStatus()
    {
        // Arrange
        var customer = new ApplicationUser { Id = "cust1", FirstName = "John", LastName = "Doe", UserName = "john", Email = "john@example.com", Code = "C1", Phone = "555-1234" };
        _context.Users.Add(customer);

        var t1 = new Ticket { Guid = Guid.NewGuid(), Title = "T1", Description = "Desc1", TicketStatus = TicketMasala.Domain.Common.Status.Pending, Customer = customer, DomainId = "IT" };
        var t2 = new Ticket { Guid = Guid.NewGuid(), Title = "T2", Description = "Desc2", TicketStatus = TicketMasala.Domain.Common.Status.Completed, Customer = customer, DomainId = "IT" };

        _context.Tickets.AddRange(t1, t2);
        await _context.SaveChangesAsync();

        var query = new TicketSearchQuery { Status = TicketMasala.Domain.Common.Status.Pending };

        // Act
        var (results, totalItems) = await _repository.SearchAsync(query);

        // Assert
        Assert.Single(results);
        Assert.Equal("Desc1", results.First().Description);
        Assert.Equal(1, totalItems);
    }

    [Fact]
    public async Task SearchAsync_FiltersBySearchTerm()
    {
        // Arrange
        var customer = new ApplicationUser { Id = "cust1", FirstName = "John", LastName = "Doe", UserName = "john", Email = "john@example.com", Code = "C1", Phone = "555-1234" };
        _context.Users.Add(customer);

        var t1 = new Ticket { Guid = Guid.NewGuid(), Title = "T1", Description = "Apple Problem", TicketStatus = TicketMasala.Domain.Common.Status.Pending, Customer = customer, DomainId = "IT" };
        var t2 = new Ticket { Guid = Guid.NewGuid(), Title = "T2", Description = "Banana Issue", TicketStatus = TicketMasala.Domain.Common.Status.Pending, Customer = customer, DomainId = "IT" };

        _context.Tickets.AddRange(t1, t2);
        await _context.SaveChangesAsync();

        var query = new TicketSearchQuery { SearchTerm = "Apple" };

        // Act
        var (results, totalItems) = await _repository.SearchAsync(query);

        // Assert
        Assert.Single(results);
        Assert.Equal("Apple Problem", results.First().Description);
        Assert.Equal(1, totalItems);
    }
}
