
using Scalar.AspNetCore;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddFusionCache()
                .WithDefaultEntryOptions(opt => opt.Duration = TimeSpan.FromMinutes(5))
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                //.WithDistributedCache()
                .AsHybridCache();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();      
            app.MapControllers();
            app.MapReverseProxy();

            app.Run();
        }
    }
}
