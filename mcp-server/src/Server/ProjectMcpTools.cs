// ProjectMcpTools.cs - MCP Tools for Project Management
// Exposes project instruction operations as MCP tools

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using PersonaMcpServer.Services;

namespace PersonaMcpServer.Server;

/// <summary>
/// MCP tools for managing project instruction files
/// </summary>
[McpServerToolType]
public class ProjectMcpTools
{
    private readonly IProjectInstructionService _projectService;
    private readonly ITemplateService _templateService;

    /// <summary>
    /// Initializes a new instance of the ProjectMcpTools class
    /// </summary>
    /// <param name="projectService">Service for project instruction operations</param>
    /// <param name="templateService">Service for template operations</param>
    public ProjectMcpTools(IProjectInstructionService projectService, ITemplateService templateService)
    {
        _projectService = projectService;
        _templateService = templateService;
    }

    /// <summary>
    /// Lists all available project instruction files
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of available project names</returns>
    [McpServerTool(Name = "project_list")]
    [Description("Lists all available project instruction files in the repository")]
    public async Task<string[]> ListProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _projectService.ListAvailableProjectsAsync(cancellationToken);
        return [.. projects];
    }

    /// <summary>
    /// Gets a specific project instruction by name
    /// </summary>
    /// <param name="name">The name of the project to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON representation of the project instruction</returns>
    [McpServerTool(Name = "project_get")]
    [Description("Retrieves a specific project instruction file by name")]
    public async Task<string> GetProjectAsync(
        [Description("The name of the project to retrieve")] string name,
        CancellationToken cancellationToken = default)
    {
        var project = await _projectService.GetProjectAsync(name, cancellationToken);
        
        if (project == null)
        {
            return $"{{\"error\": \"Project '{name}' not found\"}}";
        }

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            name = project.Name,
            content = project.Content,
            applyTo = project.ApplyTo,
            description = project.Description,
            filePath = project.FilePath,
            sizeBytes = project.SizeBytes,
            lastModified = project.LastModified
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Checks if a project exists
    /// </summary>
    /// <param name="name">The name of the project to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if project exists, false otherwise</returns>
    [McpServerTool(Name = "project_exists")]
    [Description("Checks if a project instruction file exists")]
    public async Task<bool> ProjectExistsAsync(
        [Description("The name of the project to check")] string name,
        CancellationToken cancellationToken = default)
    {
        return await _projectService.ProjectExistsAsync(name, cancellationToken);
    }

    /// <summary>
    /// Gets the current active project
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON representation of the current project, or null if none set</returns>
    [McpServerTool(Name = "project_get_current")]
    [Description("Retrieves the currently active project instruction")]
    public async Task<string> GetCurrentProjectAsync(CancellationToken cancellationToken = default)
    {
        var project = await _projectService.GetCurrentProjectAsync(cancellationToken);
        
        if (project == null)
        {
            return "{\"current\": null, \"message\": \"No current project configured\"}";
        }

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            current = project.Name,
            content = project.Content,
            applyTo = project.ApplyTo,
            description = project.Description
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Sets the current active project
    /// </summary>
    /// <param name="name">The name of the project to set as current</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message or error</returns>
    [McpServerTool(Name = "project_set_current")]
    [Description("Sets the currently active project instruction")]
    public async Task<string> SetCurrentProjectAsync(
        [Description("The name of the project to set as current")] string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _projectService.SetCurrentProjectAsync(name, cancellationToken);
            return $"{{\"success\": true, \"message\": \"Current project set to '{name}'\"}}";
        }
        catch (FileNotFoundException)
        {
            return $"{{\"error\": \"Project '{name}' not found\"}}";
        }
        catch (Exception ex)
        {
            return $"{{\"error\": \"{ex.Message}\"}}";
        }
    }

    /// <summary>
    /// Invalidates the project cache
    /// </summary>
    /// <param name="name">Optional: specific project name to invalidate, or null for all</param>
    /// <returns>Success message</returns>
    [McpServerTool(Name = "project_invalidate_cache")]
    [Description("Invalidates the project instruction cache (optionally for a specific project)")]
    public string InvalidateCacheAsync(
        [Description("Optional: specific project name to invalidate (null = all)")] string? name = null)
    {
        _projectService.InvalidateCache(name);
        var target = string.IsNullOrWhiteSpace(name) ? "all projects" : $"project '{name}'";
        return $"{{\"success\": true, \"message\": \"Cache invalidated for {target}\"}}";
    }

    /// <summary>
    /// Refreshes the project cache by preloading all projects
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [McpServerTool(Name = "project_refresh_cache")]
    [Description("Refreshes the project cache by preloading all available projects")]
    public async Task<string> RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        await _projectService.RefreshCacheAsync(cancellationToken);
        return "{\"success\": true, \"message\": \"Project cache refreshed\"}";
    }

    /// <summary>
    /// Creates a new project instruction file from the template
    /// </summary>
    /// <param name="name">The name for the new project file (e.g., 'my-project')</param>
    /// <param name="projectName">Project name to replace <Project Name> placeholder</param>
    /// <param name="description">Project description to replace <short description> placeholder</param>
    /// <param name="phase">Project phase to replace <planning / prototype / ...> placeholder</param>
    /// <param name="language">Primary language to replace <e.g., Swift, C#, TypeScript> placeholder</param>
    /// <param name="framework">Primary framework to replace <e.g., SwiftUI, .NET, React> placeholder</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message with file path or error</returns>
    [McpServerTool(Name = "project_create_from_template")]
    [Description("Creates a new project instruction file from the template with specified values")]
    public async Task<string> CreateFromTemplateAsync(
        [Description("The name for the new project file (e.g., 'my-project')")] string name,
        [Description("Project name to replace <Project Name> placeholder")] string projectName,
        [Description("Project description to replace <short description> placeholder")] string description,
        [Description("Project phase to replace <planning / prototype / ...> placeholder")] string phase,
        [Description("Primary language to replace <e.g., Swift, C#, TypeScript> placeholder")] string language,
        [Description("Primary framework to replace <e.g., SwiftUI, .NET, React> placeholder")] string framework,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Load template
            var template = await _templateService.GetProjectTemplateAsync(cancellationToken);
            
            // Build replacements dictionary
            var replacements = new System.Collections.Generic.Dictionary<string, string>
            {
                { "<Project Name>", projectName },
                { "<short description: what it does, who it's for>", description },
                { "<short description>", description },
                { "<planning / prototype / active development / maintenance / MVP / release>", phase },
                { "<e.g., Swift, C#, TypeScript>", language },
                { "<e.g., SwiftUI, .NET, React>", framework }
            };
            
            // Create file from template
            var filePath = await _projectService.CreateProjectFromTemplateAsync(name, template, replacements, cancellationToken);
            
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Project '{name}' created successfully",
                filePath
            });
        }
        catch (ArgumentException ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { error = $"Failed to create project: {ex.Message}" });
        }
    }
}
