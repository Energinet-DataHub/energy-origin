using API.Api.ApiModels;
using Microsoft.EntityFrameworkCore;

namespace API.Infrastructure.Data;

public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TransferAgreement> TransferAgreements { get; set; }
        public DbSet<Subject> Subjects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subject>().HasKey(x => x.Id);
            modelBuilder.Entity<Subject>().Property(x => x.Id);
            modelBuilder.Entity<Subject>().HasIndex(x => x.Tin).IsUnique();
            modelBuilder.Entity<Subject>().HasIndex(x => x.Name).IsUnique();
            modelBuilder.Entity<TransferAgreement>().HasKey(x => x.Id);
            modelBuilder.Entity<TransferAgreement>().Property(x => x.Id);
            modelBuilder.Entity<TransferAgreement>()
                .HasOne(x => x.Sender)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TransferAgreement>()
                .HasOne(x => x.Receiver)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);


        }
    }
