namespace WebClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Get backend server URL from environment
            var backendServerUrl = Environment.GetEnvironmentVariable("BACKEND_SERVER_URL") ?? "https://192.168.68.236:5000";
            
            //// Configure HTTPS with certificate
            //var certPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SSL", "192.168.68.236.cert.pem");
            //var keyPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SSL", "192.168.68.236.key.pem");

            //if (File.Exists(certPath) && File.Exists(keyPath))
            //{
            //    try
            //    {
            //        var cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPemFile(certPath, keyPath);
            //        builder.WebHost.ConfigureKestrel(options =>
            //        {
            //            options.ListenAnyIP(5000, listenOptions =>
            //            {
            //                listenOptions.UseHttps(cert);
            //            });
            //        });
            //        Console.WriteLine("? SSL certificates loaded successfully");
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"??  Failed to load SSL certificates: {ex.Message}");
            //    }
            //}
            
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

            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseStaticFiles();

            // Configuration endpoints
            app.MapGet("/api/config", () =>
            {
                return Results.Ok(new { 
                    backendServerUrl = backendServerUrl,
                    environment = app.Environment.EnvironmentName,
                    timestamp = DateTime.UtcNow
                });
            });

            app.MapGet("/api/info", () =>
            {
                return Results.Ok(new 
                { 
                    service = "WebRTC WebClient",
                    version = "1.0",
                    backendServer = backendServerUrl,
                    timestamp = DateTime.UtcNow
                });
            });

            app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

            // SPA fallback - serve index.html for all unknown routes
            app.MapFallback(async context =>
            {
                var indexFile = Path.Combine(app.Environment.WebRootPath, "index.html");
                if (File.Exists(indexFile))
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.SendFileAsync(indexFile);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("index.html not found");
                }
            });

            // Log startup info
            Console.WriteLine("=== WebRTC WebClient Server ===");
            Console.WriteLine($"Port: https://0.0.0.0:5000");
            Console.WriteLine($"Backend Server: {backendServerUrl}");
            Console.WriteLine($"Config Endpoint: /api/config");
            Console.WriteLine($"Info Endpoint: /api/info");
            Console.WriteLine($"Health Check: /health");
            Console.WriteLine("================================");

            app.Run();
        }
    }
}
