
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Security.Claims;
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

            // Serilog config
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Webapplication1")
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            builder.Host.UseSerilog();

            //ADSF Jwt

            //builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //    .AddJwtBearer(options =>
            //    {
            //        var adsfConfig = builder.Configuration.GetSection("ADSF");

            //        options.Authority = adsfConfig["Authority"];
            //        options.Audience = adsfConfig["Audience"];
            //        options.RequireHttpsMetadata = true;

            //        options.TokenValidationParameters = new TokenValidationParameters
            //        {
            //            ValidateIssuer = true,
            //            ValidIssuer = adsfConfig["Issuer"],

            //            ValidateAudience = true,
            //            ValidAudience = adsfConfig["Audience"],

            //            ValidateLifetime = true,
            //            ClockSkew = TimeSpan.FromMinutes(5),

            //            ValidateIssuerSigningKey = true,

            //            NameClaimType = ClaimTypes.Name,
            //            RoleClaimType = ClaimTypes.Role
            //        };

            //        options.Events = new JwtBearerEvents
            //        {
            //            OnAuthenticationFailed = context =>
            //            {
            //                Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
            //                return Task.CompletedTask;
            //            },
            //            OnTokenValidated = context =>
            //            {
            //                var userName = context.Principal?.Identity?.Name ?? "Unknown";
            //                Log.Information("JWt Token validated for user: {UserName}", userName);
            //                return Task.CompletedTask;
            //            },
            //            OnChallenge = context =>
            //            {
            //                Log.Warning("");
            //                return Task.CompletedTask;
            //            }
            //        };
            //    });

            //builder.Services.AddAuthorization(options =>
            //{
            //    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            //        .RequireAuthenticatedUser()
            //        .Build();

            //    options.AddPolicy("AdminOnly", policy =>
            //        policy.RequireRole("Admin", "Administrator"));

            //    options.AddPolicy("CanManageJobs", policy =>
            //        policy.RequireClaim("permissions", "jobs.manage"));
            //});

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
                    .UseSqlServerStorage(builder.Configuration.GetConnectionString("sqlHangfire"))
                    .UseSerilogLogProvider();
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
            app.UseSerilogRequestLogging();

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

            try
            {
                Log.Information("Starting web application");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                return;
            }
            finally
            {
                Log.CloseAndFlush();
            }

            //app.Run();
        }
    }
}
