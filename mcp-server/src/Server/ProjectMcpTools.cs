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

    /// <summary>
    /// Initializes a new instance of the ProjectMcpTools class
    /// </summary>
    /// <param name="projectService">Service for project instruction operations</param>
    public ProjectMcpTools(IProjectInstructionService projectService)
    {
        _projectService = projectService;
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
}
