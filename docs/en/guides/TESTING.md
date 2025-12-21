# Testing Guide

Comprehensive guide to testing in Ticket Masala.

## Quick Start

```bash
# Run all tests
cd src/factory/TicketMasala
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific test project
dotnet test src/TicketMasala.Tests/

# Run specific test class
dotnet test --filter "FullyQualifiedName~TicketServiceTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Test Project Structure

```
src/TicketMasala.Tests/
├── Api/                    # API endpoint tests
├── Architecture/           # Architecture rule tests (NetArchTest)
├── Configuration/          # Configuration loading tests
├── Controllers/            # MVC controller tests
├── Fixtures/               # Shared test fixtures
├── Functional/             # End-to-end functional tests
├── IntegrationTests/       # WebApplicationFactory tests
├── Middleware/             # Custom middleware tests
├── Repositories/           # Repository pattern tests
├── Robustness/             # Edge case and error handling tests
├── Services/               # Business logic unit tests
├── Tenancy/                # Multi-tenant tests
├── TestHelpers/            # Shared utilities
└── UnitTests/              # Pure unit tests
```

---

## Test Frameworks & Libraries

| Library | Purpose |
|---------|---------|
| **xUnit** | Test framework |
| **FluentAssertions** | Readable assertions |
| **Moq** | Mocking framework |
| **Bogus** | Fake data generation |
| **FsCheck** | Property-based testing |
| **NetArchTest** | Architecture rule validation |
| **WebApplicationFactory** | Integration testing |

---

## Unit Tests

Test individual components in isolation using mocks.

**Example: Testing TicketService**

```csharp
public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _mockRepo;
    private readonly Mock<ILogger<TicketService>> _mockLogger;
    private readonly TicketService _sut;

    public TicketServiceTests()
    {
        _mockRepo = new Mock<ITicketRepository>();
        _mockLogger = new Mock<ILogger<TicketService>>();
        _sut = new TicketService(_mockRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateTicket_ValidInput_ReturnsTicket()
    {
        // Arrange
        var description = "Test ticket";
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        // Act
        var result = await _sut.CreateTicketAsync(
            description, "customer-id", null, null, DateTime.UtcNow.AddDays(7));

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Contain(description);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
    }
}
```

---

## Integration Tests

Test with real database using WebApplicationFactory.

```csharp
public class TicketApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TicketApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace with in-memory database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MasalaDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<MasalaDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetTickets_ReturnsOkStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/work-items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

---

## Architecture Tests

Enforce architectural constraints using NetArchTest.

```csharp
public class ArchitectureTests
{
    [Fact]
    public void Controllers_ShouldNotDependOnRepositories()
    {
        var result = Types.InAssembly(typeof(TicketController).Assembly)
            .That()
            .ResideInNamespace("TicketMasala.Web.Controllers")
            .ShouldNot()
            .HaveDependencyOn("TicketMasala.Web.Repositories")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Services_ShouldHaveServiceSuffix()
    {
        var result = Types.InAssembly(typeof(TicketService).Assembly)
            .That()
            .ResideInNamespace("TicketMasala.Web.Engine")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Service")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
```

---

## Property-Based Tests

Use FsCheck for generative testing.

```csharp
public class TicketPropertyTests
{
    [Property]
    public Property TicketPriority_IsAlwaysNonNegative()
    {
        return Prop.ForAll<string, int>((description, effort) =>
        {
            var ticket = new Ticket
            {
                Description = description ?? "",
                EstimatedEffortPoints = Math.Abs(effort)
            };
            return ticket.PriorityScore >= 0;
        });
    }
}
```

---

## Test Data Generation

Use Bogus for realistic test data.

```csharp
public static class TestDataFactory
{
    private static readonly Faker<ApplicationUser> UserFaker = new Faker<ApplicationUser>()
        .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
        .RuleFor(u => u.FirstName, f => f.Name.FirstName())
        .RuleFor(u => u.LastName, f => f.Name.LastName())
        .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
        .RuleFor(u => u.UserName, (f, u) => u.Email);

    private static readonly Faker<Ticket> TicketFaker = new Faker<Ticket>()
        .RuleFor(t => t.Guid, f => Guid.NewGuid())
        .RuleFor(t => t.Description, f => f.Lorem.Paragraph())
        .RuleFor(t => t.Status, f => f.PickRandom<Status>())
        .RuleFor(t => t.CreationDate, f => f.Date.Past());

    public static ApplicationUser CreateUser() => UserFaker.Generate();
    public static Ticket CreateTicket() => TicketFaker.Generate();
    public static List<Ticket> CreateTickets(int count) => TicketFaker.Generate(count);
}
```

---

## Shared Fixtures

Reduce test setup duplication.

```csharp
public class DatabaseFixture : IDisposable
{
    public MasalaDbContext Context { get; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        Context = new MasalaDbContext(options);
        SeedTestData();
    }

    private void SeedTestData()
    {
        Context.Users.AddRange(TestDataFactory.CreateUsers(5));
        Context.Tickets.AddRange(TestDataFactory.CreateTickets(10));
        Context.SaveChanges();
    }

    public void Dispose() => Context.Dispose();
}

// Usage
public class MyTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    public MyTests(DatabaseFixture fixture) => _fixture = fixture;
}
```

---

## Test Categories

Use traits to categorize tests:

```csharp
[Trait("Category", "Unit")]
public class UnitTests { }

[Trait("Category", "Integration")]
public class IntegrationTests { }

[Trait("Category", "Slow")]
public class SlowTests { }
```

Run by category:
```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category!=Slow"
```

---

## Code Coverage

```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Install ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html
```

---

## CI/CD Integration

Tests run automatically on GitHub Actions:

```yaml
# .github/workflows/test.yml
- name: Run tests
  run: dotnet test --no-build --verbosity normal
```

---

## Best Practices

1. **Naming**: `MethodName_Scenario_ExpectedBehavior`
2. **Arrange-Act-Assert**: Clear test structure
3. **One assert per test**: Keep tests focused
4. **No test interdependence**: Tests should run in any order
5. **Test edge cases**: Null, empty, boundary values
6. **Mock external dependencies**: Database, APIs, file system
7. **Use fixtures**: Share expensive setup across tests

---

## Further Reading

- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [FluentAssertions](https://fluentassertions.com/)
- [Bogus](https://github.com/bchavez/Bogus)
- [NetArchTest](https://github.com/BenMorris/NetArchTest)
