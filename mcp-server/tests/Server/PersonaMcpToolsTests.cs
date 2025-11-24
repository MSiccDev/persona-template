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
    private readonly ITemplateService _mockTemplateService;
    private readonly PersonaMcpTools _tools;

    public PersonaMcpToolsTests()
    {
        _mockService = Substitute.For<IPersonaInstructionService>();
        _mockTemplateService = Substitute.For<ITemplateService>();
        _tools = new PersonaMcpTools(_mockService, _mockTemplateService);
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

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldCreatePersona()
    {
        // Arrange
        var template = "---\napplyTo: '**'\n---\n# Personal Persona Instructions – [Your Name] ([Your Role/Title])\n";
        var filePath = "/path/to/test-persona_persona.instructions.md";
        
        _mockTemplateService.GetPersonaTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(template);
        _mockService.CreatePersonaFromTemplateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<System.Collections.Generic.Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(filePath);

        // Act
        var result = await _tools.CreateFromTemplateAsync("test-persona", "John Doe", "Developer", "USA", "Apple");

        // Assert
        result.Should().Contain("success", "should indicate success");
        result.Should().Contain("true", "should indicate success status");
        result.Should().Contain("test-persona", "should include persona name");
        result.Should().Contain(filePath, "should include file path");
        await _mockTemplateService.Received(1).GetPersonaTemplateAsync(Arg.Any<CancellationToken>());
        await _mockService.Received(1).CreatePersonaFromTemplateAsync(
            "test-persona",
            template,
            Arg.Is<System.Collections.Generic.Dictionary<string, string>>(d => 
                d["[Your Name]"] == "John Doe" && 
                d["[Your Role/Title]"] == "Developer" &&
                d["[Your Location]"] == "USA"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateFromTemplateAsync_WithExistingFile_ShouldReturnJsonError()
    {
        // Arrange
        var template = "template content";
        _mockTemplateService.GetPersonaTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(template);
        _mockService.CreatePersonaFromTemplateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<System.Collections.Generic.Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("File already exists")));

        // Act
        var result = await _tools.CreateFromTemplateAsync("existing-persona", "John", "Dev", "USA", ".NET");

        // Assert
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("File already exists", "should include error message");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_WithInvalidName_ShouldReturnJsonError()
    {
        // Arrange
        var template = "template content";
        _mockTemplateService.GetPersonaTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(template);
        _mockService.CreatePersonaFromTemplateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<System.Collections.Generic.Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new ArgumentException("Invalid name format")));

        // Act
        var result = await _tools.CreateFromTemplateAsync("invalid name", "John", "Dev", "USA", ".NET");

        // Assert
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("Invalid name format", "should include error message");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_WithTemplateError_ShouldReturnJsonError()
    {
        // Arrange
        _mockTemplateService.GetPersonaTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new FileNotFoundException("Template not found")));

        // Act
        var result = await _tools.CreateFromTemplateAsync("test-persona", "John", "Dev", "USA", ".NET");

        // Assert
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("Failed to create persona", "should include error prefix");
    }

    [Fact]
    public async Task ValidatePersonaAsync_WithValidFile_ShouldReturnValidJson()
    {
        // Arrange
        var validResult = new ValidationResult();
        var testPath = "/test/persona.md";
        _mockService.ValidatePersonaAsync(testPath, Arg.Any<CancellationToken>())
            .Returns(validResult);

        // Act
        var result = await _tools.ValidatePersonaAsync(testPath);

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("\"isValid\": true", "should indicate file is valid");
        result.Should().Contain("\"issues\": []", "should have empty issues array");
        await _mockService.Received(1).ValidatePersonaAsync(testPath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidatePersonaAsync_WithValidationIssues_ShouldReturnIssuesInJson()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Metadata", "Missing section");
        result.AddWarning("Content", "Content too short");
        var testPath = "/test/persona.md";
        _mockService.ValidatePersonaAsync(testPath, Arg.Any<CancellationToken>())
            .Returns(result);

        // Act
        var jsonResult = await _tools.ValidatePersonaAsync(testPath);

        // Assert
        jsonResult.Should().NotBeNull("result should not be null");
        jsonResult.Should().Contain("\"isValid\": false", "should indicate file is invalid");
        jsonResult.Should().Contain("\"errorCount\": 1", "should include error count");
        jsonResult.Should().Contain("\"warningCount\": 1", "should include warning count");
        jsonResult.Should().Contain("\"severity\": \"Error\"", "should include error severity");
        jsonResult.Should().Contain("\"severity\": \"Warning\"", "should include warning severity");
    }

    [Fact]
    public async Task ValidatePersonaAsync_WithFileNotFound_ShouldReturnErrorJson()
    {
        // Arrange
        var testPath = "/nonexistent/persona.md";
        _mockService.ValidatePersonaAsync(testPath, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ValidationResult>(new FileNotFoundException("File not found")));

        // Act
        var result = await _tools.ValidatePersonaAsync(testPath);

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("File not found", "should include file not found message");
    }

    [Fact]
    public async Task ValidatePersonaAsync_WithException_ShouldReturnErrorJson()
    {
        // Arrange
        var testPath = "/test/persona.md";
        _mockService.ValidatePersonaAsync(testPath, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ValidationResult>(new Exception("Unexpected error")));

        // Act
        var result = await _tools.ValidatePersonaAsync(testPath);

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("Validation failed", "should include validation failed message");
    }
}
