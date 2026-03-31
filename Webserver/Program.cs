using Webserver.DataCache;
using Webserver.Hubs;
using Webserver.Interfaces;

namespace Webserver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddSignalR(options => options.MaximumParallelInvocationsPerClient = 64);
            builder.Services.AddSingleton<EventPublisher>();
            builder.Services.AddSingleton<ISwarmCache, SwarmDataCache>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(_ => true)
                        .AllowCredentials();
                });
            });
            builder.Logging.AddConsole();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("AllowAll");

            app.MapControllers();
            app.MapHub<DataHub>("/datahub");
            app.MapHub<UiHub>("/uihub");
            app.MapFallbackToFile("index.html");
            app.Run();
        }
    }
}
