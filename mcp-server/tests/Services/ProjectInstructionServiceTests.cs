// ProjectInstructionServiceTests.cs - Implementation Tests
// Tests for ProjectInstructionService with real file I/O and caching behavior

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
/// Tests for ProjectInstructionService implementation
/// </summary>
public class ProjectInstructionServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly PersonaServerConfig _config;
    private readonly ILogger<ProjectInstructionService> _logger;
    private readonly ProjectInstructionService _service;

    public ProjectInstructionServiceTests()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"project_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _config = new PersonaServerConfig
        {
            PersonaRepoPath = _testDirectory,
            CacheTtlSeconds = 300,
            MaxCacheSizeBytes = 1024 * 1024 // 1MB
        };

        _logger = Substitute.For<ILogger<ProjectInstructionService>>();
        var options = Options.Create(_config);
        _service = new ProjectInstructionService(options, _logger);
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
    public async Task GetProjectAsync_WithExistingFile_ShouldLoadAndCache()
    {
        // Arrange
        var projectName = "test";
        var content = "# Test Project\nThis is a test project.";
        CreateTestProjectFile(projectName, content);

        // Act
        var result = await _service.GetProjectAsync(projectName);

        // Assert
        result.Should().NotBeNull("project should be loaded from file");
        result!.Name.Should().Be(projectName, "project name should match");
        result.Content.Should().Be(content, "content should match file content");
        result.FilePath.Should().Contain(projectName, "file path should contain project name");
        result.SizeBytes.Should().BeGreaterThan(0, "file size should be tracked");

        // Second call should hit cache
        var cachedResult = await _service.GetProjectAsync(projectName);
        cachedResult.Should().BeSameAs(result, "second call should return cached instance");
    }

    [Fact]
    public async Task GetProjectAsync_WithYamlFrontmatter_ShouldParseMetadata()
    {
        // Arrange
        var projectName = "yaml-test";
        var content = @"---
applyTo: '**/*.cs'
description: 'Test project with YAML'
---
# Test Project
Content here.";
        CreateTestProjectFile(projectName, content);

        // Act
        var result = await _service.GetProjectAsync(projectName);

        // Assert
        result.Should().NotBeNull("project should be loaded");
        result!.ApplyTo.Should().Be("**/*.cs", "applyTo should be parsed from YAML");
        result.Description.Should().Be("Test project with YAML", "description should be parsed from YAML");
    }

    [Fact]
    public async Task GetProjectAsync_WithNonExistentFile_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetProjectAsync("non-existent");

        // Assert
        result.Should().BeNull("non-existent project should return null");
    }

    [Fact]
    public async Task GetProjectAsync_WithNullOrEmptyName_ShouldReturnNull()
    {
        // Act & Assert
        var result1 = await _service.GetProjectAsync(null!);
        result1.Should().BeNull("null project name should return null");

        var result2 = await _service.GetProjectAsync("");
        result2.Should().BeNull("empty project name should return null");

        var result3 = await _service.GetProjectAsync("   ");
        result3.Should().BeNull("whitespace project name should return null");
    }

    [Fact]
    public async Task ListAvailableProjectsAsync_ShouldReturnAllProjectFiles()
    {
        // Arrange
        CreateTestProjectFile("project1", "Content 1");
        CreateTestProjectFile("project2", "Content 2");
        CreateTestProjectFile("project3", "Content 3");

        // Act
        var result = await _service.ListAvailableProjectsAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().HaveCount(3, "should find all 3 project files");
        result.Should().Contain("project1", "should include project1");
        result.Should().Contain("project2", "should include project2");
        result.Should().Contain("project3", "should include project3");
    }

    [Fact]
    public async Task ListAvailableProjectsAsync_WithNonExistentDirectory_ShouldReturnEmpty()
    {
        // Arrange
        _config.PersonaRepoPath = "/non/existent/path";

        // Act
        var result = await _service.ListAvailableProjectsAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().BeEmpty("non-existent directory should return empty list");
    }

    [Fact]
    public async Task ProjectExistsAsync_WithExistingProject_ShouldReturnTrue()
    {
        // Arrange
        CreateTestProjectFile("existing", "Content");

        // Act
        var result = await _service.ProjectExistsAsync("existing");

        // Assert
        result.Should().BeTrue("existing project should return true");
    }

    [Fact]
    public async Task ProjectExistsAsync_WithNonExistentProject_ShouldReturnFalse()
    {
        // Act
        var result = await _service.ProjectExistsAsync("non-existent");

        // Assert
        result.Should().BeFalse("non-existent project should return false");
    }

    [Fact]
    public async Task InvalidateCache_WithSpecificProject_ShouldRemoveFromCache()
    {
        // Arrange
        CreateTestProjectFile("cached", "Content");
        var first = await _service.GetProjectAsync("cached");

        // Act
        _service.InvalidateCache("cached");
        var second = await _service.GetProjectAsync("cached");

        // Assert
        first.Should().NotBeNull("first load should succeed");
        second.Should().NotBeNull("second load should succeed");
        second.Should().NotBeSameAs(first, "cache invalidation should force reload");
    }

    [Fact]
    public async Task InvalidateCache_WithNull_ShouldClearEntireCache()
    {
        // Arrange
        CreateTestProjectFile("project1", "Content 1");
        CreateTestProjectFile("project2", "Content 2");
        await _service.GetProjectAsync("project1");
        await _service.GetProjectAsync("project2");

        // Act
        _service.InvalidateCache(null);
        var reloaded1 = await _service.GetProjectAsync("project1");
        var reloaded2 = await _service.GetProjectAsync("project2");

        // Assert
        reloaded1.Should().NotBeNull("project1 should be reloaded");
        reloaded2.Should().NotBeNull("project2 should be reloaded");
    }

    [Fact]
    public async Task RefreshCacheAsync_ShouldPreloadAllProjects()
    {
        // Arrange
        CreateTestProjectFile("project1", "Content 1");
        CreateTestProjectFile("project2", "Content 2");

        // Act
        await _service.RefreshCacheAsync();

        // Assert - both should be cached, so second calls should be instant
        var result1 = await _service.GetProjectAsync("project1");
        var result2 = await _service.GetProjectAsync("project2");

        result1.Should().NotBeNull("project1 should be cached");
        result2.Should().NotBeNull("project2 should be cached");
    }

    [Fact]
    public async Task GetCurrentProjectAsync_WithNoCurrentConfigured_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetCurrentProjectAsync();

        // Assert
        result.Should().BeNull("no current project configured should return null");
    }

    [Fact]
    public async Task GetCurrentProjectAsync_WithCurrentConfigured_ShouldReturnProject()
    {
        // Arrange
        CreateTestProjectFile("current", "Current project content");
        _config.CurrentProject = "current";

        // Act
        var result = await _service.GetCurrentProjectAsync();

        // Assert
        result.Should().NotBeNull("current project should be returned");
        result!.Name.Should().Be("current", "should return configured current project");
    }

    [Fact]
    public async Task SetCurrentProjectAsync_WithExistingProject_ShouldUpdateConfig()
    {
        // Arrange
        CreateTestProjectFile("new-current", "Content");

        // Act
        await _service.SetCurrentProjectAsync("new-current");

        // Assert
        _config.CurrentProject.Should().Be("new-current", "current project should be updated");
    }

    [Fact]
    public async Task SetCurrentProjectAsync_WithNonExistentProject_ShouldThrow()
    {
        // Act & Assert
        var exception = await Record.ExceptionAsync(() => 
            _service.SetCurrentProjectAsync("non-existent"));

        exception.Should().NotBeNull("should throw exception");
        exception.Should().BeOfType<FileNotFoundException>("should throw FileNotFoundException");
    }

    [Fact]
    public async Task SetCurrentProjectAsync_WithNullOrEmpty_ShouldThrow()
    {
        // Act & Assert
        var exception1 = await Record.ExceptionAsync(() => 
            _service.SetCurrentProjectAsync(null!));
        exception1.Should().BeOfType<ArgumentException>("null should throw ArgumentException");

        var exception2 = await Record.ExceptionAsync(() => 
            _service.SetCurrentProjectAsync(""));
        exception2.Should().BeOfType<ArgumentException>("empty should throw ArgumentException");
    }

    [Fact]
    public async Task CacheExpiration_AfterTtl_ShouldReloadFromFile()
    {
        // Arrange
        _config.CacheTtlSeconds = 1; // 1 second TTL
        CreateTestProjectFile("expiring", "Original content");
        
        var first = await _service.GetProjectAsync("expiring");
        
        // Wait for cache to expire
        await Task.Delay(1100);
        
        // Update file content
        var filePath = Path.Combine(_testDirectory, "expiring_project.instructions.md");
        await File.WriteAllTextAsync(filePath, "Updated content");

        // Act
        var second = await _service.GetProjectAsync("expiring");

        // Assert
        first.Should().NotBeNull("first load should succeed");
        second.Should().NotBeNull("second load should succeed");
        second!.Content.Should().Be("Updated content", "cache expiration should reload from file");
    }

    [Fact]
    public async Task CreateProjectFromTemplateAsync_ShouldCreateFileInProjectsSubdirectory()
    {
        // Arrange
        var name = "test-project";
        var template = "---\nname: '{{project_name}}'\ndescription: '{{description}}'\n---\n\n# Project: {{project_name}}";
        var replacements = new Dictionary<string, string>
        {
            { "project_name", "My Project" },
            { "description", "Test Description" }
        };

        // Act
        var filePath = await _service.CreateProjectFromTemplateAsync(name, template, replacements);

        // Assert
        File.Exists(filePath).Should().BeTrue("file should be created");
        filePath.Should().EndWith("test-project_project.instructions.md");
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("name: 'My Project'");
        content.Should().Contain("# Project: My Project");
        content.Should().NotContain("{{project_name}}");
        content.Should().NotContain("{{description}}");
    }

    [Fact]
    public async Task CreateProjectFromTemplateAsync_WithExistingFile_ShouldThrowInvalidOperationException()
    {
        // Arrange
        CreateTestProjectFile("existing", "content");
        var template = "test";
        var replacements = new Dictionary<string, string>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.CreateProjectFromTemplateAsync("existing", template, replacements));
        
        exception.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateProjectFromTemplateAsync_WithInvalidName_ShouldThrowArgumentException()
    {
        // Arrange
        var template = "test";
        var replacements = new Dictionary<string, string>();

        // Act & Assert
        var exception1 = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateProjectFromTemplateAsync("invalid name", template, replacements));
        exception1.ParamName.Should().Be("name");

        var exception2 = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateProjectFromTemplateAsync("invalid$name", template, replacements));
        exception2.ParamName.Should().Be("name");

        var exception3 = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateProjectFromTemplateAsync("", template, replacements));
        exception3.ParamName.Should().Be("name");
    }

    [Fact]
    public async Task CreateProjectFromTemplateAsync_ShouldInvalidateCache()
    {
        // Arrange
        var name = "cache-test";
        var template = "---\nname: 'test'\ndescription: 'Test'\n---\n\nContent";
        var replacements = new Dictionary<string, string>();

        // Act
        var filePath = await _service.CreateProjectFromTemplateAsync(name, template, replacements);
        
        // Load into cache
        var project1 = await _service.GetProjectAsync(name);
        
        // Modify file directly
        await File.WriteAllTextAsync(filePath, "---\nname: 'test'\ndescription: 'Test'\n---\n\nModified");
        
        // Invalidate and reload
        _service.InvalidateCache(name);
        var project2 = await _service.GetProjectAsync(name);

        // Assert
        project1.Should().NotBeNull();
        project2.Should().NotBeNull();
        project2!.Content.Should().Contain("Modified");
    }

    [Fact]
    public async Task CreateProjectFromTemplateAsync_ShouldReplaceMultipleTokens()
    {
        // Arrange
        var name = "multi-token";
        var template = "{{token1}} and {{token2}} and {{token1}} again";
        var replacements = new Dictionary<string, string>
        {
            { "token1", "VALUE1" },
            { "token2", "VALUE2" }
        };

        // Act
        var filePath = await _service.CreateProjectFromTemplateAsync(name, template, replacements);

        // Assert
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Be("VALUE1 and VALUE2 and VALUE1 again");
    }

    [Fact]
    public async Task ValidateProjectAsync_WithValidFile_ShouldReturnValid()
    {
        // Arrange
        var validContent = @"---
description: Test Project
applyTo: '**'
---

# Overview
This is a test project

# Goals
- Complete implementation
- Pass all tests

# Scope
- Backend only
- Web services

# Requirements
- .NET 8 SDK
- Windows/Mac/Linux

# Constraints
- 3 month timeline
- $50k budget";

        var testFile = Path.Combine(_testDirectory, "test_valid.md");
        await File.WriteAllTextAsync(testFile, validContent);

        // Act
        var result = await _service.ValidateProjectAsync(testFile);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateProjectAsync_WithMissingFile_ShouldReturnError()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.md");

        // Act
        var result = await _service.ValidateProjectAsync(nonExistentFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCount.Should().Be(1);
        result.Issues.First().Severity.Should().Be(ValidationSeverity.Error);
        result.Issues.First().Section.Should().Be("File");
    }

    [Fact]
    public async Task ValidateProjectAsync_WithEmptyFile_ShouldReturnError()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "empty.md");
        await File.WriteAllTextAsync(testFile, string.Empty);

        // Act
        var result = await _service.ValidateProjectAsync(testFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCount.Should().Be(1);
    }

    [Fact]
    public async Task ValidateProjectAsync_WithMissingFrontmatter_ShouldReturnWarning()
    {
        // Arrange
        var content = "# Overview\nTest content";
        var testFile = Path.Combine(_testDirectory, "no_frontmatter.md");
        await File.WriteAllTextAsync(testFile, content);

        // Act
        var result = await _service.ValidateProjectAsync(testFile);

        // Assert
        result.IsValid.Should().BeTrue();
        result.WarningCount.Should().BeGreaterThan(0);
        result.Issues.Any(i => i.Severity == ValidationSeverity.Warning && i.Section == "Metadata").Should().BeTrue();
    }

    [Fact]
    public async Task ValidateProjectAsync_WithMissingSections_ShouldReturnWarnings()
    {
        // Arrange
        var content = @"---
description: Incomplete
---

# Overview
Only has overview";

        var testFile = Path.Combine(_testDirectory, "incomplete.md");
        await File.WriteAllTextAsync(testFile, content);

        // Act
        var result = await _service.ValidateProjectAsync(testFile);

        // Assert
        result.IsValid.Should().BeTrue();
        result.WarningCount.Should().BeGreaterThan(0);
        result.Issues.Should().Contain(i => i.Section == "Goals");
        result.Issues.Should().Contain(i => i.Section == "Scope");
    }

    [Fact]
    public async Task ValidateProjectAsync_WithShortContent_ShouldReturnWarning()
    {
        // Arrange
        var content = "---\n---\nShort";
        var testFile = Path.Combine(_testDirectory, "short.md");
        await File.WriteAllTextAsync(testFile, content);

        // Act
        var result = await _service.ValidateProjectAsync(testFile);

        // Assert
        result.IsValid.Should().BeTrue();
        result.WarningCount.Should().BeGreaterThan(0);
        result.Issues.Should().Contain(i => i.Section == "Content");
    }

    [Fact]
    public async Task ValidateProjectAsync_WithLowHeaderCount_ShouldReturnInfo()
    {
        // Arrange
        var content = @"---
description: Test
---

Just plain text without headers";

        var testFile = Path.Combine(_testDirectory, "no_headers.md");
        await File.WriteAllTextAsync(testFile, content);

        // Act
        var result = await _service.ValidateProjectAsync(testFile);

        // Assert
        result.IsValid.Should().BeTrue();
        result.InfoCount.Should().BeGreaterThan(0);
        result.Issues.Should().Contain(i => i.Severity == ValidationSeverity.Info && i.Section == "Format");
    }

    private void CreateTestProjectFile(string projectName, string content)
    {
        var fileName = $"{projectName}_project.instructions.md";
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content);
    }
}
