// PersonaInstructionService.cs - Implementation of IPersonaInstructionService
// Provides file I/O, memory caching, and YAML frontmatter parsing for persona instructions

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PersonaMcpServer.Models;

namespace PersonaMcpServer.Services;

/// <summary>
/// Service for managing persona instruction files with caching and file I/O
/// </summary>
public class PersonaInstructionService : IPersonaInstructionService
{
    private readonly PersonaServerConfig _config;
    private readonly ILogger<PersonaInstructionService> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly SemaphoreSlim _cacheLock;
    private long _currentCacheSize;

    private class CacheEntry
    {
        public required PersonaInstruction Instruction { get; set; }
        public DateTime CachedAt { get; set; }
        public long SizeBytes { get; set; }
    }

    public PersonaInstructionService(
        IOptions<PersonaServerConfig> config,
        ILogger<PersonaInstructionService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _cacheLock = new SemaphoreSlim(1, 1);
        _currentCacheSize = 0;
    }

    /// <inheritdoc />
    public async Task<PersonaInstruction?> GetPersonaAsync(string personaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(personaName))
        {
            _logger.LogWarning("GetPersonaAsync called with null or empty personaName");
            return null;
        }

        // Check cache first
        if (_cache.TryGetValue(personaName, out var cacheEntry))
        {
            var age = DateTime.UtcNow - cacheEntry.CachedAt;
            if (age.TotalSeconds < _config.CacheTtlSeconds)
            {
                _logger.LogDebug("Cache hit for persona '{PersonaName}' (age: {Age}s)", personaName, age.TotalSeconds);
                return cacheEntry.Instruction;
            }

            _logger.LogDebug("Cache entry expired for persona '{PersonaName}' (age: {Age}s)", personaName, age.TotalSeconds);
        }

