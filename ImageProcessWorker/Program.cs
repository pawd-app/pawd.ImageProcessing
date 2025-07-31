using Amazon.Runtime;
using Amazon.S3;
using ImageProcessWorker;
using Jobs.DataAccess;
using Jobs.ImageProcess.UploadValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Reflection;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var migrationsAssembly = typeof(JobWorkersDbContext).GetTypeInfo().Assembly.GetName().Name;
        var mySqlConnectionStr = configuration.GetConnectionString("DefaultConnection");

        services
            .AddDbContext<JobWorkersDbContext>(opt =>
            {
                opt.UseMySql(mySqlConnectionStr, ServerVersion.AutoDetect(mySqlConnectionStr), sql => sql.MigrationsAssembly(migrationsAssembly));
                opt.UseMySql(ServerVersion.AutoDetect(mySqlConnectionStr), b => b.SchemaBehavior(MySqlSchemaBehavior.Translate, (schema, entity) => $"{schema ?? "dbo"}_{entity}"));
            });

        // Run migrations at startup
        using (var scope = services.BuildServiceProvider().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobWorkersDbContext>();
            db.Database.Migrate();
        }

        services.Configure<AppOptions>(configuration);

        services.AddJobManagementSystem(
            options => { options.UseMySql(mySqlConnectionStr, ServerVersion.AutoDetect(mySqlConnectionStr), sql => sql.MigrationsAssembly(migrationsAssembly)); });

        services.AddScoped<IImageProcessor, ImageProcessor>();
        services.Configure<S3Settings>(configuration.GetSection("S3Settings"));
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
            new BasicAWSCredentials(
                context.Configuration["S3Settings:AccessKey"],
                context.Configuration["S3Settings:SecretKey"]
            ),
            new AmazonS3Config
            {
                ServiceURL = context.Configuration["S3Settings:ServiceURL"],
                ForcePathStyle = true,
                UseHttp = true,
                AuthenticationRegion = "garage",
            }
        ));

        services.AddHostedService<Runner>();
    })
    .Build()
    .RunAsync();
