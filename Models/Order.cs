// ============================================================================
// Model: Order
// ============================================================================
// Represents an Order in the Aprimo DAM system for downloading files.

using System.Text.Json.Serialization;

namespace Aprimo.CheckFramework.Demo.Models;

/// <summary>
/// Request model for creating a download order.
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    /// The type of order (e.g., "download").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "download";

    /// <summary>
    /// Whether to disable notifications for this order.
    /// </summary>
    [JsonPropertyName("disableNotification")]
    public bool DisableNotification { get; set; } = true;

    /// <summary>
    /// The targets for the order (files to download).
    /// </summary>
    [JsonPropertyName("targets")]
    public List<OrderTarget> Targets { get; set; } = new();
}

/// <summary>
/// Represents a target in an order request.
/// </summary>
public class OrderTarget
{
    /// <summary>
    /// The ID of the record to download.
    /// </summary>
    [JsonPropertyName("recordId")]
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// The target types (e.g., ["Document"]).
    /// </summary>
    [JsonPropertyName("targetTypes")]
    public List<string> TargetTypes { get; set; } = new();

    /// <summary>
    /// The asset type (e.g., "LatestVersionOfMasterFile").
    /// </summary>
    [JsonPropertyName("assetType")]
    public string AssetType { get; set; } = string.Empty;
}

/// <summary>
/// Response model for an order.
/// </summary>
public class OrderResponse
{
    /// <summary>
    /// The unique identifier of the order.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The type of order.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The status of the order (e.g., "Success").
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// The URLs of the delivered files.
    /// </summary>
    [JsonPropertyName("deliveredFiles")]
    public List<string> DeliveredFiles { get; set; } = new();
}

