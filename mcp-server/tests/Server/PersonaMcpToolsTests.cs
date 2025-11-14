// PersonaMcpToolsTests.cs - Tests for PersonaMcpTools
// Tests for MCP tool methods with mocked dependencies

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using AwesomeAssertions;
using PersonaMcpServer.Models;
using PersonaMcpServer.Services;
using PersonaMcpServer.Server;

namespace PersonaMcpServer.Tests.Server;

/// <summary>
/// Tests for PersonaMcpTools
/// </summary>
public class PersonaMcpToolsTests
{
    private readonly IPersonaInstructionService _mockService;
    private readonly PersonaMcpTools _tools;

    public PersonaMcpToolsTests()
    {
        _mockService = Substitute.For<IPersonaInstructionService>();
        _tools = new PersonaMcpTools(_mockService);
    }

    [Fact]
    public async Task ListPersonasAsync_ShouldReturnPersonaNames()
    {
        // Arrange
        var expected = new[] { "persona1", "persona2", "persona3" };
        _mockService.ListAvailablePersonasAsync(Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _tools.ListPersonasAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().BeEquivalentTo(expected, "should return all persona names");
        await _mockService.Received(1).ListAvailablePersonasAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPersonaAsync_WithExistingPersona_ShouldReturnJson()
    {
        // Arrange
        var persona = new PersonaInstruction
        {
            Name = "test",
            Content = "Test content",
            ApplyTo = "**/*.cs",
            Description = "Test description",
            FilePath = "/path/to/test_persona.instructions.md",
            SizeBytes = 100,
            LastModified = DateTime.UtcNow
        };
        _mockService.GetPersonaAsync("test", Arg.Any<CancellationToken>())
            .Returns(persona);

        // Act
        var result = await _tools.GetPersonaAsync("test");

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("\"name\": \"test\"", "JSON should contain persona name");
        result.Should().Contain("\"content\": \"Test content\"", "JSON should contain content");
        result.Should().Contain("\"applyTo\": \"**/*.cs\"", "JSON should contain applyTo");
    }

    [Fact]
    public async Task GetPersonaAsync_WithNonExistentPersona_ShouldReturnError()
    {
        // Arrange
        _mockService.GetPersonaAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns((PersonaInstruction?)null);

        // Act
        var result = await _tools.GetPersonaAsync("non-existent");

        // Assert
        result.Should().Contain("\"error\"", "should return error JSON");
        result.Should().Contain("not found", "error message should indicate not found");
    }

    [Fact]
    public async Task PersonaExistsAsync_WithExistingPersona_ShouldReturnTrue()
    {
        // Arrange
        _mockService.PersonaExistsAsync("existing", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _tools.PersonaExistsAsync("existing");

        // Assert
        result.Should().BeTrue("existing persona should return true");
    }

    [Fact]
    public async Task PersonaExistsAsync_WithNonExistentPersona_ShouldReturnFalse()
    {
        // Arrange
        _mockService.PersonaExistsAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _tools.PersonaExistsAsync("non-existent");

        // Assert
        result.Should().BeFalse("non-existent persona should return false");
    }

    [Fact]
    public async Task GetCurrentPersonaAsync_WithCurrentSet_ShouldReturnJson()
    {
        // Arrange
        var persona = new PersonaInstruction
        {
            Name = "current",
            Content = "Current content",
            ApplyTo = "**/*.cs",
            Description = "Current persona",
            FilePath = "/path/to/current_persona.instructions.md"
        };
        _mockService.GetCurrentPersonaAsync(Arg.Any<CancellationToken>())
            .Returns(persona);

        // Act
        var result = await _tools.GetCurrentPersonaAsync();

        // Assert
        result.Should().Contain("\"current\": \"current\"", "should indicate current persona");
        result.Should().Contain("\"content\": \"Current content\"", "should contain content");
    }

    [Fact]
    public async Task GetCurrentPersonaAsync_WithNoCurrentSet_ShouldReturnNull()
    {
        // Arrange
        _mockService.GetCurrentPersonaAsync(Arg.Any<CancellationToken>())
            .Returns((PersonaInstruction?)null);

        // Act
        var result = await _tools.GetCurrentPersonaAsync();

        // Assert
        result.Should().Contain("\"current\": null", "should indicate no current persona");
        result.Should().Contain("No current persona configured", "should have descriptive message");
    }

    [Fact]
    public async Task SetCurrentPersonaAsync_WithExistingPersona_ShouldReturnSuccess()
    {
        // Arrange
        _mockService.SetCurrentPersonaAsync("test", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _tools.SetCurrentPersonaAsync("test");

        // Assert
        result.Should().Contain("\"success\": true", "should indicate success");
        result.Should().Contain("test", "should mention persona name");
        await _mockService.Received(1).SetCurrentPersonaAsync("test", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCurrentPersonaAsync_WithNonExistentPersona_ShouldReturnError()
    {
        // Arrange
        _mockService.SetCurrentPersonaAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new FileNotFoundException()));

        // Act
        var result = await _tools.SetCurrentPersonaAsync("non-existent");

        // Assert
        result.Should().Contain("\"error\"", "should return error JSON");
        result.Should().Contain("not found", "error should indicate not found");
    }

    [Fact]
    public void InvalidateCacheAsync_WithSpecificPersona_ShouldInvalidateCache()
    {
        // Act
        var result = _tools.InvalidateCacheAsync("test");

        // Assert
        result.Should().Contain("\"success\": true", "should indicate success");
        result.Should().Contain("test", "should mention persona name");
        _mockService.Received(1).InvalidateCache("test");
    }

    [Fact]
    public void InvalidateCacheAsync_WithNull_ShouldInvalidateAllCache()
    {
        // Act
        var result = _tools.InvalidateCacheAsync(null);

        // Assert
        result.Should().Contain("\"success\": true", "should indicate success");
        result.Should().Contain("all personas", "should indicate all personas");
        _mockService.Received(1).InvalidateCache(null);
    }

    [Fact]
    public async Task RefreshCacheAsync_ShouldRefreshCache()
    {
        // Arrange
        _mockService.RefreshCacheAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _tools.RefreshCacheAsync();

        // Assert
        result.Should().Contain("\"success\": true", "should indicate success");
        result.Should().Contain("refreshed", "should indicate cache refresh");
        await _mockService.Received(1).RefreshCacheAsync(Arg.Any<CancellationToken>());
    }
}
