// PersonaMcpTools.cs - MCP Tools for Persona Management
// Exposes persona instruction operations as MCP tools

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using PersonaMcpServer.Services;

namespace PersonaMcpServer.Server;

/// <summary>
/// MCP tools for managing persona instruction files
/// </summary>
[McpServerToolType]
public class PersonaMcpTools
{
    private readonly IPersonaInstructionService _personaService;

    /// <summary>
    /// Initializes a new instance of the PersonaMcpTools class
    /// </summary>
    /// <param name="personaService">Service for persona instruction operations</param>
    public PersonaMcpTools(IPersonaInstructionService personaService)
    {
        _personaService = personaService;
    }

    /// <summary>
    /// Lists all available persona instruction files
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of available persona names</returns>
    [McpServerTool(Name = "persona_list")]
    [Description("Lists all available persona instruction files in the repository")]
    public async Task<string[]> ListPersonasAsync(CancellationToken cancellationToken = default)
    {
        var personas = await _personaService.ListAvailablePersonasAsync(cancellationToken);
        return [.. personas];
    }

    /// <summary>
    /// Gets a specific persona instruction by name
    /// </summary>
    /// <param name="name">The name of the persona to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON representation of the persona instruction</returns>
    [McpServerTool(Name = "persona_get")]
    [Description("Retrieves a specific persona instruction file by name")]
    public async Task<string> GetPersonaAsync(
        [Description("The name of the persona to retrieve")] string name,
        CancellationToken cancellationToken = default)
    {
        var persona = await _personaService.GetPersonaAsync(name, cancellationToken);
        
        if (persona == null)
        {
            return $"{{\"error\": \"Persona '{name}' not found\"}}";
        }

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            name = persona.Name,
            content = persona.Content,
            applyTo = persona.ApplyTo,
            description = persona.Description,
            filePath = persona.FilePath,
            sizeBytes = persona.SizeBytes,
            lastModified = persona.LastModified
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Checks if a persona exists
    /// </summary>
    /// <param name="name">The name of the persona to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if persona exists, false otherwise</returns>
    [McpServerTool(Name = "persona_exists")]
    [Description("Checks if a persona instruction file exists")]
    public async Task<bool> PersonaExistsAsync(
        [Description("The name of the persona to check")] string name,
        CancellationToken cancellationToken = default)
    {
        return await _personaService.PersonaExistsAsync(name, cancellationToken);
    }

    /// <summary>
    /// Gets the current active persona
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON representation of the current persona, or null if none set</returns>
    [McpServerTool(Name = "persona_get_current")]
    [Description("Retrieves the currently active persona instruction")]
    public async Task<string> GetCurrentPersonaAsync(CancellationToken cancellationToken = default)
    {
        var persona = await _personaService.GetCurrentPersonaAsync(cancellationToken);
        
        if (persona == null)
        {
            return "{\"current\": null, \"message\": \"No current persona configured\"}";
        }

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            current = persona.Name,
            content = persona.Content,
            applyTo = persona.ApplyTo,
            description = persona.Description
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Sets the current active persona
    /// </summary>
    /// <param name="name">The name of the persona to set as current</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message or error</returns>
    [McpServerTool(Name = "persona_set_current")]
    [Description("Sets the currently active persona instruction")]
    public async Task<string> SetCurrentPersonaAsync(
        [Description("The name of the persona to set as current")] string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _personaService.SetCurrentPersonaAsync(name, cancellationToken);
            return $"{{\"success\": true, \"message\": \"Current persona set to '{name}'\"}}";
        }
        catch (FileNotFoundException)
        {
            return $"{{\"error\": \"Persona '{name}' not found\"}}";
        }
        catch (Exception ex)
        {
            return $"{{\"error\": \"{ex.Message}\"}}";
        }
    }

    /// <summary>
    /// Invalidates the persona cache
    /// </summary>
    /// <param name="name">Optional: specific persona name to invalidate, or null for all</param>
    /// <returns>Success message</returns>
    [McpServerTool(Name = "persona_invalidate_cache")]
    [Description("Invalidates the persona instruction cache (optionally for a specific persona)")]
    public string InvalidateCacheAsync(
        [Description("Optional: specific persona name to invalidate (null = all)")] string? name = null)
    {
        _personaService.InvalidateCache(name);
        var target = string.IsNullOrWhiteSpace(name) ? "all personas" : $"persona '{name}'";
        return $"{{\"success\": true, \"message\": \"Cache invalidated for {target}\"}}";
    }

    /// <summary>
    /// Refreshes the persona cache by preloading all personas
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [McpServerTool(Name = "persona_refresh_cache")]
    [Description("Refreshes the persona cache by preloading all available personas")]
    public async Task<string> RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        await _personaService.RefreshCacheAsync(cancellationToken);
        return "{\"success\": true, \"message\": \"Persona cache refreshed\"}";
    }
}
