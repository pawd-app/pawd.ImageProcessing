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

internal class Program
{
    public static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables();
            })
            .UseSerilog() 
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

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobWorkersDbContext>();
            db.Database.Migrate();
        }

        await host.RunAsync();
    }
}
