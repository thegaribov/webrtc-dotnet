using WebRtcConference.Hubs;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for HTTP only (no HTTPS for local dev)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    var httpPort = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "3000");

    serverOptions.Listen(System.Net.IPAddress.Any, httpPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

// Add services
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32 MB for large SDP offers
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
app.UseRouting();
app.UseCors("AllowAll");

// Map SignalR hub
app.MapHub<ConferenceHub>("/conferenceHub");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// API info endpoint
app.MapGet("/api/info", () => Results.Ok(new
{
    service = "WebRtcConference Backend",
    version = "1.0",
    timestamp = DateTime.UtcNow
}));

// Log startup info
Console.WriteLine("=== WebRtcConference Backend Server ===");
Console.WriteLine($"HTTP Port: {Environment.GetEnvironmentVariable("PORT") ?? "3000"}");
Console.WriteLine("SignalR Hub: /conferenceHub");
Console.WriteLine("Health Check: /health");
Console.WriteLine("=====================================");

app.Run();
