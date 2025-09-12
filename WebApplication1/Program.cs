
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Scalar.AspNetCore;
using Serilog;
using System.ComponentModel;
using System.Security.Claims;
using WebApplication1.Services;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using System.Text;
using NeoSmart.Caching.Sqlite;

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

            //ADFS Jwt

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = new TokenValidationParameters()
                        {
                            ValidateIssuer = true,
                            ValidIssuer = "test-issuer",

                            ValidateAudience = true,
                            ValidAudience = "test-audience",

                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromMinutes(5),

                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this-is-a-very-long-secret-key-for-testing-jwt-tokens-in-development-only")),

                            NameClaimType = ClaimTypes.Name,
                            RoleClaimType = ClaimTypes.Role,
                        };
                    }
                    else
                    {
                        var adfsConfig = builder.Configuration.GetSection("ADFS");
                        options.Authority = adfsConfig["Authority"];
                        options.Audience = adfsConfig["Audience"];
                        options.RequireHttpsMetadata = true;

                        options.TokenValidationParameters = new TokenValidationParameters()
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromMinutes(5),
                            NameClaimType= ClaimTypes.Name,
                            RoleClaimType = ClaimTypes.Role
                        };
                    }

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Log.Warning("");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var userName = context.Principal?.Identity?.Name ?? "Unknown";
                            Log.Information("JWT Token validated for user: {UserName}", userName);
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            Log.Warning("JWT Challange triggered: {Error} - {ErrorDescription}");
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                //options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                //    .RequireAuthenticatedUser()
                //    .Build();

                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin", "Administrator"));

                options.AddPolicy("CanManageJobs", policy =>
                    policy.RequireClaim("permissions", "jobs.manage"));
            });

            // YARP
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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
                .WithDefaultEntryOptions(options =>
                {
                    options.Duration = TimeSpan.FromMinutes(5);
                    options.IsFailSafeEnabled = true;
                    options.FailSafeMaxDuration = TimeSpan.FromHours(1);
                    options.FailSafeThrottleDuration = TimeSpan.FromMinutes(1);
                })
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                .AsHybridCache();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    if (document.Components == null)
                        document.Components = new OpenApiComponents();

                    if (document.Components.SecuritySchemes == null)
                        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>();

                    document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Enter your JWT token"
                    });

                    document.SecurityRequirements = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = "Bearer"
                                    }
                                },
                                Array.Empty<string>()
                            }
                        }
                    };

                    return Task.CompletedTask;
                });
            });

            // Healthchecks, Kolla in det senare
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            //Serilog
            app.UseSerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }        

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();      
            app.MapControllers();
            
            app.MapReverseProxy();
            app.MapHangfireDashboard("/hangfire"); // Hangfire dashboard

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
