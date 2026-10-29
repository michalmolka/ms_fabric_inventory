namespace blz0.Models
{
    public class UserObjectMap
    {
        public int Id { get; set; }
        public string UserObjectId { get; set; } = default!;
        public string? UserKey { get; set; }
        public string? UserName { get; set; }
        public string? TenantId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime LastSeenAtUtc { get; set; }
    }
}
