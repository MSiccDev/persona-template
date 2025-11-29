using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PersonaMcpServer.Services;

/// <summary>
/// Service for loading and managing instruction file templates.
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly PersonaServerConfig _config;
    private readonly ILogger<TemplateService> _logger;
    private readonly string _templatesPath;

    public TemplateService(IOptions<PersonaServerConfig> config, ILogger<TemplateService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _templatesPath = RepositoryResourceResolver.Resolve(_config.PersonaRepoPath, "templates", _logger);
    }

    public async Task<string> GetPersonaTemplateAsync(CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(_templatesPath, "persona_template.instructions.md");
        _logger.LogDebug("Loading persona template from: {TemplatePath}", templatePath);

        if (!File.Exists(templatePath))
        {
            _logger.LogError("Persona template not found at: {TemplatePath}", templatePath);
            throw new FileNotFoundException($"Persona template not found at: {templatePath}");
        }

        var content = await File.ReadAllTextAsync(templatePath, cancellationToken);
        _logger.LogInformation("Loaded persona template ({Size} bytes)", content.Length);
        return content;
    }

    public async Task<string> GetProjectTemplateAsync(CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(_templatesPath, "project_template.instructions.md");
        _logger.LogDebug("Loading project template from: {TemplatePath}", templatePath);

        if (!File.Exists(templatePath))
        {
            _logger.LogError("Project template not found at: {TemplatePath}", templatePath);
            throw new FileNotFoundException($"Project template not found at: {templatePath}");
        }

        var content = await File.ReadAllTextAsync(templatePath, cancellationToken);
        _logger.LogInformation("Loaded project template ({Size} bytes)", content.Length);
        return content;
    }

    public Task<List<string>> ListAvailableTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing available templates from: {TemplatesPath}", _templatesPath);

        if (!Directory.Exists(_templatesPath))
        {
            _logger.LogWarning("Templates directory not found: {TemplatesPath}", _templatesPath);
            return Task.FromResult(new List<string>());
        }

        var templates = Directory.GetFiles(_templatesPath, "*_template.instructions.md")
            .Select(f => Path.GetFileNameWithoutExtension(f)!)
            .ToList();

        _logger.LogInformation("Found {Count} template(s)", templates.Count);
        return Task.FromResult(templates);
    }
}
