// ============================================================================
// Model: Record
// ============================================================================
// Represents a Record (Asset) in the Aprimo DAM system.

using System.Text.Json.Serialization;

namespace Aprimo.CheckFramework.Demo.Models;

/// <summary>
/// Represents a Record (Asset) in the Aprimo DAM system.
/// </summary>
public class Record
{
    /// <summary>
    /// The unique identifier of the record.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The master file latest version information.
    /// </summary>
    [JsonPropertyName("masterFileLatestVersion")]
    public MasterFileVersion? MasterFileLatestVersion { get; set; }
}

/// <summary>
/// Represents a master file version in Aprimo.
/// </summary>
public class MasterFileVersion
{
    /// <summary>
    /// The unique identifier of the file version.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The version label.
    /// </summary>
    [JsonPropertyName("versionLabel")]
    public string? VersionLabel { get; set; }

    /// <summary>
    /// The version number.
    /// </summary>
    [JsonPropertyName("versionNumber")]
    public int? VersionNumber { get; set; }

    /// <summary>
    /// The file name.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    /// <summary>
    /// Whether this is the latest version.
    /// </summary>
    [JsonPropertyName("isLatest")]
    public bool? IsLatest { get; set; }
}

