using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aprimo.CheckFramework.Demo.Models;

namespace Aprimo.CheckFramework.Demo.Services;

/// <summary>
/// Implementation of IAprimoApiService for interacting with the Aprimo REST API.
/// Uses IHttpClientFactory for proper HttpClient management.
/// </summary>
public class AprimoApiService : IAprimoApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AprimoApiService> _logger;
    
    private string? _cachedToken;
    private DateTime? _tokenExpiresAt;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AprimoApiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AprimoApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets the tenant ID from configuration.
    /// Used to construct the tenant-specific base URLs.
    /// </summary>
    private string? TenantId => 
        _configuration["Aprimo:TenantId"] 
        ?? _configuration["Aprimo:APRIMO_TENANT_ID"]  // Support legacy config key
        ?? Environment.GetEnvironmentVariable("APRIMO_TENANT_ID");

    /// <summary>
    /// Gets the base URL for Aprimo authentication.
    /// Format: https://{tenantId}.aprimo.com
    /// </summary>
    private string GetAuthBaseUrl()
    {
        var tenantId = TenantId;
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException(
                "Aprimo Tenant ID not configured. Please provide APRIMO_TENANT_ID in your configuration.");
        }
        return $"https://{tenantId}.aprimo.com";
    }

    /// <summary>
    /// Gets the base URL for Aprimo DAM API requests.
    /// Format: https://{tenantId}.dam.aprimo.com
    /// </summary>
    private string GetDamApiBaseUrl()
    {
        var tenantId = TenantId;
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException(
                "Aprimo Tenant ID not configured. Please provide APRIMO_TENANT_ID in your configuration.");
        }
        return $"https://{tenantId}.dam.aprimo.com";
    }


    /// <summary>
    /// Gets the client ID for OAuth2 authentication from configuration.
    /// </summary>
    private string? ClientId => 
        _configuration["Aprimo:ClientId"] 
        ?? Environment.GetEnvironmentVariable("APRIMO_CLIENT_ID");

    /// <summary>
    /// Gets the client secret for OAuth2 authentication from configuration.
    /// </summary>
    private string? ClientSecret => 
        _configuration["Aprimo:ClientSecret"] 
        ?? Environment.GetEnvironmentVariable("APRIMO_CLIENT_SECRET");

    /// <summary>
    /// Gets an access token for the Aprimo API.
    /// Implements token caching to avoid unnecessary authentication requests.
    /// Uses OAuth2 client credentials flow with the endpoint: /login/connect/token
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && 
            _tokenExpiresAt.HasValue && 
            _tokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(5))
        {
            return _cachedToken;
        }

        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
        {
            throw new InvalidOperationException(
                "Aprimo API credentials not configured. Please provide APRIMO_CLIENT_ID and APRIMO_CLIENT_SECRET in your configuration.");
        }

        var httpClient = _httpClientFactory.CreateClient();
        var authBaseUrl = GetAuthBaseUrl();
        httpClient.BaseAddress = new Uri(authBaseUrl);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        
        try
        {
            _logger.LogInformation("Authenticating with Aprimo API at {BaseUrl} using OAuth2 client credentials flow", authBaseUrl);
            
            var formData = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("scope", "api"),
                new("client_id", ClientId),
                new("client_secret", ClientSecret)
            };

            var formContent = new FormUrlEncodedContent(formData);
            var response = await httpClient.PostAsync("/login/connect/token", formContent, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(
                    JsonOptions,
                    cancellationToken);

                if (authResponse?.AccessToken != null)
                {
                    _cachedToken = authResponse.AccessToken;
                    _tokenExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn ?? 3600);
                    _logger.LogInformation("Successfully obtained access token. Expires in {ExpiresIn} seconds", authResponse.ExpiresIn);
                    return _cachedToken;
                }
                else
                {
                    throw new InvalidOperationException("Access token not found in authentication response");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OAuth2 authentication failed. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException(
                    $"Failed to authenticate with Aprimo API. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HTTP exceptions as-is
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw validation exceptions as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting access token from Aprimo API");
            throw;
        }
    }

    /// <summary>
    /// Creates an authenticated HttpClient with the access token in the Authorization header.
    /// Uses the DAM API base URL (https://{tenantId}.dam.aprimo.com) for all API requests.
    /// Adds required headers including API-VERSION for all DAM API requests.
    /// </summary>
    private async Task<HttpClient> CreateAuthenticatedClientAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var damApiBaseUrl = GetDamApiBaseUrl();
        client.BaseAddress = new Uri(damApiBaseUrl);
        
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "Compliance Check POC");
        client.DefaultRequestHeaders.Add("API-VERSION", "1");
        
        var token = await GetAccessTokenAsync(cancellationToken);
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }


    /// <summary>
    /// Gets a specific check by its ID.
    /// </summary>
    public async Task<Check?> GetCheckAsync(string checkId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            var response = await client.GetAsync($"/api/core/checks/{checkId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to get check. CheckId: {CheckId}, Status: {StatusCode}, Error: {Error}",
                    checkId, response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Check>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting check with ID: {CheckId}", checkId);
            throw;
        }
    }

    /// <summary>
    /// Gets all available checks.
    /// </summary>
    public async Task<List<Check>> GetChecksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            var response = await client.GetAsync("/api/core/checks", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to get checks. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                return new List<Check>();
            }

            var checksResponse = await response.Content.ReadFromJsonAsync<ChecksResponse>(JsonOptions, cancellationToken);
            return checksResponse?.Items ?? new List<Check>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting checks");
            throw;
        }
    }

    /// <summary>
    /// Creates a new check result by executing a compliance check.
    /// </summary>
    public async Task<CheckResult> CreateCheckResultAsync(CreateCheckResultRequest request, string fileVersionId, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            var response = await client.PostAsJsonAsync($"/api/core/fileversion/{fileVersionId}/checkresults", request, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to create check result. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException(
                    $"Failed to create check result. Status: {response.StatusCode}, Error: {errorContent}");
            }

            var responseModel = await response.Content.ReadFromJsonAsync<CreateCheckResultResponse>(JsonOptions, cancellationToken);
            if (responseModel == null || string.IsNullOrEmpty(responseModel.Id))
                throw new InvalidOperationException("Failed to deserialize check result response or response ID is missing");

            return new CheckResult
            {
                Id = responseModel.Id,
                CheckId = request.CheckId,
                Outcome = request.Outcome,
                Description = request.Description
            };
        }
        catch (Exception ex) when (!(ex is ArgumentNullException || ex is HttpRequestException || ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Error creating check result");
            throw;
        }
    }

    /// <summary>
    /// Gets a specific check result by its ID.
    /// </summary>
    public async Task<CheckResult?> GetCheckResultAsync(string checkResultId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            var response = await client.GetAsync($"/api/core/check-results/{checkResultId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to get check result. CheckResultId: {CheckResultId}, Status: {StatusCode}, Error: {Error}",
                    checkResultId, response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CheckResult>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting check result with ID: {CheckResultId}", checkResultId);
            throw;
        }
    }

    /// <summary>
    /// Gets all check results for a specific record/asset.
    /// </summary>
    public async Task<List<CheckResult>> GetCheckResultsByRecordIdAsync(string recordId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            var response = await client.GetAsync($"/api/core/records/{recordId}/check-results", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to get check results for record. RecordId: {RecordId}, Status: {StatusCode}, Error: {Error}",
                    recordId, response.StatusCode, errorContent);
                return new List<CheckResult>();
            }

            var results = await response.Content.ReadFromJsonAsync<List<CheckResult>>(JsonOptions, cancellationToken);
            return results ?? new List<CheckResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting check results for record ID: {RecordId}", recordId);
            throw;
        }
    }


    /// <summary>
    /// Gets the master file latest version ID for a specific record.
    /// </summary>
    public async Task<string?> GetMasterFileLatestVersionIdAsync(string recordId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            client.DefaultRequestHeaders.Add("select-record", "masterfilelatestversion");
            var response = await client.GetAsync($"/api/core/record/{recordId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to get master file latest version. RecordId: {RecordId}, Status: {StatusCode}, Error: {Error}",
                    recordId, response.StatusCode, errorContent);
                return null;
            }

            var record = await response.Content.ReadFromJsonAsync<Record>(JsonOptions, cancellationToken);
            return record?.MasterFileLatestVersion?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting master file latest version for record ID: {RecordId}", recordId);
            throw;
        }
    }

    /// <summary>
    /// Gets all check results for a specific file version.
    /// </summary>
    public async Task<List<CheckResult>> GetAllCheckResultsAsync(string fileVersionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            var response = await client.GetAsync($"/api/core/fileversion/{fileVersionId}/checkresults", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to get check results for file version. FileVersionId: {FileVersionId}, Status: {StatusCode}, Error: {Error}",
                    fileVersionId, response.StatusCode, errorContent);
                return new List<CheckResult>();
            }

            var checkResultsResponse = await response.Content.ReadFromJsonAsync<CheckResultsResponse>(JsonOptions, cancellationToken);
            return checkResultsResponse?.Items ?? new List<CheckResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting check results for file version ID: {FileVersionId}", fileVersionId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a specific check result.
    /// </summary>
    public async Task<bool> DeleteCheckResultAsync(string fileVersionId, string checkResultId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            var response = await client.DeleteAsync($"/api/core/fileversion/{fileVersionId}/checkresult/{checkResultId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to delete check result. FileVersionId: {FileVersionId}, CheckResultId: {CheckResultId}, Status: {StatusCode}, Error: {Error}",
                    fileVersionId, checkResultId, response.StatusCode, errorContent);
                return false;
            }

            _logger.LogInformation("Successfully deleted check result. FileVersionId: {FileVersionId}, CheckResultId: {CheckResultId}",
                fileVersionId, checkResultId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting check result. FileVersionId: {FileVersionId}, CheckResultId: {CheckResultId}", 
                fileVersionId, checkResultId);
            throw;
        }
    }

    /// <summary>
    /// Creates a download order for a record and returns the download URL.
    /// </summary>
    public async Task<string?> GetAprimoOrderAsync(string recordId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            
            var orderRequest = new CreateOrderRequest
            {
                Type = "download",
                DisableNotification = true,
                Targets = new List<OrderTarget>
                {
                    new OrderTarget
                    {
                        RecordId = recordId,
                        TargetTypes = new List<string> { "Document" },
                        AssetType = "LatestVersionOfMasterFile"
                    }
                }
            };

            var response = await client.PostAsJsonAsync("/api/core/orders", orderRequest, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to create order. RecordId: {RecordId}, Status: {StatusCode}, Error: {Error}",
                    recordId, response.StatusCode, errorContent);
                return null;
            }

            var orderResponse = await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions, cancellationToken);
            
            if (orderResponse?.DeliveredFiles == null || !orderResponse.DeliveredFiles.Any())
            {
                _logger.LogWarning("Order created but no delivered files URL found. RecordId: {RecordId}, OrderId: {OrderId}",
                    recordId, orderResponse?.Id);
                return null;
            }

            var downloadUrl = orderResponse.DeliveredFiles.First();
            _logger.LogInformation("Successfully created order and retrieved download URL. RecordId: {RecordId}, OrderId: {OrderId}",
                recordId, orderResponse.Id);
            
            return downloadUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for record ID: {RecordId}", recordId);
            throw;
        }
    }

    /// <summary>
    /// Response model for token authentication.
    /// Matches Aprimo's actual authentication response structure.
    /// </summary>
    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}

