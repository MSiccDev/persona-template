// ValidationIssue.cs - Represents a single validation issue for a persona or project

namespace PersonaMcpServer.Models;

/// <summary>
/// Represents a single validation issue found during persona or project validation.
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// Gets or sets the severity level of this issue.
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the section or area of the instruction file where this issue was found.
    /// Examples: "Metadata", "Behavior", "Traits", "Constraints"
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed message describing the validation issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional line number where the issue was found in the file.
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationIssue"/> class.
    /// </summary>
    public ValidationIssue() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationIssue"/> class with the specified values.
    /// </summary>
    /// <param name="severity">The severity level of the issue.</param>
    /// <param name="section">The section where the issue was found.</param>
    /// <param name="message">The detailed message describing the issue.</param>
    /// <param name="lineNumber">The optional line number where the issue was found.</param>
    public ValidationIssue(ValidationSeverity severity, string section, string message, int? lineNumber = null)
    {
        Severity = severity;
        Section = section;
        Message = message;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns a string representation of this validation issue.
    /// </summary>
    /// <returns>A formatted string describing the issue.</returns>
    public override string ToString()
    {
        var location = LineNumber.HasValue ? $" (line {LineNumber})" : string.Empty;
        return $"[{Severity}] {Section}{location}: {Message}";
    }
}

/// <summary>
/// Enumerates the possible severity levels for validation issues.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational issue that does not affect functionality.
    /// </summary>
    Info,

    /// <summary>
    /// Warning that indicates a potential issue but does not prevent functionality.
    /// </summary>
    Warning,

    /// <summary>
    /// Error that indicates a critical issue that must be resolved.
    /// </summary>
    Error
}
