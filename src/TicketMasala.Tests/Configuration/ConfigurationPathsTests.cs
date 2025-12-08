using Xunit;
using TicketMasala.Web.Configuration;

namespace TicketMasala.Tests.Configuration;

public class ConfigurationPathsTests : IDisposable
{
    private readonly string _originalEnvVar;
    private readonly string _testContentRoot;

    public ConfigurationPathsTests()
    {
        // Save original environment variable
        _originalEnvVar = Environment.GetEnvironmentVariable("MASALA_CONFIG_PATH") ?? string.Empty;
        
        // Create a test content root directory
        _testContentRoot = Path.Combine(Path.GetTempPath(), "ticket-masala-test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testContentRoot);
        
        // Reset cache before each test
        ConfigurationPaths.ResetCache();
    }

    public void Dispose()
    {
        // Restore original environment variable
        if (string.IsNullOrEmpty(_originalEnvVar))
        {
            Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", null);
        }
        else
        {
            Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", _originalEnvVar);
        }
        
        // Clean up test directory
        if (Directory.Exists(_testContentRoot))
        {
            Directory.Delete(_testContentRoot, true);
        }
        
        // Reset cache after each test
        ConfigurationPaths.ResetCache();
    }

    [Fact]
    public void GetConfigBasePath_WithEnvironmentVariable_ReturnsEnvVarPath()
    {
        // Arrange
        var customPath = Path.Combine(_testContentRoot, "custom-config");
        Directory.CreateDirectory(customPath);
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", customPath);

        // Act
        var result = ConfigurationPaths.GetConfigBasePath(_testContentRoot);

        // Assert
        Assert.Equal(customPath, result);
    }

    [Fact]
    public void GetConfigBasePath_WithDockerPath_ReturnsDockerPath()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", null);
        var dockerPath = "/app/config";
        
        // Skip this test if not running in Docker environment
        if (!Directory.Exists(dockerPath))
        {
            return;
        }

        // Act
        var result = ConfigurationPaths.GetConfigBasePath(_testContentRoot);

        // Assert
        Assert.Equal(dockerPath, result);
    }

    [Fact]
    public void GetConfigBasePath_WithDevelopmentPath_ReturnsDevelopmentPath()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", null);
        var expectedPath = Path.Combine(_testContentRoot, "..", "..", "config");

        // Act
        var result = ConfigurationPaths.GetConfigBasePath(_testContentRoot);

        // Assert
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void GetConfigBasePath_CachesResult()
    {
        // Arrange
        var customPath = Path.Combine(_testContentRoot, "custom-config");
        Directory.CreateDirectory(customPath);
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", customPath);

        // Act
        var result1 = ConfigurationPaths.GetConfigBasePath(_testContentRoot);
        
        // Change environment variable (should not affect cached result)
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", "/different/path");
        var result2 = ConfigurationPaths.GetConfigBasePath(_testContentRoot);

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(customPath, result2);
    }

    [Fact]
    public void GetConfigFilePath_ReturnsCorrectPath()
    {
        // Arrange
        var customPath = Path.Combine(_testContentRoot, "custom-config");
        Directory.CreateDirectory(customPath);
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", customPath);
        var fileName = "masala_config.json";

        // Act
        var result = ConfigurationPaths.GetConfigFilePath(_testContentRoot, fileName);

        // Assert
        var expectedPath = Path.Combine(customPath, fileName);
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void GetConfigFilePath_WithMultipleFiles_ReturnsCorrectPaths()
    {
        // Arrange
        var customPath = Path.Combine(_testContentRoot, "custom-config");
        Directory.CreateDirectory(customPath);
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", customPath);

        // Act
        var configPath = ConfigurationPaths.GetConfigFilePath(_testContentRoot, "masala_config.json");
        var domainsPath = ConfigurationPaths.GetConfigFilePath(_testContentRoot, "masala_domains.yaml");
        var seedPath = ConfigurationPaths.GetConfigFilePath(_testContentRoot, "seed_data.json");

        // Assert
        Assert.Equal(Path.Combine(customPath, "masala_config.json"), configPath);
        Assert.Equal(Path.Combine(customPath, "masala_domains.yaml"), domainsPath);
        Assert.Equal(Path.Combine(customPath, "seed_data.json"), seedPath);
    }

    [Fact]
    public void ResetCache_ClearsCache()
    {
        // Arrange
        var customPath1 = Path.Combine(_testContentRoot, "custom-config-1");
        var customPath2 = Path.Combine(_testContentRoot, "custom-config-2");
        Directory.CreateDirectory(customPath1);
        Directory.CreateDirectory(customPath2);
        
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", customPath1);
        var result1 = ConfigurationPaths.GetConfigBasePath(_testContentRoot);

        // Act
        ConfigurationPaths.ResetCache();
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", customPath2);
        var result2 = ConfigurationPaths.GetConfigBasePath(_testContentRoot);

        // Assert
        Assert.Equal(customPath1, result1);
        Assert.Equal(customPath2, result2);
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void GetConfigBasePath_WithNonExistentEnvVarPath_FallsBackToDevelopment()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testContentRoot, "does-not-exist");
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", nonExistentPath);
        var expectedPath = Path.Combine(_testContentRoot, "..", "..", "config");

        // Act
        var result = ConfigurationPaths.GetConfigBasePath(_testContentRoot);

        // Assert
        Assert.Equal(expectedPath, result);
    }

    [Theory]
    [InlineData("masala_config.json")]
    [InlineData("masala_domains.yaml")]
    [InlineData("seed_data.json")]
    [InlineData("custom_file.txt")]
    public void GetConfigFilePath_WithVariousFileNames_ReturnsCorrectPaths(string fileName)
    {
        // Arrange
        var customPath = Path.Combine(_testContentRoot, "config");
        Directory.CreateDirectory(customPath);
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", customPath);

        // Act
        var result = ConfigurationPaths.GetConfigFilePath(_testContentRoot, fileName);

        // Assert
        Assert.Equal(Path.Combine(customPath, fileName), result);
        Assert.EndsWith(fileName, result);
    }
}
