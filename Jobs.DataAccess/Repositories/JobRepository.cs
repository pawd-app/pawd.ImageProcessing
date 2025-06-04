using Jobs.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace JobManagement.DataAccess.Repositories
{
    public class JobRepository : IRepository<Job>
    {
        private readonly JobWorkersDbContext _db;

        public JobRepository(JobWorkersDbContext db)
        {
            _db = db;
        }
        
        public async Task<Job> GetByIdAsync(Guid jobGuid, CancellationToken ct)
        {
            return await _db.Jobs.FirstOrDefaultAsync(x => x.JobGuid == jobGuid, ct);
        }

        public async Task<Job> InsertAsync(Job entity, CancellationToken ct)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.UserGuid == Guid.Empty)
                throw new ArgumentException("UserGuid must be provided");

            entity.JobGuid = Guid.Empty;

            _db.Jobs.Add(entity);
            
            await _db.SaveChangesAsync(ct);

            await _db.Entry(entity).ReloadAsync(ct);

            return entity;
        }

        public async Task<Job> UpdateAsync(Job entity, CancellationToken ct)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.JobGuid == Guid.Empty)
                throw new ArgumentException("JobGuid must be provided");

            var existingJob = await _db.Jobs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.JobGuid == entity.JobGuid, cancellationToken: ct);

            if (existingJob == null)
                throw new KeyNotFoundException($"Job with JobGuid {entity.JobGuid} not found");

            _db.Jobs.Update(entity);
            await _db.SaveChangesAsync(ct);

            return entity;
        }
    }
}
