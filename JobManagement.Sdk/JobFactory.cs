
using JobManagement.DataAccess.Repositories;
using Jobs.DataAccess;

namespace JobManagement.Sdk
{
    public interface IJobFactory
    {
        Task<Job> CreateJobAsync<TDetails>(
            Guid userGuid,
            TDetails details,
            string instanceName,
            string instanceStatus,
            CancellationToken ct = default) where TDetails : class;

        Task<Job> CreateJobAsync<TDetails>(
            Guid userGuid,
            string instanceName,
            string instanceStatus,
            CancellationToken ct = default) where TDetails : class, new();
    }

    public class JobFactory : IJobFactory
    {
        private readonly IRepository<Job> _repository;

        public JobFactory(IRepository<Job> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Job> CreateJobAsync<TDetails>(
            Guid userGuid,
            TDetails details,
            string instanceName,
            string instanceStatus,
            CancellationToken ct = default) where TDetails : class
        {
            ValidateParameters(userGuid, details, instanceName, instanceStatus);

            var job = new Job()
            {
                UserGuid = userGuid,
                InstanceName = instanceName,
                InstanceStatus = instanceStatus
            }.SetInstanceDetails(details);

            return await _repository.InsertAsync(job, ct);
        }

        public async Task<Job> CreateJobAsync<TDetails>(
            Guid userGuid,
            string instanceName,
            string instanceStatus,
            CancellationToken ct = default) where TDetails : class, new()
        {
            return await CreateJobAsync(
                userGuid,
                new TDetails(),
                instanceName,
                instanceStatus,
                ct);
        }

        private static void ValidateParameters<TDetails>(
            Guid userGuid,
            TDetails details,
            string instanceName,
            string instanceStatus)
        {
            if (userGuid == Guid.Empty)
                throw new ArgumentException("User GUID cannot be empty", nameof(userGuid));

            if (details == null)
                throw new ArgumentNullException(nameof(details));

            if (string.IsNullOrWhiteSpace(instanceName))
                throw new ArgumentException("Instance name is required", nameof(instanceName));

            if (string.IsNullOrWhiteSpace(instanceStatus))
                throw new ArgumentException("Instance status is required", nameof(instanceStatus));
        }
    }
}
