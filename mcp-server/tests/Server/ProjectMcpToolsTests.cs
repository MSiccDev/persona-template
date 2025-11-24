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
    private readonly ITemplateService _mockTemplateService;
    private readonly ProjectMcpTools _tools;

    public ProjectMcpToolsTests()
    {
        _mockService = Substitute.For<IProjectInstructionService>();
        _mockTemplateService = Substitute.For<ITemplateService>();
        _tools = new ProjectMcpTools(_mockService, _mockTemplateService);
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

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldCreateProject()
    {
        // Arrange
        var template = "---\napplyTo: '**/*.cs'\n---\n# Project Instructions – <Project Name>\n";
        var filePath = "/path/to/test-project_project.instructions.md";
        
        _mockTemplateService.GetProjectTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(template);
        _mockService.CreateProjectFromTemplateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<System.Collections.Generic.Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(filePath);

        // Act
        var result = await _tools.CreateFromTemplateAsync("test-project", "My Project", "A test project", "Development", "C#", "ASP.NET Core");

        // Assert
        result.Should().Contain("success", "should indicate success");
        result.Should().Contain("true", "should indicate success status");
        result.Should().Contain("test-project", "should include project name");
        result.Should().Contain(filePath, "should include file path");
        await _mockTemplateService.Received(1).GetProjectTemplateAsync(Arg.Any<CancellationToken>());
        await _mockService.Received(1).CreateProjectFromTemplateAsync(
            "test-project",
            template,
            Arg.Is<System.Collections.Generic.Dictionary<string, string>>(d => 
                d["<Project Name>"] == "My Project" && 
                d["<short description>"] == "A test project" &&
                d["<planning / prototype / active development / maintenance / MVP / release>"] == "Development"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateFromTemplateAsync_WithExistingFile_ShouldReturnJsonError()
    {
        // Arrange
        var template = "template content";
        _mockTemplateService.GetProjectTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(template);
        _mockService.CreateProjectFromTemplateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<System.Collections.Generic.Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("File already exists")));

        // Act
        var result = await _tools.CreateFromTemplateAsync("existing-project", "Project", "Desc", "Dev", "C#", "ASP.NET");

        // Assert
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("File already exists", "should include error message");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_WithInvalidName_ShouldReturnJsonError()
    {
        // Arrange
        var template = "template content";
        _mockTemplateService.GetProjectTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(template);
        _mockService.CreateProjectFromTemplateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<System.Collections.Generic.Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new ArgumentException("Invalid name format")));

        // Act
        var result = await _tools.CreateFromTemplateAsync("invalid name", "Project", "Desc", "Dev", "C#", "ASP.NET");

        // Assert
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("Invalid name format", "should include error message");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_WithTemplateError_ShouldReturnJsonError()
    {
        // Arrange
        _mockTemplateService.GetProjectTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new FileNotFoundException("Template not found")));

        // Act
        var result = await _tools.CreateFromTemplateAsync("test-project", "Project", "Desc", "Dev", "C#", "ASP.NET");

        // Assert
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("Failed to create project", "should include error prefix");
    }

    [Fact]
    public async Task ValidateProjectAsync_WithValidFile_ShouldReturnValidJson()
    {
        // Arrange
        var validResult = new ValidationResult();
        var testPath = "/test/project.md";
        _mockService.ValidateProjectAsync(testPath, Arg.Any<CancellationToken>())
            .Returns(validResult);

        // Act
        var result = await _tools.ValidateProjectAsync(testPath);

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("\"isValid\": true", "should indicate file is valid");
        result.Should().Contain("\"issues\": []", "should have empty issues array");
        await _mockService.Received(1).ValidateProjectAsync(testPath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateProjectAsync_WithValidationIssues_ShouldReturnIssuesInJson()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Overview", "Missing section");
        result.AddWarning("Content", "Content too short");
        var testPath = "/test/project.md";
        _mockService.ValidateProjectAsync(testPath, Arg.Any<CancellationToken>())
            .Returns(result);

        // Act
        var jsonResult = await _tools.ValidateProjectAsync(testPath);

        // Assert
        jsonResult.Should().NotBeNull("result should not be null");
        jsonResult.Should().Contain("\"isValid\": false", "should indicate file is invalid");
        jsonResult.Should().Contain("\"errorCount\": 1", "should include error count");
        jsonResult.Should().Contain("\"warningCount\": 1", "should include warning count");
        jsonResult.Should().Contain("\"severity\": \"Error\"", "should include error severity");
        jsonResult.Should().Contain("\"severity\": \"Warning\"", "should include warning severity");
    }

    [Fact]
    public async Task ValidateProjectAsync_WithFileNotFound_ShouldReturnErrorJson()
    {
        // Arrange
        var testPath = "/nonexistent/project.md";
        _mockService.ValidateProjectAsync(testPath, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ValidationResult>(new FileNotFoundException("File not found")));

        // Act
        var result = await _tools.ValidateProjectAsync(testPath);

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("File not found", "should include file not found message");
    }

    [Fact]
    public async Task ValidateProjectAsync_WithException_ShouldReturnErrorJson()
    {
        // Arrange
        var testPath = "/test/project.md";
        _mockService.ValidateProjectAsync(testPath, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ValidationResult>(new Exception("Unexpected error")));

        // Act
        var result = await _tools.ValidateProjectAsync(testPath);

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("Validation failed", "should include validation failed message");
    }
}
