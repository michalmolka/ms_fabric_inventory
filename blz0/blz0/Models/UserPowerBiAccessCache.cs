namespace blz0.Models
{
    public class UserPowerBiAccessCache
    {
        public int Id { get; set; }
        public string UserKey { get; set; } = default!;
        public string? UserName { get; set; }
        public string ObjectType { get; set; } = default!;
        public string ObjectId { get; set; } = default!;
        public string? ScopeId { get; set; }
        public DateTime FetchedAtUtc { get; set; }
    }
}
