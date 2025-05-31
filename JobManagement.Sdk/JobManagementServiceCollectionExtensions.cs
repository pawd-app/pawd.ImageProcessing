using JobManagement.DataAccess.Repositories;
using JobManagement.Sdk;
using Jobs.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class JobManagementServiceCollectionExtensions
{
    /// <summary>
    /// Registers JobFactory with your specific JobRepository implementation
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="dbContextConfig">Optional action to configure DbContext</param>
    public static IServiceCollection AddJobManagementSystem(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbContextConfig = null!)
    {
        services.AddScoped<IRepository<Job>, JobRepository>();

        services.AddScoped<IJobFactory, JobFactory>();

        if (dbContextConfig != null)
        {
            services.AddDbContext<JobWorkersDbContext>(dbContextConfig);
            services.BuildServiceProvider()
                  .GetRequiredService<JobWorkersDbContext>()
                  .Database.Migrate();
        }

        return services;
    }
}