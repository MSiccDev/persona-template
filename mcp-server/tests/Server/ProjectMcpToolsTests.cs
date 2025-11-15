// ProjectMcpToolsTests.cs - Tests for ProjectMcpTools
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
/// Tests for ProjectMcpTools
/// </summary>
public class ProjectMcpToolsTests
{
    private readonly IProjectInstructionService _mockService;
    private readonly ProjectMcpTools _tools;

    public ProjectMcpToolsTests()
    {
        _mockService = Substitute.For<IProjectInstructionService>();
        _tools = new ProjectMcpTools(_mockService);
    }

    [Fact]
    public async Task ListProjectsAsync_ShouldReturnProjectNames()
    {
        // Arrange
        var expected = new[] { "project1", "project2", "project3" };
        _mockService.ListAvailableProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _tools.ListProjectsAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().BeEquivalentTo(expected, "should return all project names");
        await _mockService.Received(1).ListAvailableProjectsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProjectAsync_WithExistingProject_ShouldReturnJson()
    {
        // Arrange
        var project = new ProjectInstruction
        {
            Name = "test",
            Content = "Test content",
            ApplyTo = "**/*.cs",
            Description = "Test description",
            FilePath = "/path/to/test_project.instructions.md",
            SizeBytes = 100,
            LastModified = DateTime.UtcNow
        };
        _mockService.GetProjectAsync("test", Arg.Any<CancellationToken>())
            .Returns(project);

        // Act
        var result = await _tools.GetProjectAsync("test");

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("\"name\": \"test\"", "JSON should contain project name");
        result.Should().Contain("\"content\": \"Test content\"", "JSON should contain content");
        result.Should().Contain("\"applyTo\": \"**/*.cs\"", "JSON should contain applyTo");
    }

    [Fact]
    public async Task GetProjectAsync_WithNonExistentProject_ShouldReturnError()
    {
        // Arrange
        _mockService.GetProjectAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns((ProjectInstruction?)null);

        // Act
        var result = await _tools.GetProjectAsync("non-existent");

        // Assert
        result.Should().Contain("\"error\"", "should return error JSON");
        result.Should().Contain("not found", "error message should indicate not found");
    }

    [Fact]
    public async Task ProjectExistsAsync_WithExistingProject_ShouldReturnTrue()
    {
        // Arrange
        _mockService.ProjectExistsAsync("existing", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _tools.ProjectExistsAsync("existing");

        // Assert
        result.Should().BeTrue("existing project should return true");
    }

    [Fact]
    public async Task ProjectExistsAsync_WithNonExistentProject_ShouldReturnFalse()
    {
        // Arrange
        _mockService.ProjectExistsAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _tools.ProjectExistsAsync("non-existent");

        // Assert
        result.Should().BeFalse("non-existent project should return false");
    }

    [Fact]
    public async Task GetCurrentProjectAsync_WithCurrentSet_ShouldReturnJson()
    {
        // Arrange
        var project = new ProjectInstruction
        {
            Name = "current",
            Content = "Current content",
            ApplyTo = "**/*.cs",
            Description = "Current project",
            FilePath = "/path/to/current_project.instructions.md"
        };
        _mockService.GetCurrentProjectAsync(Arg.Any<CancellationToken>())
            .Returns(project);

        // Act
        var result = await _tools.GetCurrentProjectAsync();

        // Assert
        result.Should().Contain("\"current\": \"current\"", "should indicate current project");
        result.Should().Contain("\"content\": \"Current content\"", "should contain content");
    }

    [Fact]
    public async Task GetCurrentProjectAsync_WithNoCurrentSet_ShouldReturnNull()
    {
        // Arrange
        _mockService.GetCurrentProjectAsync(Arg.Any<CancellationToken>())
            .Returns((ProjectInstruction?)null);

        // Act
        var result = await _tools.GetCurrentProjectAsync();

        // Assert
        result.Should().Contain("\"current\": null", "should indicate no current project");
        result.Should().Contain("No current project configured", "should have descriptive message");
    }

    [Fact]
    public async Task SetCurrentProjectAsync_WithExistingProject_ShouldReturnSuccess()
    {
        // Arrange
        _mockService.SetCurrentProjectAsync("test", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _tools.SetCurrentProjectAsync("test");

        // Assert
        result.Should().Contain("\"success\": true", "should indicate success");
        result.Should().Contain("test", "should mention project name");
        await _mockService.Received(1).SetCurrentProjectAsync("test", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCurrentProjectAsync_WithNonExistentProject_ShouldReturnError()
    {
        // Arrange
        _mockService.SetCurrentProjectAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new FileNotFoundException()));

        // Act
        var result = await _tools.SetCurrentProjectAsync("non-existent");

        // Assert
        result.Should().Contain("\"error\"", "should return error JSON");
        result.Should().Contain("not found", "error should indicate not found");
    }

    [Fact]
    public void InvalidateCacheAsync_WithSpecificProject_ShouldInvalidateCache()
    {
        // Act
        var result = _tools.InvalidateCacheAsync("test");

        // Assert
        result.Should().Contain("\"success\": true", "should indicate success");
        result.Should().Contain("test", "should mention project name");
        _mockService.Received(1).InvalidateCache("test");
    }

    [Fact]
    public void InvalidateCacheAsync_WithNull_ShouldInvalidateAllCache()
    {
        // Act
        var result = _tools.InvalidateCacheAsync(null);

        // Assert
        result.Should().Contain("\"success\": true", "should indicate success");
        result.Should().Contain("all projects", "should indicate all projects");
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
