using WebRtcConference.Hubs;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for HTTPS support
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    var httpPort = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "3000");
    var httpsPort = int.Parse(Environment.GetEnvironmentVariable("HTTPS_PORT") ?? "3443");

    serverOptions.Listen(System.Net.IPAddress.Any, httpPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    serverOptions.Listen(System.Net.IPAddress.Any, httpsPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        listenOptions.UseHttps();
    });
});

// Add services
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
app.UseHttpsRedirection();
app.UseHsts();
app.UseCors("AllowAll");
app.UseStaticFiles();

// Map SignalR hub
app.MapHub<ConferenceHub>("/conferenceHub");

// Serve index.html for root path
app.MapFallback(async context =>
{
    var indexFile = Path.Combine(app.Environment.WebRootPath, "index.html");
    if (File.Exists(indexFile))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(indexFile);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("index.html not found");
    }
});

app.Run();
