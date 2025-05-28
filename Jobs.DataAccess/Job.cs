using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Jobs.DataAccess
{
    [Table("Job", Schema = "Dagable")]
    public class Job : DomainObject
    {
        public Guid RequestGuid { get; set; }
        public Guid UserGuid { get; set; }
        public string InstanceName { get; set; }
        public string InstanceStatus { get; set; }
        public string InstanceDetailsJson { get; set; }

        public Job() { }

        public Job(Guid requestGuid, Guid userGuid)
        {
            RequestGuid = requestGuid;
            UserGuid = userGuid;
        }

        public T GetInstanceDetails<T>()
        {
            if (string.IsNullOrWhiteSpace(InstanceDetailsJson))
                return default!;

            return JsonSerializer.Deserialize<T>(InstanceDetailsJson)!;
        }

        public Job SetInstanceDetails<T>(T details)
        {
            InstanceDetailsJson = JsonSerializer.Serialize(details);
            return this;
        }
    }
}
