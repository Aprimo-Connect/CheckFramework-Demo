# Ngrok Setup Instructions

## Prerequisites
1. Install ngrok from https://ngrok.com/download
2. Sign up for a free ngrok account at https://ngrok.com
3. Get your authtoken from https://dashboard.ngrok.com/get-started/your-authtoken

## Setup Steps

### 1. Configure ngrok authtoken
```bash
ngrok config add-authtoken YOUR_AUTHTOKEN_HERE
```

### 2. Run the C# Web Application
```bash
dotnet run
```

The application will start on `http://localhost:5000`

### 3. In a separate terminal, start ngrok
```bash
ngrok http 5000
```

Or if you want to use a specific domain (requires paid plan):
```bash
ngrok http 5000 --domain=your-domain.ngrok.io
```

### 4. Test the endpoint
Use the ngrok URL (e.g., `https://abc123.ngrok.io`) to make requests:

**Health Check:**
```bash
curl https://your-ngrok-url.ngrok.io/api/compliance/health
```

**Compliance Check Request (with ID):**
```bash
curl -X POST https://your-ngrok-url.ngrok.io/api/compliance/compliance \
  -H "Content-Type: application/json" \
  -d '{"id":"test-123"}'
```

## Alternative: Using ngrok with a config file

You can create an `ngrok.yml` config file in your project root:

```yaml
version: "2"
authtoken: YOUR_AUTHTOKEN_HERE
tunnels:
  webapp:
    addr: 5000
    proto: http
```

Then run:
```bash
ngrok start webapp
```

