using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PersonaMcpServer.Services;

/// <summary>
/// Service for loading validation prompt files with in-memory caching.
/// </summary>
public class PromptService : IPromptService
{
    private readonly PersonaServerConfig _config;
    private readonly ILogger<PromptService> _logger;
    private readonly string _promptsPath;
    private readonly ConcurrentDictionary<string, string> _cache;

    public PromptService(IOptions<PersonaServerConfig> config, ILogger<PromptService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _promptsPath = Path.GetFullPath(Path.Combine(_config.PersonaRepoPath, "..", "prompts"));
        _cache = new ConcurrentDictionary<string, string>();
    }

    public async Task<string> GetPersonaValidationPromptAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "persona_validation";
        
        if (_cache.TryGetValue(cacheKey, out var cachedContent))
        {
            _logger.LogDebug("Returning cached persona validation prompt");
            return cachedContent;
        }

        var promptPath = Path.Combine(_promptsPath, "validate-persona-instructions.prompt.md");
        _logger.LogDebug("Loading persona validation prompt from: {PromptPath}", promptPath);

        if (!File.Exists(promptPath))
        {
            _logger.LogError("Persona validation prompt not found at: {PromptPath}", promptPath);
            throw new FileNotFoundException($"Persona validation prompt not found at: {promptPath}");
        }

        var content = await File.ReadAllTextAsync(promptPath, cancellationToken);
        _cache.TryAdd(cacheKey, content);
        _logger.LogInformation("Loaded and cached persona validation prompt ({Size} bytes)", content.Length);
        return content;
    }

    public async Task<string> GetProjectValidationPromptAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "project_validation";
        
        if (_cache.TryGetValue(cacheKey, out var cachedContent))
        {
            _logger.LogDebug("Returning cached project validation prompt");
            return cachedContent;
        }

        var promptPath = Path.Combine(_promptsPath, "validate-project-instructions.prompt.md");
        _logger.LogDebug("Loading project validation prompt from: {PromptPath}", promptPath);

        if (!File.Exists(promptPath))
        {
            _logger.LogError("Project validation prompt not found at: {PromptPath}", promptPath);
            throw new FileNotFoundException($"Project validation prompt not found at: {promptPath}");
        }

        var content = await File.ReadAllTextAsync(promptPath, cancellationToken);
        _cache.TryAdd(cacheKey, content);
        _logger.LogInformation("Loaded and cached project validation prompt ({Size} bytes)", content.Length);
        return content;
    }
}
