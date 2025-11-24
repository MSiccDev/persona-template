using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using AwesomeAssertions;
using PersonaMcpServer.Services;

namespace PersonaMcpServer.Tests.Services;

public class PromptServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _promptsDirectory;
    private readonly IOptions<PersonaServerConfig> _mockConfig;
    private readonly ILogger<PromptService> _mockLogger;
    private readonly PromptService _service;

    public PromptServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"prompt_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        // Create persona repo directory that will be used by the service
        var personaRepoPath = Path.Combine(_testDirectory, "personas");
        Directory.CreateDirectory(personaRepoPath);
        
        // Create prompts directory as sibling to persona repo
        _promptsDirectory = Path.Combine(_testDirectory, "prompts");
        Directory.CreateDirectory(_promptsDirectory);

        _mockConfig = Options.Create(new PersonaServerConfig
        {
            PersonaRepoPath = personaRepoPath,
            Host = "localhost",
            Port = 3000,
            Transport = "STDIO"
        });

        _mockLogger = Substitute.For<ILogger<PromptService>>();
        _service = new PromptService(_mockConfig, _mockLogger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task GetPersonaValidationPromptAsync_ShouldReturnFullPrompt()
    {
        // Arrange
        var promptContent = "---\ndescription: 'Test prompt'\nagent: 'agent'\n---\n\n# Validation Prompt\n\nThis is a test.";
        var promptPath = Path.Combine(_promptsDirectory, "validate-persona-instructions.prompt.md");
        await File.WriteAllTextAsync(promptPath, promptContent);

        // Act
        var result = await _service.GetPersonaValidationPromptAsync();

        // Assert
        result.Should().Be(promptContent);
        result.Should().Contain("---"); // YAML frontmatter
        result.Should().Contain("description:");
        result.Should().Contain("# Validation Prompt");
    }

    [Fact]
    public async Task GetProjectValidationPromptAsync_ShouldReturnFullPrompt()
    {
        // Arrange
        var promptContent = "---\ndescription: 'Test project prompt'\nagent: 'agent'\n---\n\n# Project Validation\n\nTest content.";
        var promptPath = Path.Combine(_promptsDirectory, "validate-project-instructions.prompt.md");
        await File.WriteAllTextAsync(promptPath, promptContent);

        // Act
        var result = await _service.GetProjectValidationPromptAsync();

        // Assert
        result.Should().Be(promptContent);
        result.Should().Contain("---");
        result.Should().Contain("description:");
        result.Should().Contain("# Project Validation");
    }

    [Fact]
    public async Task GetPersonaValidationPromptAsync_UsesCaching()
    {
        // Arrange
        var promptContent = "---\ndescription: 'Cached prompt'\n---\n\n# Test";
        var promptPath = Path.Combine(_promptsDirectory, "validate-persona-instructions.prompt.md");
        await File.WriteAllTextAsync(promptPath, promptContent);

        // Act
        var result1 = await _service.GetPersonaValidationPromptAsync();
        
        // Delete the file to verify cache is used
        File.Delete(promptPath);
        
        var result2 = await _service.GetPersonaValidationPromptAsync();

        // Assert
        result1.Should().Be(promptContent);
        result2.Should().Be(promptContent);
        result1.Should().Be(result2);
    }

    [Fact]
    public async Task GetProjectValidationPromptAsync_UsesCaching()
    {
        // Arrange
        var promptContent = "---\ndescription: 'Cached project prompt'\n---\n\n# Test";
        var promptPath = Path.Combine(_promptsDirectory, "validate-project-instructions.prompt.md");
        await File.WriteAllTextAsync(promptPath, promptContent);

        // Act
        var result1 = await _service.GetProjectValidationPromptAsync();
        
        // Delete the file to verify cache is used
        File.Delete(promptPath);
        
        var result2 = await _service.GetProjectValidationPromptAsync();

        // Assert
        result1.Should().Be(promptContent);
        result2.Should().Be(promptContent);
        result1.Should().Be(result2);
    }

    [Fact]
    public async Task GetPersonaValidationPromptAsync_WithMissingFile_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _service.GetPersonaValidationPromptAsync());
    }

    [Fact]
    public async Task GetProjectValidationPromptAsync_WithMissingFile_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _service.GetProjectValidationPromptAsync());
    }

    [Fact]
    public async Task GetPersonaValidationPromptAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var promptPath = Path.Combine(_promptsDirectory, "validate-persona-instructions.prompt.md");
        await File.WriteAllTextAsync(promptPath, "content");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _service.GetPersonaValidationPromptAsync(cts.Token));
    }
}
