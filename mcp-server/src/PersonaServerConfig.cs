// PersonaServerConfig.cs - Configuration Model
// Implements configuration schema with validation and environment override support

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace PersonaMcpServer;

/// <summary>
/// Configuration model for the PersonaMcpServer with validation and defaults
/// </summary>
public class PersonaServerConfig
{
    /// <summary>Host to listen on (default: localhost)</summary>
    [Required]
    public string Host { get; set; } = "localhost";
    
    /// <summary>Port to listen on (default: 3000)</summary>
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int Port { get; set; } = 3000;
    
    /// <summary>Transport mechanism (SSE or STDIO)</summary>
    [Required]
    [RegularExpression("^(SSE|STDIO)$", ErrorMessage = "Transport must be 'SSE' or 'STDIO'")]
    public string Transport { get; set; } = "SSE";
    
    /// <summary>Path to persona-template repository (default: ../)</summary>
    [Required]
    public string PersonaRepoPath { get; set; } = "../";
    
    /// <summary>Name of current persona file</summary>
    public string? CurrentPersona { get; set; }
    
    /// <summary>Name of current project file</summary>
    public string? CurrentProject { get; set; }
    
    /// <summary>Cache TTL in seconds (default: 300)</summary>
    [Range(1, int.MaxValue, ErrorMessage = "CacheTtlSeconds must be >= 1")]
    public int CacheTtlSeconds { get; set; } = 300;
    
    /// <summary>Max cache size in bytes (default: 100MB)</summary>
    [Range(1024, long.MaxValue, ErrorMessage = "MaxCacheSizeBytes must be >= 1024 (1KB)")]
    public long MaxCacheSizeBytes { get; set; } = 100 * 1024 * 1024;
    
    /// <summary>
    /// Validation: Ensure configuration is valid using Data Annotations and custom logic
    /// </summary>
    /// <param name="errors">List of validation errors if any</param>
    /// <returns>True if configuration is valid, false otherwise</returns>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();
        
        // Use DataAnnotations validation
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            errors.AddRange(validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error"));
        }
        
        // Custom validation logic
        if (string.IsNullOrWhiteSpace(Host))
        {
            errors.Add("Host cannot be null or whitespace");
        }
        
        if (string.IsNullOrWhiteSpace(PersonaRepoPath))
        {
            errors.Add("PersonaRepoPath cannot be null or whitespace");
        }
        
        // Validate path exists (if not relative)
        if (!string.IsNullOrWhiteSpace(PersonaRepoPath) && 
            Path.IsPathFullyQualified(PersonaRepoPath) && 
            !Directory.Exists(PersonaRepoPath))
        {
            errors.Add($"PersonaRepoPath directory does not exist: {PersonaRepoPath}");
        }
        
        return errors.Count == 0;
    }
    
    /// <summary>
    /// Gets a string representation of the configuration for logging (excludes sensitive data)
    /// </summary>
    /// <returns>Safe string representation for logging</returns>
    public override string ToString()
    {
        return $"PersonaServerConfig {{ Host: {Host}, Port: {Port}, Transport: {Transport}, " +
               $"PersonaRepoPath: {PersonaRepoPath}, CacheTtlSeconds: {CacheTtlSeconds}, " +
               $"MaxCacheSizeBytes: {MaxCacheSizeBytes:N0} }}";
    }
}