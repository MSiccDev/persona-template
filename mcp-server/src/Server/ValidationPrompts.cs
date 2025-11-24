// ValidationPrompts.cs - MCP Prompts for Validation Workflows
// Exposes validation prompt workflows as MCP prompts

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using PersonaMcpServer.Services;

namespace PersonaMcpServer.Server;

/// <summary>
/// MCP prompts for validation workflows
/// </summary>
[McpServerPromptType]
public class ValidationPrompts
{
    private readonly IPromptService _promptService;

    /// <summary>
    /// Initializes a new instance of the ValidationPrompts class
    /// </summary>
    /// <param name="promptService">Service for prompt operations</param>
    public ValidationPrompts(IPromptService promptService)
    {
        _promptService = promptService;
    }

    /// <summary>
    /// Gets the persona validation prompt with optional persona file content
    /// </summary>
    /// <param name="personaContent">Optional: content of the persona file to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Persona validation prompt with inserted context</returns>
    [McpServerPrompt(Name = "validate_persona_prompt")]
    [Description("Provides a structured prompt for validating persona instruction files")]
    public async Task<string> GetPersonaValidationPromptAsync(
        [Description("Optional persona file content to include in the prompt context")] string? personaContent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var basePrompt = await _promptService.GetPersonaValidationPromptAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(personaContent))
            {
                return basePrompt;
            }

            // Insert persona content into the prompt template
            var promptWithContext = basePrompt.Replace(
                "<!-- INSERT_PERSONA_CONTENT -->",
                $"```markdown\n{personaContent}\n```",
                StringComparison.OrdinalIgnoreCase);

            return promptWithContext;
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to retrieve persona validation prompt: {ex.Message}" });
        }
    }

    /// <summary>
    /// Gets the project validation prompt with optional project file content
    /// </summary>
    /// <param name="projectContent">Optional: content of the project file to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Project validation prompt with inserted context</returns>
    [McpServerPrompt(Name = "validate_project_prompt")]
    [Description("Provides a structured prompt for validating project instruction files")]
    public async Task<string> GetProjectValidationPromptAsync(
        [Description("Optional project file content to include in the prompt context")] string? projectContent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var basePrompt = await _promptService.GetProjectValidationPromptAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(projectContent))
            {
                return basePrompt;
            }

            // Insert project content into the prompt template
            var promptWithContext = basePrompt.Replace(
                "<!-- INSERT_PROJECT_CONTENT -->",
                $"```markdown\n{projectContent}\n```",
                StringComparison.OrdinalIgnoreCase);

            return promptWithContext;
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to retrieve project validation prompt: {ex.Message}" });
        }
    }
}
