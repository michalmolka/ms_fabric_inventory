using Microsoft.EntityFrameworkCore;
using blz0.Models;

namespace blz0.Data
{
    public class blz0Context : DbContext
    {
        public blz0Context (DbContextOptions<blz0Context> options)
            : base(options)
        {
        }
        public DbSet<blz0.Models.Dataset> Dataset { get; set; } = default!;
        public DbSet<QueryScaleOutSettings> QueryScaleOutSettings { get; set; }
        public DbSet<blz0.Models.Workspace> Workspace { get; set; } = default!;
        public DbSet<blz0.Models.Capacity> Capacity { get; set; } = default!;
        public DbSet<blz0.Models.Report> Report { get; set; } = default!;
        public DbSet<blz0.Models.Lakehouse> Lakehouse { get; set; } = default!;
        public DbSet<blz0.Models.Warehouse> Warehouse { get; set; } = default!;
        public DbSet<CacheRefreshState> CacheRefreshState { get; set; } = default!;
        public DbSet<UserPowerBiAccessCache> UserPowerBiAccessCache { get; set; } = default!;
        public DbSet<UserObjectMap> UserObjectMap { get; set; } = default!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<QueryScaleOutSettings>().HasNoKey();
            modelBuilder.Entity<CacheRefreshState>()
                .HasIndex(e => e.CacheKey)
                .IsUnique();
            modelBuilder.Entity<CacheRefreshState>()
                .HasIndex(e => new { e.UserObjectId, e.ObjectType, e.ScopeId })
                .IsUnique()
                .HasFilter(null);
            modelBuilder.Entity<CacheRefreshState>()
                .Property(e => e.ObjectType)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<CacheRefreshState>()
                .Property(e => e.UserObjectId)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<CacheRefreshState>()
                .Property(e => e.ScopeId)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<Capacity>()
                .Property(e => e.UserObjectId)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<Capacity>()
                .HasKey(e => new { e.UserObjectId, e.Id });
            modelBuilder.Entity<Workspace>()
                .Property(e => e.UserObjectId)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<Workspace>()
                .HasKey(e => new { e.UserObjectId, e.Id });
            modelBuilder.Entity<Dataset>()
                .Property(e => e.UserObjectId)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<Dataset>()
                .HasKey(e => new { e.UserObjectId, e.Id });
            modelBuilder.Entity<Report>()
                .Property(e => e.UserObjectId)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<Report>()
                .HasKey(e => new { e.UserObjectId, e.Id });
            modelBuilder.Entity<Lakehouse>()
                .Property(e => e.UserObjectId)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<Lakehouse>()
                .HasKey(e => new { e.UserObjectId, e.Id });
            modelBuilder.Entity<Warehouse>()
                .Property(e => e.UserObjectId)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<Warehouse>()
                .HasKey(e => new { e.UserObjectId, e.Id });
            modelBuilder.Entity<UserPowerBiAccessCache>()
                .HasIndex(e => new { e.UserKey, e.ObjectType, e.ObjectId, e.ScopeId })
                .IsUnique()
                .HasFilter(null);
            modelBuilder.Entity<UserObjectMap>()
                .Property(e => e.UserObjectId)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<UserObjectMap>()
                .Property(e => e.UserKey)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<UserObjectMap>()
                .Property(e => e.TenantId)
                .HasColumnType("nvarchar(450)");
            modelBuilder.Entity<UserObjectMap>()
                .HasIndex(e => e.UserObjectId)
                .IsUnique();
            modelBuilder.Entity<UserObjectMap>()
                .HasIndex(e => e.UserKey);
        }
    }
}
