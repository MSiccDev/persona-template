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

public class TemplateServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _templatesDirectory;
    private readonly IOptions<PersonaServerConfig> _mockConfig;
    private readonly ILogger<TemplateService> _mockLogger;
    private readonly TemplateService _service;

    public TemplateServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"template_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        // Create persona repo directory that will be used by the service
        var personaRepoPath = Path.Combine(_testDirectory, "personas");
        Directory.CreateDirectory(personaRepoPath);
        
        // Create templates directory as sibling to persona repo
        _templatesDirectory = Path.Combine(_testDirectory, "templates");
        Directory.CreateDirectory(_templatesDirectory);

        _mockConfig = Options.Create(new PersonaServerConfig
        {
            PersonaRepoPath = personaRepoPath,
            Host = "localhost",
            Port = 3000,
            Transport = "STDIO"
        });

        _mockLogger = Substitute.For<ILogger<TemplateService>>();
        _service = new TemplateService(_mockConfig, _mockLogger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task GetPersonaTemplateAsync_ShouldReturnContent()
    {
        // Arrange
        var templateContent = "# Persona Template\n\nThis is a test template.";
        var templatePath = Path.Combine(_templatesDirectory, "persona_template.instructions.md");
        await File.WriteAllTextAsync(templatePath, templateContent);

        // Act
        var result = await _service.GetPersonaTemplateAsync();

        // Assert
        result.Should().Be(templateContent);
    }

    [Fact]
    public async Task GetProjectTemplateAsync_ShouldReturnContent()
    {
        // Arrange
        var templateContent = "# Project Template\n\nThis is a test project template.";
        var templatePath = Path.Combine(_templatesDirectory, "project_template.instructions.md");
        await File.WriteAllTextAsync(templatePath, templateContent);

        // Act
        var result = await _service.GetProjectTemplateAsync();

        // Assert
        result.Should().Be(templateContent);
    }

    [Fact]
    public async Task ListAvailableTemplatesAsync_ShouldReturnBothTemplates()
    {
        // Arrange
        var personaPath = Path.Combine(_templatesDirectory, "persona_template.instructions.md");
        var projectPath = Path.Combine(_templatesDirectory, "project_template.instructions.md");
        await File.WriteAllTextAsync(personaPath, "persona content");
        await File.WriteAllTextAsync(projectPath, "project content");

        // Act
        var result = await _service.ListAvailableTemplatesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("persona_template.instructions");
        result.Should().Contain("project_template.instructions");
    }

    [Fact]
    public async Task ListAvailableTemplatesAsync_WithNoTemplates_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.ListAvailableTemplatesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAvailableTemplatesAsync_WithMissingDirectory_ShouldReturnEmptyList()
    {
        // Arrange
        Directory.Delete(_templatesDirectory);

        // Act
        var result = await _service.ListAvailableTemplatesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPersonaTemplateAsync_WithMissingFile_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _service.GetPersonaTemplateAsync());
    }

    [Fact]
    public async Task GetProjectTemplateAsync_WithMissingFile_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _service.GetProjectTemplateAsync());
    }

    [Fact]
    public async Task GetPersonaTemplateAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var templatePath = Path.Combine(_templatesDirectory, "persona_template.instructions.md");
        await File.WriteAllTextAsync(templatePath, "content");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _service.GetPersonaTemplateAsync(cts.Token));
    }
}