        // Load from file system
        var filePath = GetPersonaFilePath(personaName);
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Persona file not found: {FilePath}", filePath);
            return null;
        }

        try
        {
            var instruction = await LoadPersonaFromFileAsync(filePath, personaName, cancellationToken);
            await CacheInstructionAsync(personaName, instruction, cancellationToken);
            return instruction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading persona '{PersonaName}' from {FilePath}", personaName, filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListAvailablePersonasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var repoPath = Path.GetFullPath(_config.PersonaRepoPath);
            if (!Directory.Exists(repoPath))
            {
                _logger.LogWarning("Persona repository path does not exist: {RepoPath}", repoPath);
                return Enumerable.Empty<string>();
            }

            var files = Directory.GetFiles(repoPath, "*_persona.instructions.md", SearchOption.AllDirectories);
            var personaNames = files
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Select(name => name.Replace("_persona.instructions", ""))
                .OrderBy(name => name)
                .ToList();

            _logger.LogInformation("Found {Count} persona instruction files", personaNames.Count);
            return await Task.FromResult(personaNames);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error listing available personas from {RepoPath}", _config.PersonaRepoPath);
            return Enumerable.Empty<string>();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when listing available personas from {RepoPath}", _config.PersonaRepoPath);
            return Enumerable.Empty<string>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> PersonaExistsAsync(string personaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(personaName))
        {
            return false;
        }

        var filePath = GetPersonaFilePath(personaName);
        return await Task.FromResult(File.Exists(filePath));
    }

    /// <inheritdoc />
    public void InvalidateCache(string? personaName = null)
    {
        if (personaName == null)
        {
            _logger.LogInformation("Invalidating entire persona cache");
            _cache.Clear();
            Interlocked.Exchange(ref _currentCacheSize, 0);
        }
        else if (_cache.TryRemove(personaName, out var removed))
        {
            _logger.LogInformation("Invalidated cache for persona '{PersonaName}'", personaName);
            Interlocked.Add(ref _currentCacheSize, -removed.SizeBytes);
        }
    }

    /// <inheritdoc />
    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing persona cache");
        var personaNames = await ListAvailablePersonasAsync(cancellationToken);
        
        foreach (var personaName in personaNames)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await GetPersonaAsync(personaName, cancellationToken);
        }

        _logger.LogInformation("Cache refresh complete. Cached {Count} personas, total size: {Size} bytes", 
            _cache.Count, _currentCacheSize);
    }

    /// <inheritdoc />
    public async Task<PersonaInstruction?> GetCurrentPersonaAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.CurrentPersona))
        {
            _logger.LogDebug("No current persona configured");
            return null;
        }

        return await GetPersonaAsync(_config.CurrentPersona, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetCurrentPersonaAsync(string personaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(personaName))
        {
            throw new ArgumentException("Persona name cannot be null or empty", nameof(personaName));
        }

        if (!await PersonaExistsAsync(personaName, cancellationToken))
        {
            throw new FileNotFoundException($"Persona '{personaName}' does not exist");
        }

        _config.CurrentPersona = personaName;
        _logger.LogInformation("Set current persona to '{PersonaName}'", personaName);
    }

    private string GetPersonaFilePath(string personaName)
    {
        // Sanitize personaName to prevent path traversal
        var safePersonaName = Path.GetFileName(personaName);
        if (!IsValidPersonaName(safePersonaName))
        {
            throw new ArgumentException("Persona name contains invalid characters.", nameof(personaName));
        }
        var repoPath = Path.GetFullPath(_config.PersonaRepoPath);
        var fileName = $"{safePersonaName}_persona.instructions.md";
        return Path.Combine(repoPath, fileName);
    }

    /// <summary>
    /// Validates that the persona name contains only allowed characters (alphanumeric, underscore, hyphen).
    /// </summary>
    private static bool IsValidPersonaName(string personaName)
    {
        // Only allow letters, numbers, underscores, and hyphens
        return Regex.IsMatch(personaName, @"^[a-zA-Z0-9_\-]+$");
    }
    private async Task<PersonaInstruction> LoadPersonaFromFileAsync(
        string filePath, 
        string personaName, 
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var fileInfo = new FileInfo(filePath);

        // Parse YAML frontmatter if present
        string? applyTo = null;
        string? description = null;

        var yamlMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);
        if (yamlMatch.Success)
        {
            var yamlContent = yamlMatch.Groups[1].Value;
            
            var applyToMatch = Regex.Match(yamlContent, @"applyTo:\s*['""]?(.+?)['""]?\s*$", RegexOptions.Multiline);
            if (applyToMatch.Success)
            {
                applyTo = applyToMatch.Groups[1].Value.Trim();
            }

            var descMatch = Regex.Match(yamlContent, @"description:\s*['""]?(.+?)['""]?\s*$", RegexOptions.Multiline);
            if (descMatch.Success)
            {
                description = descMatch.Groups[1].Value.Trim();
            }
        }

        _logger.LogDebug("Loaded persona '{PersonaName}' from {FilePath} ({Size} bytes)", 
            personaName, filePath, fileInfo.Length);

        return new PersonaInstruction
        {
            Name = personaName,
            FilePath = filePath,
            Content = content,
            ApplyTo = applyTo,
            Description = description,
            LastModified = fileInfo.LastWriteTimeUtc,
            SizeBytes = fileInfo.Length
        };
    }

    private async Task CacheInstructionAsync(
        string personaName, 
        PersonaInstruction instruction, 
        CancellationToken cancellationToken)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            var entry = new CacheEntry
            {
                Instruction = instruction,
                CachedAt = DateTime.UtcNow,
                SizeBytes = instruction.SizeBytes
            };

            // Check cache size limits
            var newSize = Interlocked.Add(ref _currentCacheSize, instruction.SizeBytes);
            if (newSize > _config.MaxCacheSizeBytes)
            {
                _logger.LogWarning("Cache size ({CurrentSize} bytes) exceeds limit ({MaxSize} bytes), evicting oldest entries",
                    newSize, _config.MaxCacheSizeBytes);
                await EvictOldestEntriesAsync(cancellationToken);
            }

            _cache.AddOrUpdate(personaName, entry, (_, __) => entry);
            _logger.LogDebug("Cached persona '{PersonaName}' ({Size} bytes)", personaName, instruction.SizeBytes);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task EvictOldestEntriesAsync(CancellationToken cancellationToken)
    {
        var targetSize = _config.MaxCacheSizeBytes / 2; // Evict to 50% capacity
        var sortedEntries = _cache
            .OrderBy(kvp => kvp.Value.CachedAt)
            .ToList();

        foreach (var kvp in sortedEntries)
        {
            if (_currentCacheSize <= targetSize || cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (_cache.TryRemove(kvp.Key, out var removed))
            {
                Interlocked.Add(ref _currentCacheSize, -removed.SizeBytes);
                _logger.LogDebug("Evicted persona '{PersonaName}' from cache", kvp.Key);
            }
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<string> CreatePersonaFromTemplateAsync(
        string name,
        string template,
        Dictionary<string, string> replacements,
        CancellationToken cancellationToken = default)
    {
        // Validate name format
        ValidateInstructionName(name);

        // Construct file path
        var filePath = GetPersonaFilePath(name);

        // Check if file already exists
        if (File.Exists(filePath))
        {
            _logger.LogError("Persona file already exists: {FilePath}", filePath);
            throw new InvalidOperationException($"Persona file '{name}_persona.instructions.md' already exists");
        }

        // Perform token replacement
        var content = template;
        foreach (var kvp in replacements)
        {
            // If key already contains brackets/angle brackets, use as-is
            // Otherwise wrap in double curly braces for {{TOKEN}} format
            var token = kvp.Key.StartsWith("[") || kvp.Key.StartsWith("<") || kvp.Key.StartsWith("{{")
                ? kvp.Key
                : $"{{{{{kvp.Key}}}}}";
            content = content.Replace(token, kvp.Value, StringComparison.Ordinal);
        }

        // Write file
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
        _logger.LogInformation("Created persona file: {FilePath}", filePath);

        // Invalidate cache to ensure fresh load
        InvalidateCache(name);

        return filePath;
    }

    private static void ValidateInstructionName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or whitespace", nameof(name));
        }

        // Only allow alphanumeric, hyphens, and underscores
        var namePattern = new Regex(@"^[a-zA-Z0-9_-]+$");
        if (!namePattern.IsMatch(name))
        {
            throw new ArgumentException(
                "Name must contain only alphanumeric characters, hyphens, and underscores",
                nameof(name));
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidatePersonaAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        try
        {
            if (!File.Exists(filePath))
            {
                result.AddError("File", $"Persona instruction file not found at path: {filePath}");
                return result;
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                result.AddError("Content", "Persona instruction file is empty");
                return result;
            }

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            // Check for YAML frontmatter
            if (!lines.FirstOrDefault()?.StartsWith("---") ?? false)
            {
                result.AddWarning("Metadata", "Missing YAML frontmatter (---) at start of file");
            }

            // Check for required sections (heuristic: look for markdown headers)
            var sectionHeaders = new[] { "# Metadata", "# Behavior", "# Traits", "# Constraints" };
            var foundSections = new HashSet<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                foreach (var section in sectionHeaders)
                {
                    if (lines[i].StartsWith(section))
                    {
                        foundSections.Add(section);
                    }
                }
            }

            // Warn about missing recommended sections
            if (!foundSections.Contains("# Metadata"))
            {
                result.AddWarning("Metadata", "Recommended section '# Metadata' not found");
            }
            if (!foundSections.Contains("# Behavior"))
            {
                result.AddWarning("Behavior", "Recommended section '# Behavior' not found");
            }
            if (!foundSections.Contains("# Traits"))
            {
                result.AddWarning("Traits", "Recommended section '# Traits' not found");
            }
            if (!foundSections.Contains("# Constraints"))
            {
                result.AddWarning("Constraints", "Recommended section '# Constraints' not found");
            }

            // Check for minimum content length
            if (content.Length < 100)
            {
                result.AddWarning("Content", "Persona instruction content is quite short (< 100 characters)");
            }

            // Check for proper markdown formatting
            var headerCount = lines.Count(l => l.StartsWith("#"));
            if (headerCount < 2)
            {
                result.AddInfo("Format", "Consider adding section headers (# Header) for better structure");
            }
        }
        catch (Exception ex)
        {
            result.AddError("Exception", $"Error validating persona: {ex.Message}");
            _logger.LogError(ex, "Error validating persona at {FilePath}", filePath);
        }

        return result;
    }
}