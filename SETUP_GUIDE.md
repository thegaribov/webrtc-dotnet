# WebRTC Application Setup Guide

## Prerequisites

- .NET 10 SDK installed
- Webcam connected (for console client video streaming)
- Visual Studio 2022 or VS Code (recommended)
- Administrator access to Windows (for HTTPS)

## Quick Start

### 1. Clone Repository

```bash
git clone https://github.com/thegaribov/webrtc-dotnet
cd webrtc-dotnet
```

### 2. Verify SSL Certificates

Check that certificates are in place:

```powershell
ls .\SSL\
```

Expected files:
- `192.168.68.236.cert.pem`
- `192.168.68.236.key.pem`

### 3. Start WebRTC Backend

```bash
cd WebRtcBackend
dotnet run
```

Expected output:
```
? SSL certificates loaded successfully
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://[::]:5000
      Now listening on: https://127.0.0.1:5000
```

### 4. Start Web Client (Optional)

In a new terminal:

```bash
cd WebClient
dotnet run
```

Access at: `https://192.168.68.236:5000`

### 5. Start Console Client

In a new terminal:

```bash
cd ConsoleClient
dotnet run
```

Follow the prompts:
```
Enter your name: Alice
Enter room ID: conference-room-1
```

## Application Architecture

```
????????????????????????????????????????????????????????????????
?                    WebRTC Application                        ?
????????????????????????????????????????????????????????????????
?                                                              ?
?  ???????????????????????         ????????????????????????  ?
?  ?   Web Client        ?         ?   Console Client     ?  ?
?  ?  (Browser/SPA)      ?         ?  (CLI App)           ?  ?
?  ?  - WebSocket via    ?         ?  - Camera Capture    ?  ?
?  ?    SignalR          ?         ?  - Video Streaming   ?  ?
?  ?  - HTML/JS/CSS      ?         ?  - Frame Encoding    ?  ?
?  ???????????????????????         ????????????????????????  ?
?           ?                                 ?                ?
?           ?         HTTPS/SignalR           ?                ?
?           ???????????????????????????????????                ?
?                        ?                                     ?
?                  ?????????????                              ?
?                  ?  Backend   ?                              ?
?                  ?   Hub      ?                              ?
?                  ? (Port 5000)?                              ?
?                  ??????????????                              ?
?                                                              ?
????????????????????????????????????????????????????????????????
```

## Features

### ? Complete in This Release

- [x] WebRTC Backend with SignalR Hub
- [x] Web Client SPA
- [x] Console Client with CLI
- [x] Real-time Camera Capture (30fps)
- [x] Video Frame Encoding (JPEG)
- [x] Video Streaming to Multiple Peers
- [x] WebRTC Signaling (Offer/Answer/ICE)
- [x] HTTPS/SSL Support with Self-Signed Certificates
- [x] Room-Based Conferencing

### ?? Future Enhancements

- [ ] Audio Streaming
- [ ] Screen Sharing
- [ ] Video Recording
- [ ] User Authentication
- [ ] Persistent Storage
- [ ] Mobile App Support
- [ ] Advanced Codec Support

## Usage Examples

### Starting a Conference

**Terminal 1 - Backend:**
```bash
cd WebRtcBackend
dotnet run
```

**Terminal 2 - User 1 (Console Client):**
```bash
cd ConsoleClient
dotnet run
# Enter name: Alice
# Enter room: conference1
```

**Terminal 3 - User 2 (Console Client):**
```bash
cd ConsoleClient
dotnet run
# Enter name: Bob
# Enter room: conference1
```

### Console Client Commands

```
> list
?? Users in Room:
  [0] Bob
      ID: abc123...

> stream 0 85
? Video stream started to abc123... (quality: 85%)

> streams
?? Active Video Streams:
  • abc123... - 150 frames sent

> stop-stream 0
? Video stream stopped for abc123...

> offer 0
?? Offer sent to abc123...

> answer 0
?? Answer sent to abc123...

> exit
Goodbye!
```

## Troubleshooting

### Problem: Cannot connect to backend

**Check:**
1. Backend is running on the correct port (5000)
2. Firewall allows HTTPS (port 5000)
3. IP address is correct (use `ipconfig` to verify)

**Solution:**
```bash
# Test backend connectivity
curl -k https://192.168.68.236:5000/
curl -k https://localhost:5000/
```

### Problem: Camera not detected

**Check:**
1. Webcam is connected and working
2. No other application is using the camera
3. Camera drivers are up to date

**Command:**
```powershell
# In console client
> camera-info
?? Camera Info:
  Status: ? Stopped
```

### Problem: SSL certificate errors

**Solution:**
- Certificates are already configured for development
- Console Client automatically trusts self-signed certs
- See `SSL_CONFIGURATION.md` for detailed SSL setup

### Problem: Video frames not showing

**Check:**
1. Camera is capturing frames: `camera-info`
2. Stream is active: `streams`
3. Peer is connected: `list`

## Project Structure

```
webrtc-dotnet/
??? WebRtcBackend/              # SignalR Hub Server
?   ??? Hubs/
?   ?   ??? ConferenceHub.cs   # WebRTC signaling logic
?   ??? Program.cs             # Kestrel + HTTPS config
?   ??? ...
??? WebClient/                  # Web-based Client
?   ??? wwwroot/               # Static files
?   ?   ??? index.html
?   ??? Services/
?   ?   ??? WebClientConnection.cs
?   ??? Program.cs             # SPA server
?   ??? ...
??? ConsoleClient/              # Console-based Client
?   ??? Services/
?   ?   ??? ConferenceConnection.cs
?   ?   ??? CameraCapture.cs
?   ?   ??? VideoStreamEncoder.cs
?   ??? Program.cs             # CLI app
?   ??? ...
??? SSL/                        # SSL Certificates
?   ??? 192.168.68.236.cert.pem
?   ??? 192.168.68.236.key.pem
?   ??? key.pem
??? SSL_CONFIGURATION.md        # SSL setup guide
??? CONSOLE_CLIENT_STREAMING.md # Console client guide
```

## Development Tips

### Use Environment Variables

```powershell
# PowerShell
$env:BACKEND_SERVER_URL = "https://localhost:5000"
dotnet run --project ConsoleClient

# Bash
export BACKEND_SERVER_URL="https://localhost:5000"
dotnet run --project ConsoleClient
```

### Enable Debug Logging

```csharp
// In Program.cs
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

### Hot Reload

```bash
dotnet watch run --project ConsoleClient
```

## Performance Tuning

### Video Stream Quality

Adjust quality per-stream:

```
> stream 0 50   # Low quality for slow networks
> stream 1 85   # High quality for fast networks
> stream 2 100  # Maximum quality
```

### Frame Rate

Default: 30fps at 640x480

To adjust, edit `CameraCapture` initialization:

```csharp
_camera = new CameraCapture(
    cameraIndex: 0,
    width: 1280,      // Increase for higher quality
    height: 720,      // Increase for higher quality
    fps: 60           // Increase for smoother video
);
```

## References

- [ASP.NET Core SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr)
- [WebRTC Standards](https://webrtc.org/)
- [Kestrel Web Server](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
- [OpenCvSharp](https://github.com/shimat/opencvsharp)

## Support

For issues or questions:
1. Check the guides: `SSL_CONFIGURATION.md`, `CONSOLE_CLIENT_STREAMING.md`
2. Review error messages in console output
3. Check GitHub repository issues

## License

[Add your license here]
