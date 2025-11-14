// PersonaInstruction.cs - Data Model for Persona Instruction Files
// Represents a persona instruction file with metadata and content

using System;

namespace PersonaMcpServer.Models;

/// <summary>
/// Represents a persona instruction file with its content and metadata
/// </summary>
public class PersonaInstruction
{
    /// <summary>Gets or sets the name of the persona (without file extension)</summary>
    public required string Name { get; set; }
    
    /// <summary>Gets or sets the full file path to the persona instruction file</summary>
    public required string FilePath { get; set; }
    
    /// <summary>Gets or sets the full markdown content of the persona instruction</summary>
    public required string Content { get; set; }
    
    /// <summary>Gets or sets the applyTo pattern from the YAML frontmatter (if present)</summary>
    public string? ApplyTo { get; set; }
    
    /// <summary>Gets or sets the description from the YAML frontmatter (if present)</summary>
    public string? Description { get; set; }
    
    /// <summary>Gets or sets the last modified timestamp of the file</summary>
    public DateTime LastModified { get; set; }
    
    /// <summary>Gets or sets the file size in bytes</summary>
    public long SizeBytes { get; set; }
    
    /// <summary>
    /// Returns a safe string representation for logging (excludes full content)
    /// </summary>
    public override string ToString()
    {
        var contentPreview = Content.Length > 100 
            ? Content.Substring(0, 100) + "..." 
            : Content;
        
        return $"PersonaInstruction {{ Name: {Name}, FilePath: {FilePath}, " +
               $"SizeBytes: {SizeBytes:N0}, LastModified: {LastModified:yyyy-MM-dd HH:mm:ss}, " +
               $"ApplyTo: {ApplyTo ?? "null"}, Description: {Description ?? "null"} }}";
    }
}
