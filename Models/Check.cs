// ============================================================================
// Model: Check
// ============================================================================
// Represents a Check in the Aprimo Check Framework.
// Checks are pre-created in Aprimo and define what compliance rules to verify.

using System.Text.Json.Serialization;

namespace Aprimo.CheckFramework.Demo.Models;

/// <summary>
/// Represents a Check in the Aprimo Check Framework.
/// </summary>
public class Check
{
    /// <summary>
    /// The unique identifier of the check.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the check.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the action type associated with this check.
    /// </summary>
    [JsonPropertyName("actionTypeId")]
    public string ActionTypeId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the check category.
    /// </summary>
    [JsonPropertyName("checkCategoryId")]
    public string CheckCategoryId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the check category (e.g., "Risk & Compliance").
    /// </summary>
    [JsonPropertyName("checkCategoryName")]
    public string CheckCategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Localized labels for the check.
    /// </summary>
    [JsonPropertyName("labels")]
    public List<CheckLabel> Labels { get; set; } = new();

    /// <summary>
    /// The date and time when the check was created.
    /// </summary>
    [JsonPropertyName("createdOn")]
    public DateTime? CreatedOn { get; set; }

    /// <summary>
    /// The ID of the user who created the check (null if not available).
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Represents a localized label for a check.
/// </summary>
public class CheckLabel
{
    /// <summary>
    /// The localized text value of the label.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the language for this label.
    /// </summary>
    [JsonPropertyName("languageId")]
    public string LanguageId { get; set; } = string.Empty;
}

/// <summary>
/// Response wrapper for the GetChecks API endpoint.
/// </summary>
public class ChecksResponse
{
    /// <summary>
    /// Checks collection (typically null in the response).
    /// </summary>
    [JsonPropertyName("checks")]
    public List<Check>? Checks { get; set; }

    /// <summary>
    /// The list of check items.
    /// </summary>
    [JsonPropertyName("items")]
    public List<Check> Items { get; set; } = new();
}

