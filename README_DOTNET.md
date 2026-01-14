# WebRTC Conference - .NET Version

A real-time video conference system built with ASP.NET Core and SignalR, featuring WebRTC for peer-to-peer connections.

## Features

- **Video Conferencing**: Real-time video conferencing using WebRTC
- **Signaling**: ASP.NET Core SignalR for WebRTC signaling
- **Chat**: Real-time text chat using SignalR
- **Media Controls**: Audio/video toggle and screen sharing
- **Room Management**: Join/leave conference rooms
- **Docker Support**: Full Docker Compose setup with Nginx reverse proxy and SSL

## Prerequisites

- .NET 8.0 SDK
- Docker & Docker Compose (for containerized deployment)
- Modern web browser with WebRTC support

## Local Development

### Build the project
```bash
dotnet build
```

### Run the application
```bash
dotnet run
```

The application will start on `http://localhost:3000`

### Watch mode (with auto-reload)
```bash
dotnet watch run
```

## Docker Deployment

### Build and run with Docker Compose
```bash
docker-compose up --build
```

The application will be available at:
- `http://localhost` (HTTP - redirects to HTTPS)
- `https://localhost` (HTTPS with self-signed certificate)

## Project Structure

```
app/
├── Dockerfile                 # Multi-stage Docker build
├── Program.cs                 # ASP.NET Core configuration
├── WebRtcConference.csproj   # Project file
├── Hubs/
│   └── ConferenceHub.cs      # SignalR hub for WebRTC signaling
├── Models/                    # Data models
└── wwwroot/
    └── index.html            # Frontend (HTML/CSS/JavaScript)
```

## Technology Stack

- **Backend**: ASP.NET Core 8.0
- **Real-time Communication**: SignalR
- **Frontend**: HTML5, CSS3, JavaScript (Vanilla)
- **P2P Media**: WebRTC
- **Containerization**: Docker & Docker Compose
- **Reverse Proxy**: Nginx

## Key Components

### ConferenceHub.cs
SignalR hub that handles:
- Room management (join/leave)
- WebRTC signaling (offer/answer/ICE candidates)
- Chat messaging
- Media state updates (audio/video status)

### index.html
Frontend client with:
- Login interface for room/username
- Video grid layout
- Media controls (audio, video, screen share)
- Real-time chat
- WebRTC peer connection management

## Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Set to `Production` for deployment
- `ASPNETCORE_URLS`: Server URL binding (default: `http://+:3000`)
- `PORT`: Application port

## CORS Configuration

The application allows CORS from all origins by default. For production, modify the CORS policy in `Program.cs`:

```csharp
options.AddPolicy("AllowAll", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});
```

## SSL/TLS

For HTTPS support in Docker, certificates are mounted from the `ssl/` directory. Generate self-signed certificates:

```bash
openssl req -x509 -newkey rsa:4096 -keyout server.key -out server.crt -days 365 -nodes
```

## Browser Support

- Chrome 51+
- Firefox 55+
- Safari 11+
- Edge 15+

WebRTC requires HTTPS in production (HTTP allowed for localhost).

## License

MIT
