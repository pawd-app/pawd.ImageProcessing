namespace Jobs.DataAccess
{
    public class DomainObject
    {
 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public string LastUpdatedBy { get; set; } = "SYSTEM";
    }
}
