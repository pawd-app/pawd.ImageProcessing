
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

        Task<Job> GetJobAsync(Guid jobGuid, CancellationToken ct = default);

        Task<Job> UpdateJobAsync<TDetails>(
            Guid jobGuid,
            string instanceStatus,
            TDetails details,
            CancellationToken ct = default) where TDetails : class;
    }

    public class JobFactory : IJobFactory
    {
        private readonly IRepository<Job> _repository;

        public JobFactory(IRepository<Job> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }
        
        public async Task<Job> GetJobAsync(Guid jobGuid, CancellationToken ct = default)
        {
            if (jobGuid == Guid.Empty)
                throw new ArgumentException("JobGuid must be provided", nameof(jobGuid));

            var job = await _repository.GetByIdAsync(jobGuid, ct);
    
            if (job == null)
                throw new KeyNotFoundException($"Job with JobGuid {jobGuid} not found");

            return job;
        }

        public async Task<Job> UpdateJobAsync<TDetails>(
            Guid jobGuid,
            string instanceStatus,
            TDetails details,
            CancellationToken ct = default) where TDetails : class
        {
            if (jobGuid == Guid.Empty)
                throw new ArgumentException("JobGuid must be provided", nameof(jobGuid));

            if (string.IsNullOrWhiteSpace(instanceStatus))
                throw new ArgumentException("InstanceStatus must be provided", nameof(instanceStatus));

            if (details == null)
                throw new ArgumentNullException(nameof(details));

            var existingJob = await GetJobAsync(jobGuid, ct);
    
            existingJob.InstanceStatus = instanceStatus;
            existingJob.SetInstanceDetails(details);

            return await _repository.UpdateAsync(existingJob, ct);
        }

        public async Task<Job> CreateJobAsync<TDetails>(
            Guid userGuid,
            TDetails details,
            string instanceName,
            string instanceStatus,
            CancellationToken ct = default) where TDetails : class
        {
            ValidateParameters(userGuid, details, instanceName, instanceStatus);

            var job = new Job
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
