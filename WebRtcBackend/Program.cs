using WebRtcBackend.Hubs;

namespace WebRtcBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddSignalR();

            var app = builder.Build();


            app.UseCors(builder =>
            {
                builder
                    .WithOrigins("https://localhost:7198")
                    .AllowCredentials()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });

            app.MapGet("/", () => "Web RTC Backend!");

            app.MapHub<ConferenceHub>("/conferenceHub");

            app.Run();
        }
    }
}
