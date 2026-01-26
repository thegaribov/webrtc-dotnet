# WebRTC Console Client - Camera Streaming Guide

## Overview

The Console Client now supports real-time camera streaming via WebRTC signaling. It captures video frames from your webcam, encodes them, and sends them to other peers in the conference room.

## Features

### ?? Camera Capture
- **Real-time Video Capture**: Captures frames at 30fps from connected camera
- **Resolution**: 640x480 (configurable)
- **Frame Buffering**: Maintains a queue of recent frames to handle network delays

### ?? Video Streaming
- **Adaptive Quality**: Adjustable JPEG quality (1-100%)
- **Frame Encoding**: JPEG encoding for efficient transmission
- **Real-time Statistics**: Displays FPS and bandwidth metrics

### ?? Stream Management
- **Multiple Streams**: Can stream to multiple users simultaneously
- **Per-user Quality Control**: Set different quality levels for different recipients
- **Stream Status**: Monitor active streams

## Installation

### Prerequisites
- .NET 10
- Webcam connected to your system
- SignalR backend server running

### Dependencies Added
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="10.0.0" />
<PackageReference Include="OpenCvSharp4" Version="4.10.0.20240616" />
<PackageReference Include="OpenCvSharp4.runtime.win" Version="4.10.0.20240616" />
```

## Usage

### Starting the Console Client

```bash
dotnet run
```

You will be prompted to enter:
- Your name
- Room ID to join

### Available Commands

#### Basic Commands
```
list               - Show all users in the room
info               - Display connection status
camera-info        - Show camera status and buffered frames
help               - Show all available commands
exit               - Exit the application
```

#### WebRTC Signaling
```
offer <id>         - Send SDP offer to initiate connection
answer <id>        - Send SDP answer to establish connection
ice <id>           - Send ICE candidate for connectivity
```

#### Video Streaming
```
stream <id> [quality]  - Start video stream to user (quality: 1-100, default 80)
stop-stream <id>       - Stop video stream to user
streams                - List all active video streams
```

### Examples

```
> list
?? Users in Room:
  [0] Alice
      ID: abc123def456...
  [1] Bob
      ID: xyz789uvw012...

> stream 0 80
? Video stream started to abc123def456... (quality: 80%)

> streams
?? Active Video Streams:
  • abc123def456... - 150 frames sent

> stop-stream 0
? Video stream stopped for abc123def456...
```

## Services Overview

### CameraCapture.cs
Manages webcam input:
- Initializes video capture device
- Captures frames continuously in background thread
- Maintains frame buffer for smooth streaming
- Provides thread-safe frame retrieval

```csharp
var camera = new CameraCapture(cameraIndex: 0, width: 640, height: 480, fps: 30);
camera.Initialize();
camera.StartCapture();
var frame = camera.GetLatestFrame();
```

### VideoStreamEncoder.cs
Handles frame encoding and transmission:
- Encodes frames to JPEG format
- Controls quality level
- Sends frames via SignalR
- Calculates and displays streaming statistics

```csharp
var encoder = new VideoStreamEncoder(connection, camera, targetUserId, quality: 80);
encoder.StartStreaming();
encoder.StopStreaming();
```

### ConferenceConnection.cs
Extended with video streaming methods:
- `SendVideoFrameAsync()` - Transmit encoded frame
- `OnVideoFrameReceived` - Event for receiving frames
- Manages frame data serialization

## Video Frame Structure

Transmitted video frames contain:
```csharp
public class VideoFrameData
{
    public long Timestamp { get; set; }        // Unix milliseconds
    public int FrameIndex { get; set; }        // Frame sequence number
    public int Width { get; set; }             // Image width
    public int Height { get; set; }            // Image height
    public string EncodedData { get; set; }    // Base64 encoded JPEG
    public int QualityLevel { get; set; }      // JPEG quality (1-100)
    public int BytesSize { get; set; }         // Encoded frame size
}
```

## Backend Support

The WebRTC backend (`ConferenceHub.cs`) has been updated with:
```csharp
public async Task SendVideoFrame(string targetId, object frameData)
```

This method relays video frames from sender to recipient through the SignalR hub.

## Performance Notes

- **Frame Rate**: Targets 30fps but depends on camera, encoding speed, and network
- **Quality vs Bandwidth**: Lower quality (50-60) for poor networks, higher (85-100) for fast connections
- **Latency**: Typical frame-to-frame latency 50-100ms
- **CPU Usage**: Moderate CPU due to JPEG encoding

## Troubleshooting

### Camera Not Detected
```
? Failed to open camera
```
- Check if camera is connected
- Verify no other application is using the camera
- Try `camera-info` command to check status

### Poor Video Quality
- Reduce quality setting: `stream 0 50`
- Check network connectivity
- Monitor bandwidth usage

### Frames Not Showing
- Verify peer is running and connected
- Check SignalR connection status with `info`
- Ensure both peers are in the same room

## Future Enhancements

- [ ] Audio streaming support
- [ ] Multiple camera selection
- [ ] Video codec selection (H.264, VP8, VP9)
- [ ] Bandwidth throttling
- [ ] Recording functionality
- [ ] Screen sharing option
- [ ] Video preview in terminal (ASCII art)
