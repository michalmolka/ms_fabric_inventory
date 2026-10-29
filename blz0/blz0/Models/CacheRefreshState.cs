namespace blz0.Models
{
    public class CacheRefreshState
    {
        public int Id { get; set; }
        public string CacheKey { get; set; } = default!;
        public string ObjectType { get; set; } = default!;
        public string? ScopeId { get; set; }
        public string UserObjectId { get; set; } = string.Empty;
        public DateTime RefreshedAtUtc { get; set; }
        public int RowCount { get; set; }
    }
}
