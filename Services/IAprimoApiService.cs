using Aprimo.CheckFramework.Demo.Models;

namespace Aprimo.CheckFramework.Demo.Services;

/// <summary>
/// Service interface for interacting with the Aprimo REST API.
/// Handles authentication, token management, and domain-specific API operations.
/// </summary>
public interface IAprimoApiService
{
   
    /// <summary>
    /// Gets an access token for the Aprimo API.
    /// This method handles token caching and refresh logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The access token string</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    
    /// <summary>
    /// Gets a specific check by its ID.
    /// </summary>
    /// <param name="checkId">The unique identifier of the check</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The check, or null if not found</returns>
    Task<Check?> GetCheckAsync(string checkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available checks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>A list of all checks</returns>
    Task<List<Check>> GetChecksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new check result by executing a compliance check.
    /// </summary>
    /// <param name="request">The check result creation request</param>
    /// <param name="fileVersionId">The ID of the file version to create the check result for</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The created check result</returns>
    Task<CheckResult> CreateCheckResultAsync(CreateCheckResultRequest request, string fileVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific check result by its ID.
    /// </summary>
    /// <param name="checkResultId">The unique identifier of the check result</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The check result, or null if not found</returns>
    Task<CheckResult?> GetCheckResultAsync(string checkResultId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all check results for a specific record/asset.
    /// </summary>
    /// <param name="recordId">The ID of the record/asset</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>A list of check results for the record</returns>
    Task<List<CheckResult>> GetCheckResultsByRecordIdAsync(string recordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the master file latest version ID for a specific record.
    /// </summary>
    /// <param name="recordId">The ID of the record/asset</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The ID of the master file latest version, or null if not found</returns>
    Task<string?> GetMasterFileLatestVersionIdAsync(string recordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all check results for a specific file version.
    /// </summary>
    /// <param name="fileVersionId">The ID of the file version</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>A list of check results for the file version</returns>
    Task<List<CheckResult>> GetAllCheckResultsAsync(string fileVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific check result.
    /// </summary>
    /// <param name="fileVersionId">The ID of the file version</param>
    /// <param name="checkResultId">The ID of the check result to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>True if the check result was successfully deleted, false otherwise</returns>
    Task<bool> DeleteCheckResultAsync(string fileVersionId, string checkResultId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a download order for a record and returns the download URL.
    /// </summary>
    /// <param name="recordId">The ID of the record to download</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The download URL, or null if the order failed or no URL was returned</returns>
    Task<string?> GetAprimoOrderAsync(string recordId, CancellationToken cancellationToken = default);
}
