// ValidationPromptsTests.cs - Tests for ValidationPrompts
// Tests for MCP prompt methods with mocked dependencies

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using AwesomeAssertions;
using PersonaMcpServer.Services;
using PersonaMcpServer.Server;

namespace PersonaMcpServer.Tests.Server;

/// <summary>
/// Tests for ValidationPrompts
/// </summary>
public class ValidationPromptsTests
{
    private readonly IPromptService _mockPromptService;
    private readonly ValidationPrompts _prompts;

    public ValidationPromptsTests()
    {
        _mockPromptService = Substitute.For<IPromptService>();
        _prompts = new ValidationPrompts(_mockPromptService);
    }

    [Fact]
    public async Task GetPersonaValidationPromptAsync_WithoutContent_ShouldReturnBasePrompt()
    {
        // Arrange
        const string basePrompt = "# Persona Validation Prompt\n\nValidate the persona file for required sections.";
        _mockPromptService.GetPersonaValidationPromptAsync(Arg.Any<CancellationToken>())
            .Returns(basePrompt);

        // Act
        var result = await _prompts.GetPersonaValidationPromptAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Be(basePrompt, "should return base prompt when no content provided");
        await _mockPromptService.Received(1).GetPersonaValidationPromptAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPersonaValidationPromptAsync_WithContent_ShouldInsertContentIntoPrompt()
    {
        // Arrange
        const string basePrompt = "# Persona Validation Prompt\n\n<!-- INSERT_PERSONA_CONTENT -->\n\nPlease validate the above persona.";
        const string personaContent = "# Metadata\n\nYour Name: John";
        _mockPromptService.GetPersonaValidationPromptAsync(Arg.Any<CancellationToken>())
            .Returns(basePrompt);

        // Act
        var result = await _prompts.GetPersonaValidationPromptAsync(personaContent);

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("```markdown", "should wrap content in markdown code block");
        result.Should().Contain(personaContent, "should include persona content");
        result.Should().NotContain("<!-- INSERT_PERSONA_CONTENT -->", "should replace placeholder");
        await _mockPromptService.Received(1).GetPersonaValidationPromptAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPersonaValidationPromptAsync_WithEmptyContent_ShouldReturnBasePrompt()
    {
        // Arrange
        const string basePrompt = "# Persona Validation Prompt\n\nValidate the persona file.";
        _mockPromptService.GetPersonaValidationPromptAsync(Arg.Any<CancellationToken>())
            .Returns(basePrompt);

        // Act
        var result = await _prompts.GetPersonaValidationPromptAsync(string.Empty);

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Be(basePrompt, "should return base prompt when empty content provided");
    }

    [Fact]
    public async Task GetPersonaValidationPromptAsync_WithServiceError_ShouldReturnErrorJson()
    {
        // Arrange
        _mockPromptService.GetPersonaValidationPromptAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new FileNotFoundException("Prompt not found")));

        // Act
        var result = await _prompts.GetPersonaValidationPromptAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("Failed to retrieve persona validation prompt", "should include error message");
    }

    [Fact]
    public async Task GetProjectValidationPromptAsync_WithoutContent_ShouldReturnBasePrompt()
    {
        // Arrange
        const string basePrompt = "# Project Validation Prompt\n\nValidate the project file for required sections.";
        _mockPromptService.GetProjectValidationPromptAsync(Arg.Any<CancellationToken>())
            .Returns(basePrompt);

        // Act
        var result = await _prompts.GetProjectValidationPromptAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Be(basePrompt, "should return base prompt when no content provided");
        await _mockPromptService.Received(1).GetProjectValidationPromptAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProjectValidationPromptAsync_WithContent_ShouldInsertContentIntoPrompt()
    {
        // Arrange
        const string basePrompt = "# Project Validation Prompt\n\n<!-- INSERT_PROJECT_CONTENT -->\n\nPlease validate the above project.";
        const string projectContent = "# Overview\n\nProject Name: MyProject";
        _mockPromptService.GetProjectValidationPromptAsync(Arg.Any<CancellationToken>())
            .Returns(basePrompt);

        // Act
        var result = await _prompts.GetProjectValidationPromptAsync(projectContent);

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("```markdown", "should wrap content in markdown code block");
        result.Should().Contain(projectContent, "should include project content");
        result.Should().NotContain("<!-- INSERT_PROJECT_CONTENT -->", "should replace placeholder");
        await _mockPromptService.Received(1).GetProjectValidationPromptAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProjectValidationPromptAsync_WithEmptyContent_ShouldReturnBasePrompt()
    {
        // Arrange
        const string basePrompt = "# Project Validation Prompt\n\nValidate the project file.";
        _mockPromptService.GetProjectValidationPromptAsync(Arg.Any<CancellationToken>())
            .Returns(basePrompt);

        // Act
        var result = await _prompts.GetProjectValidationPromptAsync(string.Empty);

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Be(basePrompt, "should return base prompt when empty content provided");
    }

    [Fact]
    public async Task GetProjectValidationPromptAsync_WithServiceError_ShouldReturnErrorJson()
    {
        // Arrange
        _mockPromptService.GetProjectValidationPromptAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new FileNotFoundException("Prompt not found")));

        // Act
        var result = await _prompts.GetProjectValidationPromptAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("\"error\"", "should contain error field");
        result.Should().Contain("Failed to retrieve project validation prompt", "should include error message");
    }
}
