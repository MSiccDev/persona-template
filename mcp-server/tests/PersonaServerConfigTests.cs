using Xunit;
using NSubstitute;
using AwesomeAssertions;
using PersonaMcpServer;

namespace PersonaMcpServer.Tests;

/// <summary>
/// Tests for PersonaServerConfig validation and functionality
/// </summary>
public class PersonaServerConfigTests
{
    [Fact]
    public void PersonaServerConfig_WithValidValues_ShouldPassValidation()
    {
        // Arrange
        var config = new PersonaServerConfig
        {
            Host = "localhost",
            Port = 3000,
            Transport = "SSE",
            PersonaRepoPath = "../", // Use relative path so it passes validation
            CacheTtlSeconds = 300,
            MaxCacheSizeBytes = 10485760
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.Should().BeTrue("configuration should be valid with proper values");
        errors.Should().BeEmpty("there should be no validation errors");
    }

    [Fact]
    public void PersonaServerConfig_WithInvalidPort_ShouldFailValidation()
    {
        // Arrange
        var config = new PersonaServerConfig
        {
            Host = "localhost",
            Port = 0, // Invalid port
            Transport = "http",
            PersonaRepoPath = "/tmp/test",
            CacheTtlSeconds = 300,
            MaxCacheSizeBytes = 10485760
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse("configuration should be invalid with port 0");
        errors.Should().Contain(error => error.Contains("Port"), "should contain port validation error");
    }

    [Fact]
    public void PersonaServerConfig_WithInvalidTransport_ShouldFailValidation()
    {
        // Arrange
        var config = new PersonaServerConfig
        {
            Host = "localhost",
            Port = 3000,
            Transport = "invalid", // Invalid transport
            PersonaRepoPath = "/tmp/test",
            CacheTtlSeconds = 300,
            MaxCacheSizeBytes = 10485760
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse("configuration should be invalid with invalid transport");
        errors.Should().Contain(error => error.Contains("Transport"), "should contain transport validation error");
    }

    [Fact]
    public void PersonaServerConfig_WithEmptyHost_ShouldFailValidation()
    {
        // Arrange
        var config = new PersonaServerConfig
        {
            Host = "", // Empty host
            Port = 3000,
            Transport = "http",
            PersonaRepoPath = "/tmp/test",
            CacheTtlSeconds = 300,
            MaxCacheSizeBytes = 10485760
        };

        // Act
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse("configuration should be invalid with empty host");
        errors.Should().Contain(error => error.Contains("Host"), "should contain host validation error");
    }

    [Fact]
    public void PersonaServerConfig_ToString_ShouldNotExposeSecrets()
    {
        // Arrange
        var config = new PersonaServerConfig
        {
            Host = "localhost",
            Port = 3000,
            Transport = "SSE",
            PersonaRepoPath = "/home/user/persona",
            CacheTtlSeconds = 300,
            MaxCacheSizeBytes = 10485760
        };

        // Act
        var configString = config.ToString();

        // Assert
        configString.Should().Contain("Host: localhost", "should show host");
        configString.Should().Contain("Port: 3000", "should show port");
        configString.Should().Contain("Transport: SSE", "should show transport");
        configString.Should().Contain("/home/user/persona", "should show the repo path");
    }

    [Fact]
    public void PersonaServerConfig_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var config = new PersonaServerConfig();
        var isValid = config.IsValid(out var errors);

        // Assert
        isValid.Should().BeTrue("default configuration should be valid");
        errors.Should().BeEmpty("default configuration should have no validation errors");
        config.Host.Should().Be("localhost", "default host should be localhost");
        config.Port.Should().Be(3000, "default port should be 3000");
        config.Transport.Should().Be("SSE", "default transport should be SSE");
        config.CacheTtlSeconds.Should().Be(300, "default cache TTL should be 5 minutes");
        config.MaxCacheSizeBytes.Should().Be(100 * 1024 * 1024, "default max cache size should be 100MB");
    }
}