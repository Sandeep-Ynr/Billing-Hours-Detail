using Microsoft.EntityFrameworkCore;
using BillingSoftware.Models;

namespace BillingSoftware.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<WorkTask> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Client entity
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
            });

            // Configure WorkTask entity
            modelBuilder.Entity<WorkTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.TaskLink).HasMaxLength(500);
                entity.Property(e => e.HoursWorked).HasColumnType("decimal(18,2)");
                
                entity.HasOne(e => e.Client)
                      .WithMany(c => c.Tasks)
                      .HasForeignKey(e => e.ClientId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed some sample data
            modelBuilder.Entity<Client>().HasData(
                new Client { Id = 1, Name = "Tech Solutions Inc.", HourlyRate = 75.00m, Email = "contact@techsolutions.com", Description = "Software development client", IsActive = true, CreatedAt = DateTime.Now },
                new Client { Id = 2, Name = "Digital Marketing Pro", HourlyRate = 50.00m, Email = "info@digitalmarketingpro.com", Description = "Marketing automation project", IsActive = true, CreatedAt = DateTime.Now },
                new Client { Id = 3, Name = "StartUp Ventures", HourlyRate = 100.00m, Email = "team@startupventures.io", Description = "MVP development", IsActive = true, CreatedAt = DateTime.Now }
            );
        }
    }
}
