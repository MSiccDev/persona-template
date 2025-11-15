// PersonaInstructionServiceTests.cs - Implementation Tests
// Tests for PersonaInstructionService with real file I/O and caching behavior

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PersonaMcpServer.Services;
using PersonaMcpServer.Models;

namespace PersonaMcpServer.Tests.Services;

/// <summary>
/// Tests for PersonaInstructionService implementation
/// </summary>
public class PersonaInstructionServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly PersonaServerConfig _config;
    private readonly ILogger<PersonaInstructionService> _logger;
    private readonly PersonaInstructionService _service;

    public PersonaInstructionServiceTests()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"persona_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _config = new PersonaServerConfig
        {
            PersonaRepoPath = _testDirectory,
            CacheTtlSeconds = 300,
            MaxCacheSizeBytes = 1024 * 1024 // 1MB
        };

        _logger = Substitute.For<ILogger<PersonaInstructionService>>();
        var options = Options.Create(_config);
        _service = new PersonaInstructionService(options, _logger);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task GetPersonaAsync_WithExistingFile_ShouldLoadAndCache()
    {
        // Arrange
        var personaName = "test";
        var content = "# Test Persona\nThis is a test persona.";
        CreateTestPersonaFile(personaName, content);

        // Act
        var result = await _service.GetPersonaAsync(personaName);

        // Assert
        result.Should().NotBeNull("persona should be loaded from file");
        result!.Name.Should().Be(personaName, "persona name should match");
        result.Content.Should().Be(content, "content should match file content");
        result.FilePath.Should().Contain(personaName, "file path should contain persona name");
        result.SizeBytes.Should().BeGreaterThan(0, "file size should be tracked");

        // Second call should hit cache
        var cachedResult = await _service.GetPersonaAsync(personaName);
        cachedResult.Should().BeSameAs(result, "second call should return cached instance");
    }

    [Fact]
    public async Task GetPersonaAsync_WithYamlFrontmatter_ShouldParseMetadata()
    {
        // Arrange
        var personaName = "yaml-test";
        var content = @"---
applyTo: '**/*.cs'
description: 'Test persona with YAML'
---
# Test Persona
Content here.";
        CreateTestPersonaFile(personaName, content);

        // Act
        var result = await _service.GetPersonaAsync(personaName);

        // Assert
        result.Should().NotBeNull("persona should be loaded");
        result!.ApplyTo.Should().Be("**/*.cs", "applyTo should be parsed from YAML");
        result.Description.Should().Be("Test persona with YAML", "description should be parsed from YAML");
    }

    [Fact]
    public async Task GetPersonaAsync_WithNonExistentFile_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetPersonaAsync("non-existent");

        // Assert
        result.Should().BeNull("non-existent persona should return null");
    }

    [Fact]
    public async Task GetPersonaAsync_WithNullOrEmptyName_ShouldReturnNull()
    {
        // Act & Assert
        var result1 = await _service.GetPersonaAsync(null!);
        result1.Should().BeNull("null persona name should return null");

        var result2 = await _service.GetPersonaAsync("");
        result2.Should().BeNull("empty persona name should return null");

        var result3 = await _service.GetPersonaAsync("   ");
        result3.Should().BeNull("whitespace persona name should return null");
    }

    [Fact]
    public async Task ListAvailablePersonasAsync_ShouldReturnAllPersonaFiles()
    {
        // Arrange
        CreateTestPersonaFile("persona1", "Content 1");
        CreateTestPersonaFile("persona2", "Content 2");
        CreateTestPersonaFile("persona3", "Content 3");

        // Act
        var result = await _service.ListAvailablePersonasAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().HaveCount(3, "should find all 3 persona files");
        result.Should().Contain("persona1", "should include persona1");
        result.Should().Contain("persona2", "should include persona2");
        result.Should().Contain("persona3", "should include persona3");
    }

    [Fact]
    public async Task ListAvailablePersonasAsync_WithNonExistentDirectory_ShouldReturnEmpty()
    {
        // Arrange
        _config.PersonaRepoPath = "/non/existent/path";

        // Act
        var result = await _service.ListAvailablePersonasAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().BeEmpty("non-existent directory should return empty list");
    }

    [Fact]
    public async Task PersonaExistsAsync_WithExistingPersona_ShouldReturnTrue()
    {
        // Arrange
        CreateTestPersonaFile("existing", "Content");

        // Act
        var result = await _service.PersonaExistsAsync("existing");

        // Assert
        result.Should().BeTrue("existing persona should return true");
    }

    [Fact]
    public async Task PersonaExistsAsync_WithNonExistentPersona_ShouldReturnFalse()
    {
        // Act
        var result = await _service.PersonaExistsAsync("non-existent");

        // Assert
        result.Should().BeFalse("non-existent persona should return false");
    }

    [Fact]
    public async Task InvalidateCache_WithSpecificPersona_ShouldRemoveFromCache()
    {
        // Arrange
        CreateTestPersonaFile("cached", "Content");
        var first = await _service.GetPersonaAsync("cached");

        // Act
        _service.InvalidateCache("cached");
        var second = await _service.GetPersonaAsync("cached");

        // Assert
        first.Should().NotBeNull("first load should succeed");
        second.Should().NotBeNull("second load should succeed");
        second.Should().NotBeSameAs(first, "cache invalidation should force reload");
    }

    [Fact]
    public async Task InvalidateCache_WithNull_ShouldClearEntireCache()
    {
        // Arrange
        CreateTestPersonaFile("persona1", "Content 1");
        CreateTestPersonaFile("persona2", "Content 2");
        await _service.GetPersonaAsync("persona1");
        await _service.GetPersonaAsync("persona2");

        // Act
        _service.InvalidateCache(null);
        var reloaded1 = await _service.GetPersonaAsync("persona1");
        var reloaded2 = await _service.GetPersonaAsync("persona2");

        // Assert
        reloaded1.Should().NotBeNull("persona1 should be reloaded");
        reloaded2.Should().NotBeNull("persona2 should be reloaded");
    }

    [Fact]
    public async Task RefreshCacheAsync_ShouldPreloadAllPersonas()
    {
        // Arrange
        CreateTestPersonaFile("persona1", "Content 1");
        CreateTestPersonaFile("persona2", "Content 2");

        // Act
        await _service.RefreshCacheAsync();

        // Assert - both should be cached, so second calls should be instant
        var result1 = await _service.GetPersonaAsync("persona1");
        var result2 = await _service.GetPersonaAsync("persona2");

        result1.Should().NotBeNull("persona1 should be cached");
        result2.Should().NotBeNull("persona2 should be cached");
    }

    [Fact]
    public async Task GetCurrentPersonaAsync_WithNoCurrentConfigured_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetCurrentPersonaAsync();

        // Assert
        result.Should().BeNull("no current persona configured should return null");
    }

    [Fact]
    public async Task GetCurrentPersonaAsync_WithCurrentConfigured_ShouldReturnPersona()
    {
        // Arrange
        CreateTestPersonaFile("current", "Current persona content");
        _config.CurrentPersona = "current";

        // Act
        var result = await _service.GetCurrentPersonaAsync();

        // Assert
        result.Should().NotBeNull("current persona should be returned");
        result!.Name.Should().Be("current", "should return configured current persona");
    }

    [Fact]
    public async Task SetCurrentPersonaAsync_WithExistingPersona_ShouldUpdateConfig()
    {
        // Arrange
        CreateTestPersonaFile("new-current", "Content");

        // Act
        await _service.SetCurrentPersonaAsync("new-current");

        // Assert
        _config.CurrentPersona.Should().Be("new-current", "current persona should be updated");
    }

    [Fact]
    public async Task SetCurrentPersonaAsync_WithNonExistentPersona_ShouldThrow()
    {
        // Act & Assert
        var exception = await Record.ExceptionAsync(() => 
            _service.SetCurrentPersonaAsync("non-existent"));

        exception.Should().NotBeNull("should throw exception");
        exception.Should().BeOfType<FileNotFoundException>("should throw FileNotFoundException");
    }

    [Fact]
    public async Task SetCurrentPersonaAsync_WithNullOrEmpty_ShouldThrow()
    {
        // Act & Assert
        var exception1 = await Record.ExceptionAsync(() => 
            _service.SetCurrentPersonaAsync(null!));
        exception1.Should().BeOfType<ArgumentException>("null should throw ArgumentException");

        var exception2 = await Record.ExceptionAsync(() => 
            _service.SetCurrentPersonaAsync(""));
        exception2.Should().BeOfType<ArgumentException>("empty should throw ArgumentException");
    }

    [Fact]
    public async Task CacheExpiration_AfterTtl_ShouldReloadFromFile()
    {
        // Arrange
        _config.CacheTtlSeconds = 1; // 1 second TTL
        CreateTestPersonaFile("expiring", "Original content");
        
        var first = await _service.GetPersonaAsync("expiring");
        
        // Wait for cache to expire
        await Task.Delay(1100);
        
        // Update file content
        var filePath = Path.Combine(_testDirectory, "expiring_persona.instructions.md");
        await File.WriteAllTextAsync(filePath, "Updated content");

        // Act
        var second = await _service.GetPersonaAsync("expiring");

        // Assert
        first.Should().NotBeNull("first load should succeed");
        second.Should().NotBeNull("second load should succeed");
        second!.Content.Should().Be("Updated content", "cache expiration should reload from file");
    }

    private void CreateTestPersonaFile(string personaName, string content)
    {
        var fileName = $"{personaName}_persona.instructions.md";
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content);
    }
}
