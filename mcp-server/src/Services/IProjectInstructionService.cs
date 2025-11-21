// IProjectInstructionService.cs - Interface for Project Instruction Management
// Provides async operations for loading, caching, and managing project instruction files

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PersonaMcpServer.Models;

namespace PersonaMcpServer.Services;

/// <summary>
/// Service interface for managing project instruction files with caching support
/// </summary>
public interface IProjectInstructionService
{
    /// <summary>
    /// Gets a project instruction by name (without .instructions.md extension)
    /// </summary>
    /// <param name="projectName">The name of the project to retrieve</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The project instruction if found, null otherwise</returns>
    Task<ProjectInstruction?> GetProjectAsync(string projectName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists all available project instruction files in the configured repository path
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of project names (without file extensions)</returns>
    Task<IEnumerable<string>> ListAvailableProjectsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a project instruction file exists
    /// </summary>
    /// <param name="projectName">The name of the project to check</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if the project exists, false otherwise</returns>
    Task<bool> ProjectExistsAsync(string projectName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidates the cache for a specific project or all projects
    /// </summary>
    /// <param name="projectName">The project name to invalidate, or null to clear entire cache</param>
    void InvalidateCache(string? projectName = null);
    
    /// <summary>
    /// Refreshes the cache by reloading all project instructions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current active project instruction (configured in PersonaServerConfig)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The current project instruction if configured and found, null otherwise</returns>
    Task<ProjectInstruction?> GetCurrentProjectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the current active project (updates configuration)
    /// </summary>
    /// <param name="projectName">The name of the project to set as current</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task SetCurrentProjectAsync(string projectName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new project instruction file from a template with token replacement
    /// </summary>
    /// <param name="name">The name of the project (alphanumeric, hyphens, underscores only)</param>
    /// <param name="template">The template content to use</param>
    /// <param name="replacements">Dictionary of tokens to replace (e.g., {{name}} -> value)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The full path to the created file</returns>
    /// <exception cref="ArgumentException">Thrown when name format is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when file already exists</exception>
    Task<string> CreateProjectFromTemplateAsync(
        string name,
        string template,
        Dictionary<string, string> replacements,
        CancellationToken cancellationToken = default);
}
