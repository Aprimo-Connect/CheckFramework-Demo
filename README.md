### Aprimo's Open Source Policy 
This code is provided by Aprimo _as-is_ as an example of how you might solve a specific business problem. It is not intended for direct use in Production without modification.

You are welcome to submit issues or feedback to help us improve visibility into potential bugs or enhancements. Aprimo may, at its discretion, address minor bugs, but does not guarantee fixes, vulnerability remediation or ongoing support.

It is expected that developers who clone or use this code take full responsibility for supporting, maintaining, securing, and vulnerability management of any deployments derived from it.

If you are interested in a production-ready and supported version of this solution, please contact your Aprimo account representative. They can connect you with our technical services team or a partner who may be able to build and support a packaged implementation for you.

Please note: This code may include references to non-Aprimo services or APIs. You are responsible for acquiring any required credentials or API keys to use those services—Aprimo does not provide them.


# Aprimo Check Framework Demo

A demonstration ASP.NET Core web application showcasing the [Aprimo Check Framework](https://developers.aprimo.com/docs/rest-api/dam/check-framework). This application implements automated compliance checks that integrate with Aprimo DAM via webhooks and the REST API.

## Overview

The Aprimo Check Framework allows you to create custom compliance checks that validate assets in your Aprimo DAM system. This demo application shows how to:

- **Create Check Results** - Programmatically create check results via the Aprimo REST API
- **Execute Compliance Checks** - Perform automated validation on assets (QR codes, barcodes, color compliance)
- **Integrate with Aprimo Webhooks** - Receive compliance check requests from Aprimo DAM via webhooks/pagehooks
- **Manage Check Results** - Retrieve, query, and delete check results for file versions

## What is the Check Framework?

The Check Framework is an Aprimo DAM API feature that enables you to:
- Define custom checks that validate assets against business rules
- Create check results that record the outcome of these validations
- Track compliance status for assets and file versions
- Integrate external validation services with Aprimo DAM

For detailed documentation, see: [Aprimo Check Framework Documentation](https://developers.aprimo.com/docs/rest-api/dam/check-framework)

## Features

This demo mock implements three types of compliance checks. In a real integration you will need to identify what compliance checks are needed for your business case and how to properly perform them.

### 1. QR Code Check
- Downloads the asset image from Aprimo
- Uses ZXing.NET to detect and read QR codes
- Validates that the QR code content is a valid URL
- Creates a check result with `pass` or `fail` outcome

### 2. Bar Code Check
- Downloads the asset image from Aprimo
- Uses ZXing.NET to detect and read Code128 barcodes
- Validates that the barcode content contains only valid ASCII characters
- Creates a check result with `pass` or `fail` outcome

### 3. Color Compliance Check
- Placeholder for color compliance validation
- Demonstrates the structure for implementing brand color validation

## Architecture

### Components

- **`ComplianceController`** - Handles incoming webhook requests from Aprimo and orchestrates compliance checks
- **`AprimoApiService`** - Service layer for interacting with the Aprimo REST API
  - OAuth2 authentication with token caching
  - Check and CheckResult CRUD operations
  - File version and order management
- **`Models/`** - Data models for Checks, CheckResults, Records, and Orders
- **`Services/`** - Business logic and API integration

### API Integration Flow

1. **Webhook Trigger**: Aprimo sends a webhook request to `/api/compliance/compliance` with a `recordId`
2. **File Version Lookup**: Application retrieves the master file latest version ID for the record
3. **Asset Download**: Creates an Aprimo download order to get the asset file
4. **Compliance Validation**: Performs checks (QR code, barcode, color) on the downloaded asset
5. **Check Result Creation**: Creates check results in Aprimo via REST API for each check performed

## Prerequisites

- .NET 8.0 SDK or later
- Aprimo DAM tenant with API access
- Aprimo API credentials (Client ID and Client Secret)
- ngrok (for local development and webhook testing)
- Checks configured in Aprimo (QR Code, Bar Code, Color Compliance)

## Configuration

### 1. Application Settings

Create or update `appsettings.json`:

```json
{
  "Aprimo": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "AprimoCheckQRCodeId": "your-qr-code-check-id",
    "AprimoCheckBarCodeId": "your-barcode-check-id",
    "AprimoCheckColorComplianceId": "your-color-compliance-check-id"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Port": "5000"
}
```

**Note**: `appsettings.json` is in `.gitignore` to protect sensitive credentials. Use environment variables or secure configuration management in production.

### 2. Aprimo Check Setup

Before running the application, you need to create Checks in Aprimo:

1. Log into your Aprimo DAM tenant
2. Navigate to the Checks configuration
3. Create three checks:
   - QR Code Check (note the Check ID)
   - Bar Code Check (note the Check ID)
   - Color Compliance Check (note the Check ID)
4. Add these Check IDs to your `appsettings.json`

See the [Check Framework documentation](https://developers.aprimo.com/docs/rest-api/dam/check-framework) for details on creating checks.

### 3. ngrok Setup

For local development, use ngrok to expose your local server to Aprimo webhooks:

1. Install ngrok from https://ngrok.com/download
2. Configure your authtoken: `ngrok config add-authtoken YOUR_TOKEN`
3. Start ngrok: `ngrok http 5000`

See [ngrok-setup.md](ngrok-setup.md) for detailed instructions.

4. Configure the ngrok URL in your Aprimo webhook/pagehook settings

## Getting Started

### 1. Install Dependencies

```bash
dotnet restore
```

### 2. Build the Application

```bash
dotnet build
```

### 3. Run the Application

```bash
dotnet run
```

The application will start on `http://localhost:5000` by default.

### 4. Access Swagger UI (Development)

When running in development mode, Swagger UI is available at:
```
http://localhost:5000/swagger
```

## API Endpoints

### Health Check
```
GET /api/compliance/health
```
Returns the health status of the application.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

### Compliance Check (Webhook Endpoint)
```
POST /api/compliance/compliance
Content-Type: application/x-www-form-urlencoded
```
This endpoint receives webhook requests from Aprimo when a compliance check is triggered.

**Request Parameters:**
- `recordIds` (required) - The Aprimo record ID to check
- `auth-code` (optional) - Authentication code from Aprimo

**Response:**
```json
{
  "msg": "Compliance checks completed. Outcomes: pass, pass, info"
}
```

### Delete All Check Results
```
DELETE /api/compliance/deleteall?recordId={recordId}
```
Deletes all check results for a given record's master file latest version.

**Query Parameters:**
- `recordId` (required) - The Aprimo record ID

**Response:**
```json
{
  "msg": "Delete all completed. Deleted: 3, Failed: 0, Total: 3"
}
```

## Check Framework API Methods

The `AprimoApiService` implements the following Check Framework operations:

### Check Operations
- `GetCheckAsync(checkId)` - Retrieve a specific check
- `GetChecksAsync()` - Retrieve all checks

### Check Result Operations
- `CreateCheckResultAsync(request, fileVersionId)` - Create a new check result
- `GetCheckResultAsync(checkResultId)` - Retrieve a specific check result
- `GetAllCheckResultsAsync(fileVersionId)` - Get all check results for a file version
- `GetCheckResultsByRecordIdAsync(recordId)` - Get all check results for a record
- `DeleteCheckResultAsync(fileVersionId, checkResultId)` - Delete a check result

### Supporting Operations
- `GetMasterFileLatestVersionIdAsync(recordId)` - Get the file version ID for a record
- `GetAprimoOrderAsync(recordId)` - Create a download order and get the file URL

## Example Usage

### Testing with cURL

**Health Check:**
```bash
curl https://your-ngrok-url.ngrok.io/api/compliance/health
```

**Trigger Compliance Check (simulating Aprimo webhook):**
```bash
curl -X POST https://your-ngrok-url.ngrok.io/api/compliance/compliance \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "recordIds=your-record-id&auth-code=optional-auth-code"
```

**Delete All Check Results:**
```bash
curl -X DELETE "https://your-ngrok-url.ngrok.io/api/compliance/deleteall?recordId=your-record-id"
```

## Project Structure

```
├── Controllers/
│   └── ComplianceController.cs    # Webhook endpoint and compliance check orchestration
├── Services/
│   ├── IAprimoApiService.cs        # Interface for Aprimo API operations
│   └── AprimoApiService.cs        # Implementation of Aprimo REST API client
├── Models/
│   ├── Check.cs                   # Check model
│   ├── CheckResult.cs             # CheckResult model
│   ├── Record.cs                  # Record model
│   └── Order.cs                   # Order model
├── Program.cs                     # Application entry point and DI configuration
├── appsettings.json               # Application configuration (gitignored)
├── ngrok-setup.md                 # ngrok setup instructions
└── README.md                      # This file
```

## Dependencies

- **ZXing.NET** - Barcode and QR code reading
- **System.Drawing.Common** - Image processing
- **ASP.NET Core** - Web framework
- **System.Text.Json** - JSON serialization

## Development

### Running in Development Mode

```bash
dotnet run --environment Development
```

This enables:
- Swagger UI at `/swagger`
- Detailed error pages
- Hot reload (with `dotnet watch`)

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## Troubleshooting

### Common Issues

1. **Authentication Failures**
   - Verify your `ClientId` and `ClientSecret` in `appsettings.json`
   - Check that your tenant ID is correct
   - Ensure your API credentials have the necessary permissions

2. **Check Not Found**
   - Verify the Check IDs in `appsettings.json` match your Aprimo checks
   - Ensure the checks exist in your Aprimo tenant

3. **Webhook Not Receiving Requests**
   - Verify ngrok is running and accessible
   - Check that the ngrok URL is correctly configured in Aprimo
   - Ensure the webhook endpoint is set to `/api/compliance/compliance`

4. **Image Download Failures**
   - Verify the record has a master file
   - Check that the download order API is working
   - Ensure network connectivity to Aprimo

## Resources

- [Aprimo Check Framework Documentation](https://developers.aprimo.com/docs/rest-api/dam/check-framework)
- [Aprimo Developers Portal](https://developers.aprimo.com/)
- [Aprimo REST API Reference](https://developers.aprimo.com/docs/rest-api/dam)


