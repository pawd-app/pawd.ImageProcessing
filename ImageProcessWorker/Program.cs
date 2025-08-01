using Amazon.Runtime;
using Amazon.S3;
using ImageProcessWorker;
using Jobs.DataAccess;
using Jobs.ImageProcess.UploadValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Debugging;

internal class Program
{
    public static async Task Main(string[] args)
    {
        // 1) Enable Serilog internal self-logging for setup issues
        SelfLog.Enable(msg =>
        {
            Console.Error.WriteLine($"[Serilog SelfLog] {msg}");
            File.AppendAllText("serilog_selflog.txt", $"{DateTime.UtcNow:o} {msg}{Environment.NewLine}");
        });

        // 2) Build configuration (JSON + env-vars)
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // 3) Bootstrap a static Serilog logger to Console for debug
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        // 4) Dump loaded Serilog configuration keys
        Log.Information("=== Serilog Configuration Dump ===");
        foreach (var kv in config.AsEnumerable()
                         .Where(k => k.Key.StartsWith("Serilog:Using") || k.Key.StartsWith("Serilog:WriteTo")))
        {
            Log.Information("{Key} = {Value}", kv.Key, kv.Value);
        }
        Log.Information("===================================");

        // 5) Emit test log events
        Log.Information("🔥 Test Information log");
        Log.Warning("⚠️ Test Warning log");

        // 6) Build the Generic Host with the static logger
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables();
            })
            .UseSerilog()  // picks up the pre-built Log.Logger without reloadable wrapper
            .ConfigureServices((ctx, services) =>
            {
                var c = ctx.Configuration;
                var migrationsAssembly = typeof(JobWorkersDbContext).Assembly.GetName().Name;
                var conn = c.GetConnectionString("DefaultConnection");

                services.AddDbContext<JobWorkersDbContext>(opt =>
                    opt.UseMySql(conn, ServerVersion.AutoDetect(conn),
                                 sql => sql.MigrationsAssembly(migrationsAssembly)));

                services.Configure<AppOptions>(c);
                services.AddJobManagementSystem(o =>
                    o.UseMySql(conn, ServerVersion.AutoDetect(conn),
                               s => s.MigrationsAssembly(migrationsAssembly)));

                services.AddScoped<IImageProcessor, ImageProcessor>();
                services.Configure<S3Settings>(c.GetSection("S3Settings"));
                services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                    new BasicAWSCredentials(
                        c["S3Settings:AccessKey"],
                        c["S3Settings:SecretKey"]
                    ),
                    new AmazonS3Config
                    {
                        ServiceURL = c["S3Settings:ServiceURL"],
                        ForcePathStyle = true,
                        UseHttp = true,
                        AuthenticationRegion = "garage",
                    }
                ));

                services.AddHostedService<Runner>();
            })
            .Build();

        // 7) Apply EF Core migrations after DI container finalization
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobWorkersDbContext>();
            db.Database.Migrate();
        }

        // 8) Run the worker
        await host.RunAsync();
    }
}
