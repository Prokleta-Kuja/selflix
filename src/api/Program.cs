using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using selflix.Auth;
using selflix.Db;
using selflix.Extensions;
using selflix.Services;
using Serilog;
using Serilog.Events;

namespace selflix;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(C.IsDebug ? LogEventLevel.Debug : LogEventLevel.Information)
            .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Warning)
            .MinimumLevel.Override(nameof(Hangfire), LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();
            builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.All);
            builder.Services.AddMemoryCache();
            builder.Services.AddDataProtection().PersistKeysToDbContext<AppDbContext>();
            switch (C.DbContextType)
            {
                case DbContextType.PostgreSQL: builder.Services.AddDbContext<AppDbContext, PostgresDbContext>(); break;
                case DbContextType.SQLite: builder.Services.AddDbContext<AppDbContext, SqliteDbContext>(); break;
            }

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.UseOneOfForPolymorphism();
                options.UseAllOfForInheritance();

                options.DescribeAllParametersInCamelCase();
                options.SchemaFilter<OpenApiEnumSchemaFilter>();
                options.SupportNonNullableReferenceTypes();
                options.UseAllOfToExtendReferenceSchemas();
            });

            builder.Services.AddSingleton<IPasswordHasher, PasswordHashingService>();
            builder.Services
                .AddAuthentication(AppAuthenticationHandler.AUTHENTICATION_SCHEME)
                .AddAppAuthentication();

            builder.Services.AddControllers(opt =>
                {
                    opt.Filters.Add<ExceptionFilter>();
                })
                .ConfigureApiBehaviorOptions(opt =>
                {
                    opt.InvalidModelStateResponseFactory = BadRequestFactory.Handle;
                })
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull;
                    o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    o.JsonSerializerOptions.WriteIndented = true;
                });
            // In production, the SPA files will be served from this directory
            builder.Services.AddSpaStaticFiles(c => { c.RootPath = "web"; });

            // Add Hangfire services.
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSQLiteStorage(C.Paths.Hangfire, new SQLiteStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    InvisibilityTimeout = TimeSpan.FromMinutes(30),
                    DistributedLockLifetime = TimeSpan.FromSeconds(30),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                }));

            // Remove Hangfire culture filter
            var captureFilter = GlobalJobFilters.Filters.OfType<JobFilter>().Where(c => c.Instance is CaptureCultureAttribute).FirstOrDefault();
            if (captureFilter != null)
                GlobalJobFilters.Filters.Remove(captureFilter.Instance);

            // Add the processing server as IHostedService
            builder.Services.AddHangfireServer(o =>
            {
                o.ServerName = nameof(selflix);
                o.WorkerCount = 1;//Math.Max(2, Environment.ProcessorCount / 2);
            });

            builder.Services.AddHttpClient<HibpService>();

            var app = builder.Build();
            await Initialize(app.Services);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
                app.UseForwardedHeaders();

            app.UseSpaStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseJobDashboard();
            app.ReregisterRecurringJobs();

            app.MapControllers().RequireAuthorization();

            app.MapWhen(x => !x.Request.Path.Value!.StartsWith("/api/"), builder =>
            {
                builder.UseSpa(spa =>
                {
                    spa.Options.SourcePath = "../web";
                    if (app.Environment.IsDevelopment())
                        spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
                });
            });

            Log.Information("App started");
            app.Run();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static async Task Initialize(IServiceProvider provider)
    {
        Directory.CreateDirectory(C.Paths.ConfigData);
        Directory.CreateDirectory(C.Paths.MediaData);

        using var scope = provider.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await db.InitializeDefaults(hasher);
    }
}
