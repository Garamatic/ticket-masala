using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Domain.Data;
using TicketMasala.Web.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Xunit;

namespace TicketMasala.Tests.UnitTests.Data;

public class DbSeederTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly Mock<ILogger<DbSeeder>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly MasalaDbContext _context;
    private readonly string _tempConfigPath;

    public DbSeederTests()
    {
        // Setup Temporary Config Directory
        _tempConfigPath = Path.Combine(Path.GetTempPath(), "ticket_masala_unit_test_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempConfigPath);
        
        // Create dummy seed_data.json
        var dummySeedData = "{ \"Admins\": [], \"Employees\": [], \"Customers\": [], \"WorkContainers\": [], \"UnassignedWorkItems\": [] }";
        File.WriteAllText(Path.Combine(_tempConfigPath, "seed_data.json"), dummySeedData);

        // Set Env Var for Config Path
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", _tempConfigPath);
        TicketMasala.Web.Configuration.ConfigurationPaths.ResetCache();

        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MasalaDbContext(options);

        // Setup Mocks
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

        _mockLogger = new Mock<ILogger<DbSeeder>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();

        // Setup Environment defaults
        _mockEnvironment.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempConfigPath))
        {
            try { Directory.Delete(_tempConfigPath, true); } catch { }
        }
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", null);
        TicketMasala.Web.Configuration.ConfigurationPaths.ResetCache();
    }

    [Fact]
    public async Task SeedAsync_WithExistingUsers_SkipsSeeding()
    {
        // Arrange
        _context.Users.Add(new ApplicationUser
        {
            Id = "test-user",
            UserName = "test",
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            Phone = "555-5555"
        });
        await _context.SaveChangesAsync();

        _mockRoleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true); // Assume roles exist to skip creation logic

        var seeder = new DbSeeder(
            _context,
            _mockUserManager.Object,
            _mockRoleManager.Object,
            _mockLogger.Object,
            _mockEnvironment.Object
        );

        // Act
        await seeder.SeedAsync();

        // Assert
        // Verify CreateProjectTemplates was called
        var templateCount = await _context.ProjectTemplates.CountAsync();
        Assert.True(templateCount > 0, "Templates should be created");

        // Users should NOT increase (beyond the 1 we added)
        var userCount = await _context.Users.CountAsync();
        Assert.Equal(1, userCount);

        // Verify LogWarning about skipping
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Skipping user/project seed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenEmpty_CreatesRolesAndTemplates()
    {
        // Arrange
        var seeder = new DbSeeder(
            _context,
            _mockUserManager.Object,
            _mockRoleManager.Object,
            _mockLogger.Object,
            _mockEnvironment.Object
        );

        _mockRoleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _mockRoleManager.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);
        
        // Setup UserManager to handle CreateAsync calls (prevents NullReferenceException)
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await seeder.SeedAsync();

        // Assert
        // Roles should be created
        _mockRoleManager.Verify(r => r.CreateAsync(It.Is<IdentityRole>(role => role.Name == Constants.RoleAdmin)), Times.Once);
        _mockRoleManager.Verify(r => r.CreateAsync(It.Is<IdentityRole>(role => role.Name == Constants.RoleEmployee)), Times.Once);

        // Templates should be created
        var templateCount = await _context.ProjectTemplates.CountAsync();
        Assert.Equal(5, templateCount);
    }
}
