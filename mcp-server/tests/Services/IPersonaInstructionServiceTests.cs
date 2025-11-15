// IPersonaInstructionServiceTests.cs - Interface Contract Tests
// Validates the behavior contract of IPersonaInstructionService implementations

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
/// Tests for IPersonaInstructionService interface contract
/// These tests validate that any implementation follows the expected behavior
/// </summary>
public class IPersonaInstructionServiceTests
{
    [Fact]
    public async Task GetPersonaAsync_WithValidName_ShouldReturnPersonaInstruction()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        var expectedPersona = new PersonaInstruction
        {
            Name = "test-persona",
            FilePath = "/path/to/test-persona.instructions.md",
            Content = "# Test Persona Content",
            LastModified = DateTime.UtcNow,
            SizeBytes = 100
        };
        
        service.GetPersonaAsync("test-persona", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PersonaInstruction?>(expectedPersona));
        
        // Act
        var result = await service.GetPersonaAsync("test-persona");
        
        // Assert
        result.Should().NotBeNull("service should return a persona for valid name");
        result!.Name.Should().Be("test-persona", "returned persona should have correct name");
    }
    
    [Fact]
    public async Task GetPersonaAsync_WithInvalidName_ShouldReturnNull()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        service.GetPersonaAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PersonaInstruction?>(null));
        
        // Act
        var result = await service.GetPersonaAsync("non-existent");
        
        // Assert
        result.Should().BeNull("service should return null for non-existent persona");
    }
    
    [Fact]
    public async Task ListAvailablePersonasAsync_ShouldReturnCollection()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        var expectedPersonas = new[] { "persona1", "persona2", "persona3" };
        service.ListAvailablePersonasAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(expectedPersonas));
        
        // Act
        var result = await service.ListAvailablePersonasAsync();
        
        // Assert
        result.Should().NotBeNull("service should return a collection");
        result.Should().HaveCount(3, "service should return all available personas");
    }
    
    [Fact]
    public async Task PersonaExistsAsync_WithExistingPersona_ShouldReturnTrue()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        service.PersonaExistsAsync("existing-persona", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        
        // Act
        var result = await service.PersonaExistsAsync("existing-persona");
        
        // Assert
        result.Should().BeTrue("service should return true for existing persona");
    }
    
    [Fact]
    public async Task PersonaExistsAsync_WithNonExistingPersona_ShouldReturnFalse()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        service.PersonaExistsAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        
        // Act
        var result = await service.PersonaExistsAsync("non-existent");
        
        // Assert
        result.Should().BeFalse("service should return false for non-existing persona");
    }
    
    [Fact]
    public void InvalidateCache_WithSpecificPersona_ShouldNotThrow()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        
        // Act & Assert
        var exception = Record.Exception(() => service.InvalidateCache("test-persona"));
        exception.Should().BeNull("cache invalidation should not throw exceptions");
    }
    
    [Fact]
    public void InvalidateCache_WithNull_ShouldClearEntireCache()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        
        // Act & Assert
        var exception = Record.Exception(() => service.InvalidateCache(null));
        exception.Should().BeNull("cache invalidation with null should not throw exceptions");
    }
    
    [Fact]
    public async Task RefreshCacheAsync_ShouldComplete()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        service.RefreshCacheAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        // Act & Assert
        var exception = await Record.ExceptionAsync(() => service.RefreshCacheAsync());
        exception.Should().BeNull("cache refresh should complete without exceptions");
    }
    
    [Fact]
    public async Task GetCurrentPersonaAsync_WhenConfigured_ShouldReturnPersona()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        var currentPersona = new PersonaInstruction
        {
            Name = "current",
            FilePath = "/path/to/current.instructions.md",
            Content = "# Current Persona",
            LastModified = DateTime.UtcNow,
            SizeBytes = 50
        };
        
        service.GetCurrentPersonaAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PersonaInstruction?>(currentPersona));
        
        // Act
        var result = await service.GetCurrentPersonaAsync();
        
        // Assert
        result.Should().NotBeNull("service should return current persona when configured");
    }
    
    [Fact]
    public async Task GetCurrentPersonaAsync_WhenNotConfigured_ShouldReturnNull()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        service.GetCurrentPersonaAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PersonaInstruction?>(null));
        
        // Act
        var result = await service.GetCurrentPersonaAsync();
        
        // Assert
        result.Should().BeNull("service should return null when no current persona is configured");
    }
    
    [Fact]
    public async Task SetCurrentPersonaAsync_WithValidName_ShouldComplete()
    {
        // Arrange
        var service = Substitute.For<IPersonaInstructionService>();
        service.SetCurrentPersonaAsync("new-current", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        // Act & Assert
        var exception = await Record.ExceptionAsync(() => service.SetCurrentPersonaAsync("new-current"));
        exception.Should().BeNull("setting current persona should complete without exceptions");
    }
}
