using ImageProcessWorker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IHost host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();

var services = scope.ServiceProvider;

try
{
    await services.GetRequiredService<Runner>().Run(args);
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}

static IHostBuilder CreateHostBuilder(string[] strings)
{
    return Host.CreateDefaultBuilder()
          .ConfigureAppConfiguration(app =>
          {
              app.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
          })
        .ConfigureServices((context, services) =>
        {
            services
                .AddScoped<Runner>()
                .Configure<AppOptions>(context.Configuration);
        });

}