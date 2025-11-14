using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Aprimo.CheckFramework.Demo.Services;
using Aprimo.CheckFramework.Demo.Models;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using System.Drawing;

namespace Aprimo.CheckFramework.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComplianceController : ControllerBase
{
    private readonly ILogger<ComplianceController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAprimoApiService _aprimoApiService;

    public ComplianceController(
        ILogger<ComplianceController> logger,
        IConfiguration configuration,
        IAprimoApiService aprimoApiService)
    {
        _logger = logger;
        _configuration = configuration;
        _aprimoApiService = aprimoApiService;
    }

    /// <summary>
    /// Handles compliance check requests from Aprimo webhooks.
    /// </summary>
    [HttpPost("compliance")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Compliance([FromForm] ComplianceRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request is required" });
        }

        if (string.IsNullOrEmpty(request.RecordIds))
        {
            return BadRequest(new { error = "recordIds is required" });
        }

        var recordId = request.RecordIds.Trim();
        var authCode = request.AuthCode;

        _logger.LogInformation("Received compliance check request. RecordId: {RecordId}, AuthCode: {AuthCode}", 
            recordId, !string.IsNullOrEmpty(authCode) ? "***" : "not provided");

        try
        {
            var qrCodeCheckId = _configuration["Aprimo:AprimoCheckQRCodeId"];
            var barCodeCheckId = _configuration["Aprimo:AprimoCheckBarCodeId"];
            var colorComplianceCheckId = _configuration["Aprimo:AprimoCheckColorComplianceId"];

            var checkResults = new List<CheckResult>();
        
            var masterfilelatestversionId = await _aprimoApiService.GetMasterFileLatestVersionIdAsync(recordId);

            if (string.IsNullOrEmpty(masterfilelatestversionId))
            {
                _logger.LogWarning("Could not retrieve master file latest version ID for record: {RecordId}", recordId);
                return BadRequest(new { error = $"Could not retrieve master file latest version for record: {recordId}" });
            }

            _logger.LogInformation("Retrieved master file latest version ID: {FileVersionId} for record: {RecordId}", 
                masterfilelatestversionId, recordId);

            var downloadUrl = await _aprimoApiService.GetAprimoOrderAsync(recordId);

            byte[]? imageBytes = null;
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                try
                {
                    using var httpClient = new HttpClient();
                    imageBytes = await httpClient.GetByteArrayAsync(downloadUrl);
                    _logger.LogInformation("Successfully downloaded image for compliance checks. Size: {Size} bytes", imageBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading image for compliance checks. RecordId: {RecordId}", recordId);
                }
            }
            else
            {
                _logger.LogWarning("Download URL is null or empty. Cannot perform image-based compliance checks for record: {RecordId}", recordId);
            }

            if (!string.IsNullOrEmpty(qrCodeCheckId))
            {
                _logger.LogInformation("Running QR Code compliance check for record: {RecordId}", recordId);
                
                string outcome = "fail";
                string description = "QR Code compliance check failed";
                
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    description = "QR Code compliance check failed: Could not retrieve image";
                }
                else
                {
                    try
                    {
                        using var ms = new MemoryStream(imageBytes);
                        using var bitmap = new Bitmap(ms);
                        var source = new BitmapLuminanceSource(bitmap);
                        
                        var reader = new BarcodeReaderGeneric
                        {
                            Options = new DecodingOptions
                            {
                                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                                TryHarder = true
                            }
                        };
                        
                        var result = reader.Decode(source);
                        
                        if (result != null && !string.IsNullOrEmpty(result.Text))
                        {
                            var qrCodeText = result.Text;
                            Console.WriteLine($"QR Code detected. Content: {qrCodeText}");
                            
                            if (Uri.TryCreate(qrCodeText, UriKind.Absolute, out var uri) &&
                                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                            {
                                outcome = "pass";
                                description = $"Qr Code points to a valid URL, linked [here]({qrCodeText})";
                                Console.WriteLine($"QR Code points to a valid URL: {qrCodeText}");
                            }
                            else
                            {
                                Console.WriteLine($"QR Code content is not a valid URL: {qrCodeText}");
                                description = $"QR Code detected but content is not a valid URL: {qrCodeText}";
                            }
                        }
                        else
                        {
                            Console.WriteLine("No QR Code found in the image");
                            description = "QR Code compliance check failed: No QR Code found in the image";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing QR Code check for record: {RecordId}", recordId);
                        Console.WriteLine($"Error processing QR Code: {ex.Message}");
                        description = $"QR Code compliance check failed: {ex.Message}";
                    }
                }
                
                var checkResult = await _aprimoApiService.CreateCheckResultAsync(
                    new CreateCheckResultRequest
                    {
                        CheckId = qrCodeCheckId,
                        Outcome = outcome,
                        Description = description
                    },
                    fileVersionId: masterfilelatestversionId
                );
                checkResults.Add(checkResult);
            }

            if (!string.IsNullOrEmpty(barCodeCheckId))
            {
                _logger.LogInformation("Running Bar Code compliance check for record: {RecordId}", recordId);
                
                string outcome = "fail";
                string description = "Bar Code compliance check failed";
                
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    description = "Bar Code compliance check failed: Could not retrieve image";
                }
                else
                {
                    try
                    {
                        using var ms = new MemoryStream(imageBytes);
                        using var bitmap = new Bitmap(ms);
                        var source = new BitmapLuminanceSource(bitmap);
                        
                        var reader = new BarcodeReaderGeneric
                        {
                            Options = new DecodingOptions
                            {
                                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.CODE_128 },
                                TryHarder = true
                            }
                        };
                        
                        var result = reader.Decode(source);
                        
                        if (result != null && !string.IsNullOrEmpty(result.Text))
                        {
                            var barCodeText = result.Text;
                            Console.WriteLine($"Bar Code detected. Content: {barCodeText}");
                            
                            bool isValidAscii = barCodeText.All(c => c >= 0 && c <= 127);
                            
                            if (isValidAscii)
                            {
                                outcome = "pass";
                                description = $"Bar Code is valid and contains {barCodeText}";
                                Console.WriteLine($"Bar Code is valid: {barCodeText}");
                            }
                            else
                            {
                                Console.WriteLine($"Bar Code content contains invalid ASCII characters: {barCodeText}");
                                description = $"Bar Code detected but contains invalid ASCII characters: {barCodeText}";
                            }
                        }
                        else
                        {
                            Console.WriteLine("No Bar Code was found in the document");
                            description = "No Bar Code was found in the document";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Bar Code check for record: {RecordId}", recordId);
                        Console.WriteLine($"Error processing Bar Code: {ex.Message}");
                        description = $"Bar Code compliance check failed: {ex.Message}";
                    }
                }
                
                var checkResult = await _aprimoApiService.CreateCheckResultAsync(
                    new CreateCheckResultRequest
                    {
                        CheckId = barCodeCheckId,
                        Outcome = outcome,
                        Description = description
                    },
                    fileVersionId: masterfilelatestversionId
                );
                checkResults.Add(checkResult);
            }

            if (!string.IsNullOrEmpty(colorComplianceCheckId))
            {
                _logger.LogInformation("Running Color Compliance check for record: {RecordId}", recordId);

                var checkResult = await _aprimoApiService.CreateCheckResultAsync(
                     new CreateCheckResultRequest
                     {
                         CheckId = colorComplianceCheckId,
                         Outcome = "info", // or "fail" based on actual check
                         Description = "Color Code compliance check completed"
                     },
                     fileVersionId: masterfilelatestversionId
                 );
                 checkResults.Add(checkResult);
            }

            var outcomes = checkResults.Select(cr => cr.Outcome ?? "unknown").ToList();
            var outcomesText = outcomes.Any() ? string.Join(", ", outcomes) : "none";
            return Ok(new { 
                msg = $"Compliance checks completed. Outcomes: {outcomesText}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing compliance check for record: {RecordId}", recordId);
            return StatusCode(500, new { error = "An error occurred while processing the compliance check" });
        }
    }

    /// <summary>
    /// Deletes all check results for a given record.
    /// </summary>
    [HttpDelete("deleteall")]
    public async Task<IActionResult> DeleteAll([FromQuery] string recordId)
    {
        if (string.IsNullOrEmpty(recordId))
        {
            return BadRequest(new { error = "recordId is required" });
        }

        recordId = recordId.Trim();
        _logger.LogInformation("Received delete all check results request for record: {RecordId}", recordId);

        try
        {
            var masterfilelatestversionId = await _aprimoApiService.GetMasterFileLatestVersionIdAsync(recordId);

            if (string.IsNullOrEmpty(masterfilelatestversionId))
            {
                _logger.LogWarning("Could not retrieve master file latest version ID for record: {RecordId}", recordId);
                return BadRequest(new { error = $"Could not retrieve master file latest version for record: {recordId}" });
            }

            _logger.LogInformation("Retrieved master file latest version ID: {FileVersionId} for record: {RecordId}", 
                masterfilelatestversionId, recordId);

            var checkResults = await _aprimoApiService.GetAllCheckResultsAsync(masterfilelatestversionId);

            if (checkResults == null || !checkResults.Any())
            {
                _logger.LogInformation("No check results found for file version: {FileVersionId}", masterfilelatestversionId);
                return Ok(new { msg = "No check results found to delete" });
            }

            _logger.LogInformation("Found {Count} check result(s) to delete for file version: {FileVersionId}", 
                checkResults.Count, masterfilelatestversionId);

            var deletedCount = 0;
            var failedCount = 0;

            foreach (var checkResult in checkResults)
            {
                if (string.IsNullOrEmpty(checkResult.Id))
                {
                    _logger.LogWarning("Skipping check result with null or empty ID");
                    failedCount++;
                    continue;
                }

                var deleted = await _aprimoApiService.DeleteCheckResultAsync(masterfilelatestversionId, checkResult.Id);
                if (deleted)
                {
                    deletedCount++;
                    _logger.LogInformation("Successfully deleted check result: {CheckResultId}", checkResult.Id);
                }
                else
                {
                    failedCount++;
                    _logger.LogWarning("Failed to delete check result: {CheckResultId}", checkResult.Id);
                }
            }

            return Ok(new { 
                msg = $"Delete all completed. Deleted: {deletedCount}, Failed: {failedCount}, Total: {checkResults.Count}" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all check results for record: {RecordId}", recordId);
            return StatusCode(500, new { error = "An error occurred while deleting check results" });
        }
    }

    /// <summary>
    /// Health check endpoint for monitoring.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

public class ComplianceRequest
{
    /// <summary>
    /// The source URL (URL-encoded) pointing to the content item in Aprimo.
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// The send token type (typically "AuthCode").
    /// </summary>
    public string? SendToken { get; set; }

    /// <summary>
    /// The location parameter (e.g., "new").
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// The authentication code provided by Aprimo.
    /// </summary>
    [Microsoft.AspNetCore.Mvc.BindProperty(Name = "auth-code")]
    public string? AuthCode { get; set; }

    /// <summary>
    /// The record IDs (comma-separated list of record identifiers).
    /// This is the primary identifier we extract as recordId.
    /// </summary>
    public string? RecordIds { get; set; }
}

