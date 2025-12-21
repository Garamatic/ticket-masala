using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Web.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Repositories;
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
    public async Task SearchTicketsAsync_FiltersByStatus()
    {
        // Arrange
        var customer = new ApplicationUser { Id = "cust1", FirstName = "John", LastName = "Doe", UserName = "john", Email = "john@example.com", Code = "C1", Phone = "555-1234" };
        _context.Users.Add(customer);

        var t1 = new Ticket { Guid = Guid.NewGuid(), Title = "T1", Description = "Desc1", TicketStatus = TicketMasala.Domain.Common.Status.Pending, Customer = customer, DomainId = "IT" };
        var t2 = new Ticket { Guid = Guid.NewGuid(), Title = "T2", Description = "Desc2", TicketStatus = TicketMasala.Domain.Common.Status.Completed, Customer = customer, DomainId = "IT" };

        _context.Tickets.AddRange(t1, t2);
        await _context.SaveChangesAsync();

        var searchModel = new TicketSearchViewModel { Status = TicketMasala.Domain.Common.Status.Pending };

        // Act
        var result = await _repository.SearchTicketsAsync(searchModel);

        // Assert
        Assert.Single(result.Results);
        Assert.Equal("Desc1", result.Results.First().Description);
    }

    [Fact]
    public async Task SearchTicketsAsync_FiltersBySearchTerm()
    {
        // Arrange
        var customer = new ApplicationUser { Id = "cust1", FirstName = "John", LastName = "Doe", UserName = "john", Email = "john@example.com", Code = "C1", Phone = "555-1234" };
        _context.Users.Add(customer);

        var t1 = new Ticket { Guid = Guid.NewGuid(), Title = "T1", Description = "Apple Problem", TicketStatus = TicketMasala.Domain.Common.Status.Pending, Customer = customer, DomainId = "IT" };
        var t2 = new Ticket { Guid = Guid.NewGuid(), Title = "T2", Description = "Banana Issue", TicketStatus = TicketMasala.Domain.Common.Status.Pending, Customer = customer, DomainId = "IT" };

        _context.Tickets.AddRange(t1, t2);
        await _context.SaveChangesAsync();

        var searchModel = new TicketSearchViewModel { SearchTerm = "Apple" };

        // Act
        var result = await _repository.SearchTicketsAsync(searchModel);

        // Assert
        Assert.Single(result.Results);
        Assert.Equal("Apple Problem", result.Results.First().Description);
    }
}
