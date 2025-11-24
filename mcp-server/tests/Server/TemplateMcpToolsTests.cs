// TemplateMcpToolsTests.cs - Tests for TemplateMcpTools
// Tests for MCP tool methods with mocked dependencies

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using AwesomeAssertions;
using PersonaMcpServer.Services;
using PersonaMcpServer.Server;

namespace PersonaMcpServer.Tests.Server;

/// <summary>
/// Tests for TemplateMcpTools
/// </summary>
public class TemplateMcpToolsTests
{
    private readonly ITemplateService _mockService;
    private readonly TemplateMcpTools _tools;

    public TemplateMcpToolsTests()
    {
        _mockService = Substitute.For<ITemplateService>();
        _tools = new TemplateMcpTools(_mockService);
    }

    [Fact]
    public async Task ListTemplatesAsync_ShouldReturnJsonArray()
    {
        // Arrange
        var expected = new List<string> { "persona_template.instructions", "project_template.instructions" };
        _mockService.ListAvailableTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _tools.ListTemplatesAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        var templates = JsonSerializer.Deserialize<List<string>>(result);
        templates.Should().NotBeNull("deserialized result should not be null");
        templates.Should().BeEquivalentTo(expected, "should return all template names");
        await _mockService.Received(1).ListAvailableTemplatesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTemplatesAsync_WithServiceError_ShouldReturnJsonError()
    {
        // Arrange
        _mockService.ListAvailableTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<string>>(new IOException("Test error")));

        // Act
        var result = await _tools.ListTemplatesAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("error", "should contain error field");
        result.Should().Contain("Test error", "should contain error message");
    }

    [Fact]
    public async Task GetPersonaTemplateAsync_ShouldReturnContent()
    {
        // Arrange
        var expectedContent = "---\napplyTo: '**'\ndescription: 'Persona template'\n---\n\n# Template content";
        _mockService.GetPersonaTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(expectedContent);

        // Act
        var result = await _tools.GetPersonaTemplateAsync();

        // Assert
        result.Should().Be(expectedContent, "should return template content");
        await _mockService.Received(1).GetPersonaTemplateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPersonaTemplateAsync_WithServiceError_ShouldReturnJsonError()
    {
        // Arrange
        _mockService.GetPersonaTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new FileNotFoundException("Template not found")));

        // Act
        var result = await _tools.GetPersonaTemplateAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("error", "should contain error field");
        result.Should().Contain("Template not found", "should contain error message");
    }

    [Fact]
    public async Task GetProjectTemplateAsync_ShouldReturnContent()
    {
        // Arrange
        var expectedContent = "---\nname: 'project'\ndescription: 'Project template'\n---\n\n# Template content";
        _mockService.GetProjectTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(expectedContent);

        // Act
        var result = await _tools.GetProjectTemplateAsync();

        // Assert
        result.Should().Be(expectedContent, "should return template content");
        await _mockService.Received(1).GetProjectTemplateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProjectTemplateAsync_WithServiceError_ShouldReturnJsonError()
    {
        // Arrange
        _mockService.GetProjectTemplateAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new FileNotFoundException("Template not found")));

        // Act
        var result = await _tools.GetProjectTemplateAsync();

        // Assert
        result.Should().NotBeNull("result should not be null");
        result.Should().Contain("error", "should contain error field");
        result.Should().Contain("Template not found", "should contain error message");
    }
}
