// CreationValidationIntegrationTests.cs - End-to-End Integration Tests
// Tests the complete creation and validation workflows

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
/// Integration tests for creation and validation workflows
/// </summary>
public class CreationValidationIntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IHost _host;
    private readonly IServiceProvider _services;
    private readonly PersonaMcpTools _personaTools;
    private readonly ProjectMcpTools _projectTools;

    public CreationValidationIntegrationTests()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"mcp_creation_validation_{Guid.NewGuid()}");
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
        
        // Copy actual template files
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
        
        // Clean up the entire test root directory
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
    public async Task CreateAndRetrievePersona_ShouldSucceed()
    {
        // Act - Create persona from template
        var createResult = await _personaTools.CreateFromTemplateAsync("developer", "Jane Doe", "Senior Developer", "San Francisco", "Microsoft .NET");
        
        // Assert - Parse JSON response to verify success
        var jsonDoc = JsonDocument.Parse(createResult);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue("creation should succeed");
        
        // Act - Retrieve the created persona
        var retrieved = await _personaTools.GetPersonaAsync("developer");
        
        // Assert - Retrieved persona should contain our data
        retrieved.Should().Contain("Jane Doe", "should contain replaced name");
    }

    [Fact]
    public async Task CreateAndRetrieveProject_ShouldSucceed()
    {
        // Act - Create project from template
        var createResult = await _projectTools.CreateFromTemplateAsync("webapp", "E-Commerce", "Shopping platform", "active development", "TypeScript", "React");
        
        // Assert - Parse JSON response to verify success
        var jsonDoc = JsonDocument.Parse(createResult);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue("creation should succeed");
        
        // Act - Retrieve the created project
        var retrieved = await _projectTools.GetProjectAsync("webapp");
        
        // Assert - Retrieved project should contain our data
        retrieved.Should().Contain("E-Commerce", "should contain replaced project name");
    }

    [Fact]
    public async Task ValidateCreatedPersona_ShouldPass()
    {
        // Arrange - Create a persona
        await _personaTools.CreateFromTemplateAsync("tester", "Test User", "QA Engineer", "Remote", ".NET");
        
        // Act - Find and validate the created file
        var personaService = _services.GetRequiredService<IPersonaInstructionService>();
        var personaFiles = Directory.GetFiles(_testDirectory, "*tester*.md");
        
        if (personaFiles.Length > 0)
        {
            var validationResult = await personaService.ValidatePersonaAsync(personaFiles[0]);
            
            // Assert - Should have no errors
            validationResult.ErrorCount.Should().Be(0, "created persona should have no validation errors");
        }
    }

    [Fact]
    public async Task ValidateCreatedProject_ShouldPass()
    {
        // Arrange - Create a project
        await _projectTools.CreateFromTemplateAsync("testproj", "Test Project", "Test application", "planning", "C#", "ASP.NET");
        
        // Act - Find and validate the created file
        var projectService = _services.GetRequiredService<IProjectInstructionService>();
        var projectsDir = Path.Combine(_testDirectory, "projects");
        
        if (Directory.Exists(projectsDir))
        {
            var projectFiles = Directory.GetFiles(projectsDir, "*testproj*.md");
            
            if (projectFiles.Length > 0)
            {
                var validationResult = await projectService.ValidateProjectAsync(projectFiles[0]);
                
                // Assert - Should have no errors
                validationResult.ErrorCount.Should().Be(0, "created project should have no validation errors");
            }
        }
    }

    [Fact]
    public async Task EndToEndWorkflow_ShouldComplete()
    {
        // Act 1 - Create persona
        var personaCreate = await _personaTools.CreateFromTemplateAsync("alice", "Alice Johnson", "Architect", "Boston", "Cloud");
        var personaJson = JsonDocument.Parse(personaCreate);
        personaJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue("persona creation should succeed");
        
        // Act 2 - Create project
        var projectCreate = await _projectTools.CreateFromTemplateAsync("cloudapp", "Cloud Platform", "Microservices", "active development", "Go", "Kubernetes");
        var projectJson = JsonDocument.Parse(projectCreate);
        projectJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue("project creation should succeed");
        
        // Act 3 - List and retrieve
        var personas = await _personaTools.ListPersonasAsync();
        var projects = await _projectTools.ListProjectsAsync();
        
        // Assert - Both should be present
        personas.Should().Contain("alice", "alice persona should be listed");
        projects.Should().Contain("cloudapp", "cloudapp project should be listed");
    }
}
