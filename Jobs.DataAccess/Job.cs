using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Jobs.DataAccess
{
    [Table("Job")]
    public class Job : DomainObject
    {
        [Column(TypeName = "char(36)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid JobGuid { get; set; }

        [Required]
        [Column(TypeName = "char(36)")]
        public Guid UserGuid { get; set; }

        public string InstanceName { get; set; }
        public string InstanceStatus { get; set; }
        public string InstanceDetailsJson { get; set; }

        public Job() { }

        public Job(Guid userGuid)
        {
            UserGuid = userGuid;
        }

        public T GetInstanceDetails<T>()
        {
            return string.IsNullOrWhiteSpace(InstanceDetailsJson)
                ? default!
                : JsonSerializer.Deserialize<T>(InstanceDetailsJson)!;
        }

        public Job SetInstanceDetails<T>(T details)
        {
            InstanceDetailsJson = JsonSerializer.Serialize(details);
            return this;
        }
    }
}
