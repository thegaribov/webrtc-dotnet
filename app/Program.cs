using WebRtcConference.Hubs;

var builder = WebApplication.CreateBuilder(args);

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

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
app.Run($"http://0.0.0.0:{port}");
