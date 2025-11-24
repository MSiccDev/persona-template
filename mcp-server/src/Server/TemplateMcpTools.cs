// TemplateMcpTools.cs - MCP Tools for Template Management
// Exposes template discovery and preview operations as MCP tools

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using PersonaMcpServer.Services;

namespace PersonaMcpServer.Server;

/// <summary>
/// MCP tools for managing instruction file templates
/// </summary>
[McpServerToolType]
public class TemplateMcpTools
{
    private readonly ITemplateService _templateService;

    /// <summary>
    /// Initializes a new instance of the TemplateMcpTools class
    /// </summary>
    /// <param name="templateService">Service for template operations</param>
    public TemplateMcpTools(ITemplateService templateService)
    {
        _templateService = templateService;
    }

    /// <summary>
    /// Lists all available instruction file templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON array of available template names</returns>
    [McpServerTool(Name = "template_list")]
    [Description("Lists all available instruction file templates")]
    public async Task<string> ListTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _templateService.ListAvailableTemplatesAsync(cancellationToken);
            return JsonSerializer.Serialize(templates);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the persona instruction template content
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The persona template content as markdown</returns>
    [McpServerTool(Name = "template_get_persona")]
    [Description("Returns the persona instruction template content")]
    public async Task<string> GetPersonaTemplateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _templateService.GetPersonaTemplateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the project instruction template content
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The project template content as markdown</returns>
    [McpServerTool(Name = "template_get_project")]
    [Description("Returns the project instruction template content")]
    public async Task<string> GetProjectTemplateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _templateService.GetProjectTemplateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
