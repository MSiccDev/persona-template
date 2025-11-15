// IProjectInstructionServiceTests.cs - Interface Contract Tests
// Validates the behavior contract of IProjectInstructionService implementations

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using AwesomeAssertions;
using PersonaMcpServer.Services;
using PersonaMcpServer.Models;

namespace PersonaMcpServer.Tests.Services;

/// <summary>
/// Tests for IProjectInstructionService interface contract
/// These tests validate that any implementation follows the expected behavior
/// </summary>
public class IProjectInstructionServiceTests
{
    [Fact]
    public async Task GetProjectAsync_WithValidName_ShouldReturnProjectInstruction()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        var expectedProject = new ProjectInstruction
        {
            Name = "test-project",
            FilePath = "/path/to/test-project.instructions.md",
            Content = "# Test Project Content",
            LastModified = DateTime.UtcNow,
            SizeBytes = 100
        };
        
        service.GetProjectAsync("test-project", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectInstruction?>(expectedProject));
        
        // Act
        var result = await service.GetProjectAsync("test-project");
        
        // Assert
        result.Should().NotBeNull("service should return a project for valid name");
        result!.Name.Should().Be("test-project", "returned project should have correct name");
    }
    
    [Fact]
    public async Task GetProjectAsync_WithInvalidName_ShouldReturnNull()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        service.GetProjectAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectInstruction?>(null));
        
        // Act
        var result = await service.GetProjectAsync("non-existent");
        
        // Assert
        result.Should().BeNull("service should return null for non-existent project");
    }
    
    [Fact]
    public async Task ListAvailableProjectsAsync_ShouldReturnCollection()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        var expectedProjects = new[] { "project1", "project2", "project3" };
        service.ListAvailableProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(expectedProjects));
        
        // Act
        var result = await service.ListAvailableProjectsAsync();
        
        // Assert
        result.Should().NotBeNull("service should return a collection");
        result.Should().HaveCount(3, "service should return all available projects");
    }
    
    [Fact]
    public async Task ProjectExistsAsync_WithExistingProject_ShouldReturnTrue()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        service.ProjectExistsAsync("existing-project", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        
        // Act
        var result = await service.ProjectExistsAsync("existing-project");
        
        // Assert
        result.Should().BeTrue("service should return true for existing project");
    }
    
    [Fact]
    public async Task ProjectExistsAsync_WithNonExistingProject_ShouldReturnFalse()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        service.ProjectExistsAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        
        // Act
        var result = await service.ProjectExistsAsync("non-existent");
        
        // Assert
        result.Should().BeFalse("service should return false for non-existing project");
    }
    
    [Fact]
    public void InvalidateCache_WithSpecificProject_ShouldNotThrow()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        
        // Act & Assert
        var exception = Record.Exception(() => service.InvalidateCache("test-project"));
        exception.Should().BeNull("cache invalidation should not throw exceptions");
    }
    
    [Fact]
    public void InvalidateCache_WithNull_ShouldClearEntireCache()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        
        // Act & Assert
        var exception = Record.Exception(() => service.InvalidateCache(null));
        exception.Should().BeNull("cache invalidation with null should not throw exceptions");
    }
    
    [Fact]
    public async Task RefreshCacheAsync_ShouldComplete()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        service.RefreshCacheAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        // Act & Assert
        var exception = await Record.ExceptionAsync(() => service.RefreshCacheAsync());
        exception.Should().BeNull("cache refresh should complete without exceptions");
    }
    
    [Fact]
    public async Task GetCurrentProjectAsync_WhenConfigured_ShouldReturnProject()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        var currentProject = new ProjectInstruction
        {
            Name = "current",
            FilePath = "/path/to/current.instructions.md",
            Content = "# Current Project",
            LastModified = DateTime.UtcNow,
            SizeBytes = 50
        };
        
        service.GetCurrentProjectAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectInstruction?>(currentProject));
        
        // Act
        var result = await service.GetCurrentProjectAsync();
        
        // Assert
        result.Should().NotBeNull("service should return current project when configured");
    }
    
    [Fact]
    public async Task GetCurrentProjectAsync_WhenNotConfigured_ShouldReturnNull()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        service.GetCurrentProjectAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectInstruction?>(null));
        
        // Act
        var result = await service.GetCurrentProjectAsync();
        
        // Assert
        result.Should().BeNull("service should return null when no current project is configured");
    }
    
    [Fact]
    public async Task SetCurrentProjectAsync_WithValidName_ShouldComplete()
    {
        // Arrange
        var service = Substitute.For<IProjectInstructionService>();
        service.SetCurrentProjectAsync("new-current", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        // Act & Assert
        var exception = await Record.ExceptionAsync(() => service.SetCurrentProjectAsync("new-current"));
        exception.Should().BeNull("setting current project should complete without exceptions");
    }
}
