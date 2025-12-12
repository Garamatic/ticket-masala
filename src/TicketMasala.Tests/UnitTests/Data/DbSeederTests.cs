using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Web.Data;
using TicketMasala.Web.Models;
using TicketMasala.Web.Utilities;
using Xunit;

namespace TicketMasala.Tests.UnitTests.Data;

public class DbSeederTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly Mock<ILogger<DbSeeder>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly MasalaDbContext _context;

    public DbSeederTests()
    {
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
        // Verify CreateProjectTemplates was called (it runs before user check? No, looks like it runs after EnsureCreated but BEFORE user check return?)
        // Let's check the code order in DbSeeder.
        // It runs EnsureRoles, then checks users.
        // Wait, line 99: Create Project Templates is called.
        // Line 103: if (userCount > 0) return.

        // So Templates SHOULD be created even if users exist? 
        // Logic in DbSeeder:
        // 99: Create Project Templates
        // 101: await CreateProjectTemplates();
        // 103: if (userCount > 0) ... return;

        // So templates are created regardless. 
        var templateCount = await _context.ProjectTemplates.CountAsync();
        Assert.True(templateCount > 0, "Templates should be created");

        // Users should NOT increase (beyond the 1 we added)
        // Check if LoadSeedConfigurationAsync was called? We can't verify private method easily, 
        // but we can verify side effects (no new users).
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

        // Act
        // This will likely fail at LoadSeedConfigurationAsync if the file is missing in pure unit test env.
        // We need to ensure we can load or mock the file loading.
        // Since LoadSeedConfigurationAsync reads a FILE, verification is tricky in unit test.
        // But we can rely on "Create Project Templates" part passing.

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
