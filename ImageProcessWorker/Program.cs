using ImageProcessWorker;
using Jobs.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Reflection;
using Amazon.Runtime;
using Amazon.S3;
using JobManagement.Sdk;
using Jobs.ImageProcess.UploadValidation;

using var host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();

var services = scope.ServiceProvider;

services.GetRequiredService<JobWorkersDbContext>().Database.Migrate();

try
{

    await services.GetRequiredService<Runner>().Run(args);
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}

return;

static IHostBuilder CreateHostBuilder(string[] strings)
{
    return Host.CreateDefaultBuilder()
        
          .ConfigureAppConfiguration(app =>
          {
              app.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
          })
        .ConfigureServices((context, services) =>
        {
            var migrationsAssembly = typeof(JobWorkersDbContext).GetTypeInfo().Assembly.GetName().Name;
            var mySqlConnectionStr = context.Configuration.GetConnectionString("DefaultConnection");

            services
                .AddDbContext<JobWorkersDbContext>(opt =>
                {
                    opt.UseMySql(mySqlConnectionStr, ServerVersion.AutoDetect(mySqlConnectionStr), sql => sql.MigrationsAssembly(migrationsAssembly));
                    opt.UseMySql(ServerVersion.AutoDetect(mySqlConnectionStr), b => b.SchemaBehavior(MySqlSchemaBehavior.Translate, (schema, entity) => $"{schema ?? "dbo"}_{entity}"));
                })
                .AddScoped<Runner>()
                .Configure<AppOptions>(context.Configuration);

            services.AddJobManagementSystem(
                options => { options.UseMySql(mySqlConnectionStr, ServerVersion.AutoDetect(mySqlConnectionStr), sql => sql.MigrationsAssembly(migrationsAssembly)); });
            services.AddScoped<IImageProcessor, ImageProcessor>();
            services.Configure<GarageS3Settings>(context.Configuration.GetSection("GarageS3"));
            services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                new BasicAWSCredentials(
                    context.Configuration["GarageS3:AccessKey"],
                    context.Configuration["GarageS3:SecretKey"]
                ),
                new AmazonS3Config
                {
                    ServiceURL = context.Configuration["GarageS3:ServiceURL"],
                    ForcePathStyle = true,
                    UseHttp = true,
                    AuthenticationRegion = "garage",
                }
            ));
        });
}