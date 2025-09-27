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
        public DbSet<Skill> Skills { get; set; }
        public DbSet<UserSkill> UserSkills { get; set; }
        public DbSet<SkillField> SkillFields { get; set; }
        public DbSet<SkillFieldValue> SkillFieldValues { get; set; }
        public DbSet<SkillFieldChoice> SkillFieldChoices { get; set; }
        public DbSet<SkillFieldValueChoice> SkillFieldValueChoices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<Skill>(entity =>
            {
                entity.HasIndex(e => e.MachineName).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<UserSkill>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.SkillId }).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserSkills)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Skill)
                    .WithMany(s => s.UserSkills)
                    .HasForeignKey(e => e.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SkillField>(entity =>
            {
                entity.HasIndex(e => new { e.SkillId, e.MachineName }).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.FieldType).HasConversion<int>();

                entity.HasOne(e => e.Skill)
                    .WithMany(s => s.SkillFields)
                    .HasForeignKey(e => e.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SkillFieldValue>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.SkillField)
                    .WithMany(sf => sf.SkillFieldValues)
                    .HasForeignKey(e => e.SkillFieldId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UserSkill)
                    .WithMany(us => us.SkillFieldValues)
                    .HasForeignKey(e => e.UserSkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SkillFieldChoice>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.SkillField)
                    .WithMany(e => e.SkillFieldChoices)
                    .HasForeignKey(e => e.SkillFieldId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SkillFieldValueChoice>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.SkillFieldValue)
                    .WithMany(e => e.SkillFieldValueChoices)
                    .HasForeignKey(e => e.SkillFieldValueId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SkillFieldChoice)
                    .WithMany()
                    .HasForeignKey(e => e.SkillFieldChoiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}