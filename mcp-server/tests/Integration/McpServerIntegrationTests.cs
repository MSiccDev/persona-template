// McpServerIntegrationTests.cs - End-to-End Integration Tests
// Tests the complete MCP server with real services and in-memory configuration

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PersonaMcpServer;
using PersonaMcpServer.Server;
using PersonaMcpServer.Services;

namespace PersonaMcpServer.Tests.Integration;

/// <summary>
/// Integration tests for the complete MCP server stack
/// </summary>
public class McpServerIntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IHost _host;
    private readonly IServiceProvider _services;
    private readonly PersonaMcpTools _personaTools;
    private readonly ProjectMcpTools _projectTools;

    public McpServerIntegrationTests()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"mcp_integration_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        // Create templates directory within test directory for isolation
        var sourceTemplatesDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "templates"));
        var testTempRoot = Path.Combine(Path.GetTempPath(), $"mcp_test_root_{Guid.NewGuid()}");
        var targetTemplatesDirectory = Path.Combine(testTempRoot, "templates");
        Directory.CreateDirectory(targetTemplatesDirectory);
        
        // Update test directory to be inside the test root so templates path works correctly
        var oldTestDirectory = _testDirectory;
        _testDirectory = Path.Combine(testTempRoot, Path.GetFileName(_testDirectory));
        Directory.Move(oldTestDirectory, _testDirectory);
        
        // Copy actual template files if they exist
        var personaTemplatePath = Path.Combine(sourceTemplatesDirectory, "persona_template.instructions.md");
        var projectTemplatePath = Path.Combine(sourceTemplatesDirectory, "project_template.instructions.md");
        
        if (File.Exists(personaTemplatePath))
        {
            File.Copy(personaTemplatePath, Path.Combine(targetTemplatesDirectory, "persona_template.instructions.md"), overwrite: true);
        }
        
        if (File.Exists(projectTemplatePath))
        {
            File.Copy(projectTemplatePath, Path.Combine(targetTemplatesDirectory, "project_template.instructions.md"), overwrite: true);
        }

        // Create test persona and project files
        CreateTestPersonaFile("developer", "---\napplyTo: '**/*.cs'\ndescription: 'Developer persona'\n---\n# Developer\nTest developer persona");
        CreateTestPersonaFile("architect", "---\napplyTo: '**/*.md'\ndescription: 'Architect persona'\n---\n# Architect\nTest architect persona");
        CreateTestProjectFile("webapp", "---\napplyTo: 'src/**'\ndescription: 'Web application'\n---\n# WebApp\nTest web application");
        CreateTestProjectFile("api", "---\napplyTo: 'api/**'\ndescription: 'REST API'\n---\n# API\nTest REST API");

        // Build host with real configuration
        var builder = Host.CreateApplicationBuilder();
        
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PersonaServer:PersonaRepoPath"] = _testDirectory,
            ["PersonaServer:CacheTtlSeconds"] = "300",
            ["PersonaServer:MaxCacheSizeBytes"] = "1048576",
            ["PersonaServer:CurrentPersona"] = null,
            ["PersonaServer:CurrentProject"] = null
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Warning);

        builder.Services.AddOptions<PersonaServerConfig>()
            .Bind(builder.Configuration.GetSection("PersonaServer"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<IPersonaInstructionService, PersonaInstructionService>();
        builder.Services.AddSingleton<IProjectInstructionService, ProjectInstructionService>();
        builder.Services.AddSingleton<ITemplateService, TemplateService>();
        builder.Services.AddSingleton<IPromptService, PromptService>();
        builder.Services.AddSingleton<PersonaMcpTools>();
        builder.Services.AddSingleton<ProjectMcpTools>();

        _host = builder.Build();
        _services = _host.Services;
        _personaTools = _services.GetRequiredService<PersonaMcpTools>();
        _projectTools = _services.GetRequiredService<ProjectMcpTools>();
    }

    public void Dispose()
    {
        _host?.Dispose();
        
        // Clean up the entire test root directory (includes both test directory and templates)
        var testRootDirectory = Path.GetDirectoryName(_testDirectory);
        if (testRootDirectory != null && Directory.Exists(testRootDirectory) && testRootDirectory.Contains("mcp_test_root_"))
        {
            try
            {
                Directory.Delete(testRootDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task PersonaTools_ListAndGet_ShouldWorkEndToEnd()
    {
        // Act - List personas
        var personas = await _personaTools.ListPersonasAsync();

        // Assert - List
        personas.Should().NotBeNull("list should not be null");
        personas.Should().HaveCount(2, "should list 2 test personas");
        personas.Should().Contain("developer", "should include developer persona");
        personas.Should().Contain("architect", "should include architect persona");

        // Act - Get specific persona
        var developer = await _personaTools.GetPersonaAsync("developer");

        // Assert - Get
        developer.Should().NotBeNull("get should not be null");
        developer.Should().Contain("\"name\": \"developer\"", "should contain persona name");
        developer.Should().Contain("\"applyTo\": \"**/*.cs\"", "should contain applyTo metadata");
        developer.Should().Contain("Developer persona", "should contain description");
    }

    [Fact]
    public async Task ProjectTools_ListAndGet_ShouldWorkEndToEnd()
    {
        // Act - List projects
        var projects = await _projectTools.ListProjectsAsync();

        // Assert - List
        projects.Should().NotBeNull("list should not be null");
        projects.Should().HaveCount(2, "should list 2 test projects");
        projects.Should().Contain("webapp", "should include webapp project");
        projects.Should().Contain("api", "should include api project");

        // Act - Get specific project
        var webapp = await _projectTools.GetProjectAsync("webapp");

        // Assert - Get
        webapp.Should().NotBeNull("get should not be null");
        webapp.Should().Contain("\"name\": \"webapp\"", "should contain project name");
        webapp.Should().Contain("\"applyTo\": \"src/**\"", "should contain applyTo metadata");
        webapp.Should().Contain("Web application", "should contain description");
    }

    [Fact]
    public async Task PersonaTools_SetAndGetCurrent_ShouldWorkEndToEnd()
    {
        // Act - Set current persona
        var setResult = await _personaTools.SetCurrentPersonaAsync("developer");

        // Assert - Set
        setResult.Should().Contain("\"success\": true", "set should succeed");
        setResult.Should().Contain("developer", "should mention persona name");

        // Act - Get current persona
        var current = await _personaTools.GetCurrentPersonaAsync();

        // Assert - Get current
        current.Should().Contain("\"current\": \"developer\"", "should indicate current persona");
        current.Should().Contain("Developer persona", "should contain persona description");
    }

    [Fact]
    public async Task ProjectTools_SetAndGetCurrent_ShouldWorkEndToEnd()
    {
        // Act - Set current project
        var setResult = await _projectTools.SetCurrentProjectAsync("webapp");

        // Assert - Set
        setResult.Should().Contain("\"success\": true", "set should succeed");
        setResult.Should().Contain("webapp", "should mention project name");

        // Act - Get current project
        var current = await _projectTools.GetCurrentProjectAsync();

        // Assert - Get current
        current.Should().Contain("\"current\": \"webapp\"", "should indicate current project");
        current.Should().Contain("Web application", "should contain project description");
    }

    [Fact]
    public async Task PersonaTools_ExistsCheck_ShouldWorkEndToEnd()
    {
        // Act & Assert - Existing persona
        var existsTrue = await _personaTools.PersonaExistsAsync("developer");
        existsTrue.Should().BeTrue("existing persona should return true");

        // Act & Assert - Non-existent persona
        var existsFalse = await _personaTools.PersonaExistsAsync("nonexistent");
        existsFalse.Should().BeFalse("non-existent persona should return false");
    }

    [Fact]
    public async Task ProjectTools_ExistsCheck_ShouldWorkEndToEnd()
    {
        // Act & Assert - Existing project
        var existsTrue = await _projectTools.ProjectExistsAsync("webapp");
        existsTrue.Should().BeTrue("existing project should return true");

        // Act & Assert - Non-existent project
        var existsFalse = await _projectTools.ProjectExistsAsync("nonexistent");
        existsFalse.Should().BeFalse("non-existent project should return false");
    }

    [Fact]
    public async Task PersonaTools_CacheOperations_ShouldWorkEndToEnd()
    {
        // Arrange - Load persona into cache
        await _personaTools.GetPersonaAsync("developer");

        // Act - Invalidate specific persona cache
        var invalidateResult = _personaTools.InvalidateCacheAsync("developer");

        // Assert
        invalidateResult.Should().Contain("\"success\": true", "invalidate should succeed");
        invalidateResult.Should().Contain("developer", "should mention persona name");

        // Act - Refresh cache
        var refreshResult = await _personaTools.RefreshCacheAsync();

        // Assert
        refreshResult.Should().Contain("\"success\": true", "refresh should succeed");
        refreshResult.Should().Contain("refreshed", "should indicate refresh");
    }

    [Fact]
    public async Task ProjectTools_CacheOperations_ShouldWorkEndToEnd()
    {
        // Arrange - Load project into cache
        await _projectTools.GetProjectAsync("webapp");

        // Act - Invalidate specific project cache
        var invalidateResult = _projectTools.InvalidateCacheAsync("webapp");

        // Assert
        invalidateResult.Should().Contain("\"success\": true", "invalidate should succeed");
        invalidateResult.Should().Contain("webapp", "should mention project name");

        // Act - Refresh cache
        var refreshResult = await _projectTools.RefreshCacheAsync();

        // Assert
        refreshResult.Should().Contain("\"success\": true", "refresh should succeed");
        refreshResult.Should().Contain("refreshed", "should indicate refresh");
    }

    [Fact]
    public async Task PersonaTools_GetNonExistent_ShouldReturnError()
    {
        // Act
        var result = await _personaTools.GetPersonaAsync("nonexistent");

        // Assert
        result.Should().Contain("\"error\"", "should return error JSON");
        result.Should().Contain("not found", "should indicate not found");
    }

    [Fact]
    public async Task ProjectTools_GetNonExistent_ShouldReturnError()
    {
        // Act
        var result = await _projectTools.GetProjectAsync("nonexistent");

        // Assert
        result.Should().Contain("\"error\"", "should return error JSON");
        result.Should().Contain("not found", "should indicate not found");
    }

    [Fact]
    public void Configuration_ShouldBeValidatedOnStartup()
    {
        // Arrange - Get configuration
        var config = _services.GetRequiredService<IOptions<PersonaServerConfig>>().Value;

        // Assert - Configuration should be valid
        config.Should().NotBeNull("configuration should be loaded");
        config.PersonaRepoPath.Should().Be(_testDirectory, "should use test directory");
        config.CacheTtlSeconds.Should().Be(300, "should use configured TTL");
        config.MaxCacheSizeBytes.Should().Be(1048576, "should use configured cache size");
    }

    [Fact]
    public void Services_ShouldBeRegisteredAsSingletons()
    {
        // Act - Get services twice
        var personaService1 = _services.GetRequiredService<IPersonaInstructionService>();
        var personaService2 = _services.GetRequiredService<IPersonaInstructionService>();
        var projectService1 = _services.GetRequiredService<IProjectInstructionService>();
        var projectService2 = _services.GetRequiredService<IProjectInstructionService>();

        // Assert - Should be same instances (singletons)
        personaService1.Should().BeSameAs(personaService2, "persona service should be singleton");
        projectService1.Should().BeSameAs(projectService2, "project service should be singleton");
    }

    private void CreateTestPersonaFile(string name, string content)
    {
        var fileName = $"{name}_persona.instructions.md";
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content);
    }

    private void CreateTestProjectFile(string name, string content)
    {
        var fileName = $"{name}_project.instructions.md";
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content);
    }
}
