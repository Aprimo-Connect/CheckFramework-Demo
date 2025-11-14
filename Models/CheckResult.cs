// ============================================================================
// Model: CheckResult
// ============================================================================
// Represents the result of running a compliance check in Aprimo.

using System.Text.Json.Serialization;

namespace Aprimo.CheckFramework.Demo.Models;

/// <summary>
/// Represents a CheckResult in the Aprimo Check Framework.
/// This is created when a compliance check is executed.
/// </summary>
public class CheckResult
{
    /// <summary>
    /// The unique identifier of the check result.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the file version that was checked.
    /// </summary>
    [JsonPropertyName("fileVersionId")]
    public string? FileVersionId { get; set; }

    /// <summary>
    /// The ID of the check that was executed.
    /// </summary>
    [JsonPropertyName("checkId")]
    public string? CheckId { get; set; }

    /// <summary>
    /// The ID of the record/asset that was checked.
    /// </summary>
    [JsonPropertyName("recordId")]
    public string? RecordId { get; set; }

    /// <summary>
    /// The outcome of the check result (e.g., "pass", "fail", "warning").
    /// </summary>
    [JsonPropertyName("outcome")]
    public string? Outcome { get; set; }

    /// <summary>
    /// Optional description/notes for the check result.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The ID of the user who created the check result.
    /// </summary>
    [JsonPropertyName("createdById")]
    public string? CreatedById { get; set; }

    /// <summary>
    /// Findings from the check (typically null).
    /// </summary>
    [JsonPropertyName("findings")]
    public object? Findings { get; set; }

    /// <summary>
    /// The check object (typically null in the response).
    /// </summary>
    [JsonPropertyName("check")]
    public object? Check { get; set; }

    /// <summary>
    /// Timestamp when the check result was created.
    /// </summary>
    [JsonPropertyName("createdOn")]
    public DateTime? CreatedOn { get; set; }

    /// <summary>
    /// The user who created the check result (typically null in the response).
    /// </summary>
    [JsonPropertyName("createdBy")]
    public object? CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the check was executed (legacy property, use CreatedOn instead).
    /// </summary>
    [JsonPropertyName("executedAt")]
    public DateTime? ExecutedAt { get; set; }
}

/// <summary>
/// Request model for creating a new check result.
/// </summary>
public class CreateCheckResultRequest
{
    /// <summary>
    /// The ID of the check to execute.
    /// </summary>
    [JsonPropertyName("checkId")]
    public string CheckId { get; set; } = string.Empty;

    /// <summary>
    /// The outcome of the check (e.g., "pass", "fail", "warning").
    /// </summary>
    [JsonPropertyName("outcome")]
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Optional description/notes for the check result.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Response model for creating a check result.
/// The API returns only the ID of the created check result.
/// </summary>
public class CreateCheckResultResponse
{
    /// <summary>
    /// The unique identifier of the created check result.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Response wrapper for the GetAllCheckResults API endpoint.
/// </summary>
public class CheckResultsResponse
{
    /// <summary>
    /// Check results collection (typically null in the response).
    /// </summary>
    [JsonPropertyName("checkResults")]
    public List<CheckResult>? CheckResults { get; set; }

    /// <summary>
    /// The list of check result items.
    /// </summary>
    [JsonPropertyName("items")]
    public List<CheckResult> Items { get; set; } = new();
}

