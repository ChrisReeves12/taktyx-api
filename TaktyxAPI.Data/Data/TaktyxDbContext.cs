using Microsoft.EntityFrameworkCore;
using TaktyxAPI.Data.Entities;

namespace TaktyxAPI.Data.Data
{
    public class TaktyxDbContext : DbContext
    {
        public TaktyxDbContext(DbContextOptions<TaktyxDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) 
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity => {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}