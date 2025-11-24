// ValidationResultTests.cs - Unit tests for ValidationResult model

using PersonaMcpServer.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace PersonaMcpServer.Tests.Models;

public class ValidationResultTests
{
    [Fact]
    public void Constructor_WithoutParameters_InitializesAsValid()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Constructor_WithValidParameter_InitializesCorrectly()
    {
        // Arrange & Act
        var result = new ValidationResult(isValid: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Constructor_WithInvalidAndIssues_InitializesCorrectly()
    {
        // Arrange
        var issues = new List<ValidationIssue>
        {
            new(ValidationSeverity.Error, "Behavior", "Missing behavior section"),
            new(ValidationSeverity.Warning, "Traits", "Incomplete trait description")
        };

        // Act
        var result = new ValidationResult(isValid: false, issues);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Issues.Count);
    }

    [Fact]
    public void AddError_AddsErrorAndSetsIsValidToFalse()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("Metadata", "Missing required field: name");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal(ValidationSeverity.Error, result.Issues[0].Severity);
        Assert.Equal("Metadata", result.Issues[0].Section);
    }

    [Fact]
    public void AddWarning_AddsWarningWithoutChangingIsValid()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddWarning("Behavior", "Empty behavior section");

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal(ValidationSeverity.Warning, result.Issues[0].Severity);
    }

    [Fact]
    public void AddInfo_AddsInfoWithoutChangingIsValid()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddInfo("Format", "Consider using more descriptive section headers");

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal(ValidationSeverity.Info, result.Issues[0].Severity);
    }

    [Fact]
    public void ErrorCount_ReturnsCorrectCount()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Metadata", "Error 1");
        result.AddError("Behavior", "Error 2");
        result.AddWarning("Traits", "Warning 1");

        // Act
        var errorCount = result.ErrorCount;

        // Assert
        Assert.Equal(2, errorCount);
    }

    [Fact]
    public void WarningCount_ReturnsCorrectCount()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddWarning("Traits", "Warning 1");
        result.AddWarning("Constraints", "Warning 2");
        result.AddError("Metadata", "Error 1");

        // Act
        var warningCount = result.WarningCount;

        // Assert
        Assert.Equal(2, warningCount);
    }

    [Fact]
    public void InfoCount_ReturnsCorrectCount()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddInfo("Format", "Info 1");
        result.AddInfo("Style", "Info 2");
        result.AddWarning("Traits", "Warning 1");

        // Act
        var infoCount = result.InfoCount;

        // Assert
        Assert.Equal(2, infoCount);
    }

    [Fact]
    public void ToJson_WithValidResult_ProducesCorrectJson()
    {
        // Arrange
        var result = new ValidationResult(isValid: true);

        // Act
        var json = result.ToJson(indented: false);
        var parsed = JsonDocument.Parse(json);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"isValid\":true", json);
        Assert.Contains("\"issues\":[]", json);
    }

    [Fact]
    public void ToJson_WithIssues_ProducesCorrectJson()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Metadata", "Missing name field");

        // Act
        var json = result.ToJson(indented: false);
        var parsed = JsonDocument.Parse(json);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"isValid\":false", json);
        Assert.Contains("Metadata", json);
        Assert.Contains("Missing name field", json);
    }

    [Fact]
    public void ToJson_WithLineNumber_IncludesLineNumberInJson()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Behavior", "Invalid format", lineNumber: 42);

        // Act
        var json = result.ToJson(indented: false);

        // Assert
        Assert.Contains("\"lineNumber\":42", json);
    }

    [Fact]
    public void ToJson_WithoutLineNumber_OmitsLineNumberFromJson()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddWarning("Traits", "Incomplete");

        // Act
        var json = result.ToJson(indented: false);

        // Assert
        Assert.DoesNotContain("lineNumber", json);
    }

    [Fact]
    public void ToString_WhenValid_ReturnsSuccessMessage()
    {
        // Arrange
        var result = new ValidationResult(isValid: true);

        // Act
        var text = result.ToString();

        // Assert
        Assert.Contains("Validation passed", text);
    }

    [Fact]
    public void ToString_WhenInvalid_ReturnsSummaryWithIssueCounts()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Metadata", "Error 1");
        result.AddError("Behavior", "Error 2");
        result.AddWarning("Traits", "Warning 1");
        result.AddInfo("Format", "Info 1");

        // Act
        var text = result.ToString();

        // Assert
        Assert.Contains("Validation failed", text);
        Assert.Contains("2 error", text);
        Assert.Contains("1 warning", text);
        Assert.Contains("1 info", text);
    }

    [Fact]
    public void MultipleAdditions_BuildsCompleteIssueList()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("Metadata", "Missing name");
        result.AddWarning("Behavior", "Empty section");
        result.AddInfo("Format", "Suggestion");
        result.AddError("Constraints", "Invalid format", 15);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(4, result.Issues.Count);
        Assert.Equal(2, result.ErrorCount);
        Assert.Equal(1, result.WarningCount);
        Assert.Equal(1, result.InfoCount);
    }
}
