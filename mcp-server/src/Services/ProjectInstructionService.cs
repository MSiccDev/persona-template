// ProjectInstructionService.cs - Implementation of IProjectInstructionService
// Provides file I/O, memory caching, and YAML frontmatter parsing for project instructions

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
/// Service for managing project instruction files with caching and file I/O
/// </summary>
public class ProjectInstructionService : IProjectInstructionService
{
    private readonly PersonaServerConfig _config;
    private readonly ILogger<ProjectInstructionService> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly SemaphoreSlim _cacheLock;
    private long _currentCacheSize;

    private class CacheEntry
    {
        public required ProjectInstruction Instruction { get; set; }
        public DateTime CachedAt { get; set; }
        public long SizeBytes { get; set; }
    }

    public ProjectInstructionService(
        IOptions<PersonaServerConfig> config,
        ILogger<ProjectInstructionService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _cacheLock = new SemaphoreSlim(1, 1);
        _currentCacheSize = 0;
    }

    /// <inheritdoc />
    public async Task<ProjectInstruction?> GetProjectAsync(string projectName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            _logger.LogWarning("GetProjectAsync called with null or empty projectName");
            return null;
        }

        // Check cache first
        if (_cache.TryGetValue(projectName, out var cacheEntry))
        {
            var age = DateTime.UtcNow - cacheEntry.CachedAt;
            if (age.TotalSeconds < _config.CacheTtlSeconds)
            {
                _logger.LogDebug("Cache hit for project '{ProjectName}' (age: {Age}s)", projectName, age.TotalSeconds);
                return cacheEntry.Instruction;
            }

            _logger.LogDebug("Cache entry expired for project '{ProjectName}' (age: {Age}s)", projectName, age.TotalSeconds);
        }

        // Load from file system
        var filePath = GetProjectFilePath(projectName);
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Project file not found: {FilePath}", filePath);
            return null;
        }

        try
        {
            var instruction = await LoadProjectFromFileAsync(filePath, projectName, cancellationToken);
            await CacheInstructionAsync(projectName, instruction, cancellationToken);
            return instruction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading project '{ProjectName}' from {FilePath}", projectName, filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListAvailableProjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var repoPath = Path.GetFullPath(_config.PersonaRepoPath);
            if (!Directory.Exists(repoPath))
            {
                _logger.LogWarning("Project repository path does not exist: {RepoPath}", repoPath);
                return Enumerable.Empty<string>();
            }

            var files = Directory.GetFiles(repoPath, "*_project.instructions.md", SearchOption.AllDirectories);
            var projectNames = files
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Select(name => name.Replace("_project.instructions", ""))
                .OrderBy(name => name)
                .ToList();

            _logger.LogInformation("Found {Count} project instruction files", projectNames.Count);
            return await Task.FromResult(projectNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing available projects from {RepoPath}", _config.PersonaRepoPath);
            return Enumerable.Empty<string>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ProjectExistsAsync(string projectName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return false;
        }

        var filePath = GetProjectFilePath(projectName);
        return await Task.FromResult(File.Exists(filePath));
    }

    /// <inheritdoc />
    public void InvalidateCache(string? projectName = null)
    {
        if (projectName == null)
        {
            _logger.LogInformation("Invalidating entire project cache");
            _cache.Clear();
            Interlocked.Exchange(ref _currentCacheSize, 0);
        }
        else if (_cache.TryRemove(projectName, out var removed))
        {
            _logger.LogInformation("Invalidated cache for project '{ProjectName}'", projectName);
            Interlocked.Add(ref _currentCacheSize, -removed.SizeBytes);
        }
    }

    /// <inheritdoc />
    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing project cache");
        var projectNames = await ListAvailableProjectsAsync(cancellationToken);
        
        foreach (var projectName in projectNames)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await GetProjectAsync(projectName, cancellationToken);
        }

        _logger.LogInformation("Cache refresh complete. Cached {Count} projects, total size: {Size} bytes", 
            _cache.Count, _currentCacheSize);
    }

    /// <inheritdoc />
    public async Task<ProjectInstruction?> GetCurrentProjectAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.CurrentProject))
        {
            _logger.LogDebug("No current project configured");
            return null;
        }

        return await GetProjectAsync(_config.CurrentProject, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetCurrentProjectAsync(string projectName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name cannot be null or empty", nameof(projectName));
        }

        if (!await ProjectExistsAsync(projectName, cancellationToken))
        {
            throw new FileNotFoundException($"Project '{projectName}' does not exist");
        }

        _config.CurrentProject = projectName;
        _logger.LogInformation("Set current project to '{ProjectName}'", projectName);
    }

    private string GetProjectFilePath(string projectName)
    {
        // Validate projectName to prevent path traversal and injection attacks
        // Only allow alphanumeric, dash, and underscore characters
        if (!Regex.IsMatch(projectName, @"^[a-zA-Z0-9_-]+$"))
        {
            throw new ArgumentException("Invalid project name. Only alphanumeric characters, dash, and underscore are allowed.", nameof(projectName));
        }
        var repoPath = Path.GetFullPath(_config.PersonaRepoPath);
        var fileName = $"{projectName}_project.instructions.md";
        return Path.Combine(repoPath, fileName);
    }

    private async Task<ProjectInstruction> LoadProjectFromFileAsync(
        string filePath, 
        string projectName, 
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

        _logger.LogDebug("Loaded project '{ProjectName}' from {FilePath} ({Size} bytes)", 
            projectName, filePath, fileInfo.Length);

        return new ProjectInstruction
        {
            Name = projectName,
            FilePath = filePath,
            Content = content,
            ApplyTo = applyTo,
            Description = description,
            LastModified = fileInfo.LastWriteTimeUtc,
            SizeBytes = fileInfo.Length
        };
    }

    private async Task CacheInstructionAsync(
        string projectName, 
        ProjectInstruction instruction, 
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

            _cache.AddOrUpdate(projectName, entry, (_, __) => entry);
            _logger.LogDebug("Cached project '{ProjectName}' ({Size} bytes)", projectName, instruction.SizeBytes);
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
                _logger.LogDebug("Evicted project '{ProjectName}' from cache", kvp.Key);
            }
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<string> CreateProjectFromTemplateAsync(
        string name,
        string template,
        Dictionary<string, string> replacements,
        CancellationToken cancellationToken = default)
    {
        // Validate name format
        ValidateInstructionName(name);

        // Construct file path
        var filePath = GetProjectFilePath(name);

        // Ensure projects directory exists
        var projectsDir = Path.GetDirectoryName(filePath);
        if (projectsDir != null && !Directory.Exists(projectsDir))
        {
            Directory.CreateDirectory(projectsDir);
            _logger.LogDebug("Created projects directory: {Directory}", projectsDir);
        }

        // Check if file already exists
        if (File.Exists(filePath))
        {
            _logger.LogError("Project file already exists: {FilePath}", filePath);
            throw new InvalidOperationException($"Project file '{name}_project.instructions.md' already exists");
        }

        // Perform token replacement
        var content = template;
        foreach (var kvp in replacements)
        {
            var token = $"{{{{{kvp.Key}}}}}";
            content = content.Replace(token, kvp.Value, StringComparison.Ordinal);
        }

        // Write file
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
        _logger.LogInformation("Created project file: {FilePath}", filePath);

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
}
