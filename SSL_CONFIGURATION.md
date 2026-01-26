# SSL/HTTPS Configuration Guide

## Overview

This WebRTC application uses self-signed SSL certificates for HTTPS communication. The certificates are stored in the `SSL` folder and are automatically loaded by the ASP.NET Core applications.

## Certificate Files

Located in: `./SSL/`

```
SSL/
??? 192.168.68.236.cert.pem    # Self-signed certificate
??? 192.168.68.236.key.pem     # Private key
??? key.pem                     # Backup key file
```

## Current Configuration

### WebRtcBackend (Port 5000, 5079)
- **HTTPS Port**: 5000, 5079
- **Certificate**: Loaded from `SSL/192.168.68.236.cert.pem`
- **Private Key**: Loaded from `SSL/192.168.68.236.key.pem`
- **CORS Origins**: 
  - `https://localhost:5000`
  - `https://192.168.68.236:5000`
  - `https://127.0.0.1:5000`

### WebClient (Port 5000)
- **HTTPS Port**: 5000
- **Certificate**: Loaded from `SSL/192.168.68.236.cert.pem`
- **Backend URL**: `https://192.168.68.236:5000` (configurable via `BACKEND_SERVER_URL` environment variable)

### ConsoleClient
- **Connection URL**: `https://192.168.68.236:5000` (configurable via `BACKEND_SERVER_URL` environment variable)
- **SSL Validation**: Disabled for development (accepts self-signed certificates)
- **Certificate Verification**: Logs warnings but continues connection

## Running the Applications

### Option 1: Direct Execution

```bash
# Terminal 1 - WebRTC Backend
cd WebRtcBackend
dotnet run --configuration Debug --project WebRtcBackend.csproj

# Terminal 2 - Web Client
cd WebClient
dotnet run --configuration Debug --project WebClient.csproj

# Terminal 3 - Console Client
cd ConsoleClient
dotnet run --configuration Debug --project ConsoleClient.csproj
```

### Option 2: With Environment Variables

```bash
# Set backend server URL for console client
$env:BACKEND_SERVER_URL = "https://192.168.68.236:5000"
dotnet run --project ConsoleClient/ConsoleClient.csproj

# Or for web client
$env:BACKEND_SERVER_URL = "https://192.168.68.236:5000"
dotnet run --project WebClient/WebClient.csproj
```

## Accessing the Applications

### Web Client
```
https://192.168.68.236:5000
https://localhost:5000
```

### Console Client
```
dotnet run --project ConsoleClient/ConsoleClient.csproj
```

## Troubleshooting SSL Issues

### Issue: "SSL connection could not be established"

**Solution 1: Verify Certificate Files**
```powershell
Get-Item -Path ".\SSL\*.pem" | Select-Object Name, Length
```

Expected output:
```
Name                      Length
----                      ------
192.168.68.236.cert.pem   1732
192.168.68.236.key.pem    1732
key.pem                   1732
```

**Solution 2: Check Certificate Validity**
```powershell
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$cert.Import(".\SSL\192.168.68.236.cert.pem")
$cert.Subject
$cert.NotBefore
$cert.NotAfter
```

**Solution 3: Regenerate Certificates**

If certificates are corrupted, you can regenerate them using OpenSSL:

```bash
# Install OpenSSL if needed
# For Windows: https://slproweb.com/products/Win32OpenSSL.html

# Generate new self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout SSL/192.168.68.236.key.pem -out SSL/192.168.68.236.cert.pem -days 365 -nodes -subj "/CN=192.168.68.236"
```

### Issue: "ConsoleClient can't connect to backend"

**Check these steps:**

1. **Verify Backend is Running**
   ```powershell
   netstat -ano | findstr :5000
   ```

2. **Check Firewall Rules**
   - Allow HTTPS (port 5000) in Windows Defender
   - Or disable firewall for local testing

3. **Try with localhost first**
   ```csharp
   // In ConsoleClient Program.cs, change:
   var backendServerUrl = "https://localhost:5000";
   ```

4. **Check Network Configuration**
   - Verify `192.168.68.236` is your machine's IP
   - Use `ipconfig` to find your actual IP address

### Issue: "Certificate validation failed"

This is expected with self-signed certificates. The ConsoleClient automatically accepts them for development.

To disable SSL validation warning:
```csharp
// Already configured in ConferenceConnection.cs
options.HttpMessageHandlerFactory = (message) =>
{
    if (message is HttpClientHandler clientHandler)
    {
        clientHandler.ServerCertificateCustomValidationCallback += 
            (sender, cert, chain, sslPolicyErrors) => true;
    }
    return message;
};
```

## Production Deployment

For production, you should:

1. **Use Real Certificates**
   - Obtain from a trusted Certificate Authority (CA)
   - DigiCert, Let's Encrypt, etc.

2. **Update Configuration**
   ```csharp
   // Load from secure key store
   var cert = X509Certificate2.CreateFromPemFile(
       "/secure/path/to/cert.pem",
       "/secure/path/to/key.pem");
   ```

3. **Enable Certificate Pinning**
   - Add certificate pinning for additional security

4. **Enforce SSL Validation**
   ```csharp
   // Remove custom validation callback
   options.HttpMessageHandlerFactory = null;
   ```

## Useful Commands

### View Certificate Details
```bash
openssl x509 -in SSL/192.168.68.236.cert.pem -text -noout
```

### Test HTTPS Connection
```bash
curl -k https://192.168.68.236:5000/
curl -k https://localhost:5000/
```

### Check Port Usage
```powershell
netstat -ano | findstr :5000
netstat -ano | findstr :5079
```

### Test Backend Connectivity
```powershell
$uri = "https://192.168.68.236:5000/"
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.ServerCertificateCustomValidationCallback = { $true }
$client = New-Object System.Net.Http.HttpClient -ArgumentList $handler
$client.GetAsync($uri) | Select-Object Result
```

## Environment Variables

Set these before running the applications:

```powershell
# Backend Server URL (for Web and Console clients)
$env:BACKEND_SERVER_URL = "https://192.168.68.236:5000"

# ASP.NET Environment
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Set ports (optional)
$env:ASPNETCORE_URLS = "https://0.0.0.0:5000"
```

## Certificate Renewal

Self-signed certificates don't expire unless specified. Current certificate details:

```
Subject: CN=192.168.68.236
Valid From: Jan 26, 2026
Valid Until: Jan 26, 2027 (365 days)
```

To renew before expiration:

```bash
openssl req -x509 -newkey rsa:4096 -keyout SSL/192.168.68.236.key.pem -out SSL/192.168.68.236.cert.pem -days 365 -nodes -subj "/CN=192.168.68.236"
```

## Additional Resources

- [ASP.NET Core HTTPS](https://docs.microsoft.com/en-us/aspnet/core/security/https)
- [Kestrel HTTPS Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
- [OpenSSL Documentation](https://www.openssl.org/docs/)
- [Self-Signed Certificates](https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide)
