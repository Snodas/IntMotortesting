
using Hangfire;
using Microsoft.AspNetCore.Identity.UI.Services;
using Scalar.AspNetCore;
using Serilog;
using WebApplication1.Services;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // YARP
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            // SQLite
            builder.Services.AddEasyCaching(opt =>
            {
                opt.UseSQLite(config =>
                {
                    config.DBConfig = new EasyCaching.SQLite.SQLiteDBOptions
                    {
                        FileName = "cache.db",
                    };
                }, "slqlite");
            });

            // Hangfire
            builder.Services.AddHangfire(config =>
            {
                config.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("sqlHangfire"));
            });

            builder.Services.AddHangfireServer();

            builder.Services.AddScoped<IJobTestService, JobTestService>();

            // FushionCache
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddFusionCache()
                .WithDefaultEntryOptions(opt => opt.Duration = TimeSpan.FromMinutes(5))
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                .AsHybridCache();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            //Serilog
            //app.UseSerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();      
            app.MapControllers();
            
            app.MapReverseProxy();
            app.MapHangfireDashboard("/hangfire");

            app.Run();
        }
    }
}
