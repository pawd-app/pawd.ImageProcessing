using Jobs.DataAccess;

namespace JobManagement.DataAccess.Repositories
{
    public interface IRepository<Entity> where Entity : DomainObject
    {
        Task<Entity> InsertAsync(Entity entity, CancellationToken ct);

        Task<Entity> UpdateAsync(Entity entity, CancellationToken ct);

        Task<Job> GetByIdAsync(Guid jobGuid, CancellationToken ct);
    }
}
