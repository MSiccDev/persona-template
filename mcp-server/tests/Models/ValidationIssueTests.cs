// ValidationIssueTests.cs - Unit tests for ValidationIssue model

using PersonaMcpServer.Models;
using Xunit;

namespace PersonaMcpServer.Tests.Models;

public class ValidationIssueTests
{
    [Fact]
    public void Constructor_WithoutParameters_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var issue = new ValidationIssue();

        // Assert
        Assert.Equal(ValidationSeverity.Info, issue.Severity);
        Assert.Equal(string.Empty, issue.Section);
        Assert.Equal(string.Empty, issue.Message);
        Assert.Null(issue.LineNumber);
    }

    [Fact]
    public void Constructor_WithParameters_InitializesWithProvidedValues()
    {
        // Arrange
        const ValidationSeverity severity = ValidationSeverity.Error;
        const string section = "Behavior";
        const string message = "Missing required trait description";
        const int lineNumber = 42;

        // Act
        var issue = new ValidationIssue(severity, section, message, lineNumber);

        // Assert
        Assert.Equal(severity, issue.Severity);
        Assert.Equal(section, issue.Section);
        Assert.Equal(message, issue.Message);
        Assert.Equal(lineNumber, issue.LineNumber);
    }

    [Fact]
    public void Constructor_WithoutLineNumber_SetsLineNumberToNull()
    {
        // Arrange
        const ValidationSeverity severity = ValidationSeverity.Warning;
        const string section = "Metadata";
        const string message = "Incomplete metadata";

        // Act
        var issue = new ValidationIssue(severity, section, message);

        // Assert
        Assert.Equal(severity, issue.Severity);
        Assert.Equal(section, issue.Section);
        Assert.Equal(message, issue.Message);
        Assert.Null(issue.LineNumber);
    }

    [Fact]
    public void ToString_WithLineNumber_IncludesLineNumberInOutput()
    {
        // Arrange
        var issue = new ValidationIssue(
            ValidationSeverity.Error,
            "Traits",
            "Invalid trait format",
            10
        );

        // Act
        var result = issue.ToString();

        // Assert
        Assert.Contains("[Error]", result);
        Assert.Contains("Traits", result);
        Assert.Contains("(line 10)", result);
        Assert.Contains("Invalid trait format", result);
    }

    [Fact]
    public void ToString_WithoutLineNumber_OmitsLineNumberInOutput()
    {
        // Arrange
        var issue = new ValidationIssue(
            ValidationSeverity.Warning,
            "Constraints",
            "Missing constraints section"
        );

        // Act
        var result = issue.ToString();

        // Assert
        Assert.Contains("[Warning]", result);
        Assert.Contains("Constraints", result);
        Assert.DoesNotContain("(line", result);
        Assert.Contains("Missing constraints section", result);
    }
}
