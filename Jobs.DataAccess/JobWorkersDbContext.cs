using Microsoft.EntityFrameworkCore;

namespace Jobs.DataAccess
{
    public class JobWorkersDbContext : DbContext
    {
        public DbSet<Job> Jobs { get; set; }

        public JobWorkersDbContext() : base()
        {
        }

        public JobWorkersDbContext(DbContextOptions<JobWorkersDbContext> options)
        : base(options)
        {
        }
    }
}
