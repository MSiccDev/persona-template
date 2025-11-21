// IPersonaInstructionService.cs - Interface for Persona Instruction Management
// Provides async operations for loading, caching, and managing persona instruction files

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PersonaMcpServer.Models;

namespace PersonaMcpServer.Services;

/// <summary>
/// Service interface for managing persona instruction files with caching support
/// </summary>
public interface IPersonaInstructionService
{
    /// <summary>
    /// Gets a persona instruction by name (without .instructions.md extension)
    /// </summary>
    /// <param name="personaName">The name of the persona to retrieve</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The persona instruction if found, null otherwise</returns>
    Task<PersonaInstruction?> GetPersonaAsync(string personaName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists all available persona instruction files in the configured repository path
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of persona names (without file extensions)</returns>
    Task<IEnumerable<string>> ListAvailablePersonasAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a persona instruction file exists
    /// </summary>
    /// <param name="personaName">The name of the persona to check</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if the persona exists, false otherwise</returns>
    Task<bool> PersonaExistsAsync(string personaName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidates the cache for a specific persona or all personas
    /// </summary>
    /// <param name="personaName">The persona name to invalidate, or null to clear entire cache</param>
    void InvalidateCache(string? personaName = null);
    
    /// <summary>
    /// Refreshes the cache by reloading all persona instructions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current active persona instruction (configured in PersonaServerConfig)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The current persona instruction if configured and found, null otherwise</returns>
    Task<PersonaInstruction?> GetCurrentPersonaAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the current active persona (updates configuration)
    /// </summary>
    /// <param name="personaName">The name of the persona to set as current</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task SetCurrentPersonaAsync(string personaName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new persona instruction file from a template with token replacement
    /// </summary>
    /// <param name="name">The name of the persona (alphanumeric, hyphens, underscores only)</param>
    /// <param name="template">The template content to use</param>
    /// <param name="replacements">Dictionary of tokens to replace (e.g., {{name}} -> value)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The full path to the created file</returns>
    /// <exception cref="ArgumentException">Thrown when name format is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when file already exists</exception>
    Task<string> CreatePersonaFromTemplateAsync(
        string name,
        string template,
        Dictionary<string, string> replacements,
        CancellationToken cancellationToken = default);
}
