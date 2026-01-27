using WebRtcBackend.Hubs;
using System.Security.Cryptography.X509Certificates;

namespace WebRtcBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddSignalR();

            // Configure CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWebClient", policy =>
                {
                    policy
                        .WithOrigins(
                            "https://localhost:5000", 
                            "https://192.168.68.236:5000", 
                            "https://127.0.0.1:5000",
                            "http://localhost:5000", 
                            "http://192.168.68.236:5000", 
                            "http://127.0.0.1:5000")
                        .AllowCredentials()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            //// Configure HTTPS with certificate
            //var certPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SSL", "192.168.68.236.cert.pem");
            //var keyPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SSL", "192.168.68.236.key.pem");

            //if (File.Exists(certPath) && File.Exists(keyPath))
            //{
            //    try
            //    {
            //        var cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);
            //        builder.WebHost.ConfigureKestrel(options =>
            //        {
            //            options.ListenAnyIP(5001, listenOptions =>
            //            {
            //                listenOptions.UseHttps(cert);
            //            });
            //            options.ListenAnyIP(5079, listenOptions =>
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
            //else
            //{
            //    Console.WriteLine("??  SSL certificate files not found at:");
            //    Console.WriteLine($"    Cert: {certPath}");
            //    Console.WriteLine($"    Key: {keyPath}");
            //}

            var app = builder.Build();

            // Apply CORS before mapping endpoints
            app.UseCors("AllowWebClient");

            app.MapGet("/", () => "Web RTC Backend!");

            // Apply CORS to the SignalR hub endpoint
            app.MapHub<ConferenceHub>("/conferenceHub").RequireCors("AllowWebClient");

            app.Run();
        }
    }
}
