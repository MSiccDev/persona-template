// ValidationResult.cs - Represents the result of validating a persona or project instruction file

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersonaMcpServer.Models;

/// <summary>
/// Represents the result of validating a persona or project instruction file.
/// </summary>
public class ValidationResult
{
/// <summary>
/// Gets or sets a value indicating whether validation passed (no errors).
/// </summary>
[JsonPropertyName("isValid")]
public bool IsValid { get; set; } = true;    /// <summary>
    /// Gets or sets the collection of validation issues found during validation.
    /// </summary>
    [JsonPropertyName("issues")]
    public List<ValidationIssue> Issues { get; set; } = new();

    /// <summary>
    /// Gets the count of error-level issues.
    /// </summary>
    [JsonIgnore]
    public int ErrorCount => Issues.Count(i => i.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Gets the count of warning-level issues.
    /// </summary>
    [JsonIgnore]
    public int WarningCount => Issues.Count(i => i.Severity == ValidationSeverity.Warning);

    /// <summary>
    /// Gets the count of info-level issues.
    /// </summary>
    [JsonIgnore]
    public int InfoCount => Issues.Count(i => i.Severity == ValidationSeverity.Info);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    public ValidationResult() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class with the specified values.
    /// </summary>
    /// <param name="isValid">Whether validation passed.</param>
    /// <param name="issues">The collection of validation issues found.</param>
    public ValidationResult(bool isValid, List<ValidationIssue>? issues = null)
    {
        IsValid = isValid;
        Issues = issues ?? new();
    }

    /// <summary>
    /// Adds an error-level issue to this validation result and updates IsValid to false.
    /// </summary>
    /// <param name="section">The section where the error was found.</param>
    /// <param name="message">The detailed message describing the error.</param>
    /// <param name="lineNumber">The optional line number where the error was found.</param>
    public void AddError(string section, string message, int? lineNumber = null)
    {
        Issues.Add(new ValidationIssue(ValidationSeverity.Error, section, message, lineNumber));
        IsValid = false;
    }

    /// <summary>
    /// Adds a warning-level issue to this validation result.
    /// </summary>
    /// <param name="section">The section where the warning was found.</param>
    /// <param name="message">The detailed message describing the warning.</param>
    /// <param name="lineNumber">The optional line number where the warning was found.</param>
    public void AddWarning(string section, string message, int? lineNumber = null)
    {
        Issues.Add(new ValidationIssue(ValidationSeverity.Warning, section, message, lineNumber));
    }

    /// <summary>
    /// Adds an info-level issue to this validation result.
    /// </summary>
    /// <param name="section">The section where the info was found.</param>
    /// <param name="message">The detailed message describing the info.</param>
    /// <param name="lineNumber">The optional line number where the info was found.</param>
    public void AddInfo(string section, string message, int? lineNumber = null)
    {
        Issues.Add(new ValidationIssue(ValidationSeverity.Info, section, message, lineNumber));
    }

    /// <summary>
    /// Converts this validation result to a JSON string representation.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation. Default is true.</param>
    /// <returns>A JSON string representation of this validation result.</returns>
    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Returns a string representation of this validation result.
    /// </summary>
    /// <returns>A formatted string summarizing the validation result.</returns>
    public override string ToString()
    {
        if (IsValid)
            return "Validation passed - no issues found.";

        var summary = $"Validation failed with {ErrorCount} error(s), {WarningCount} warning(s), {InfoCount} info(s).\n";
        summary += string.Join("\n", Issues.Select(i => i.ToString()));
        return summary;
    }
}
