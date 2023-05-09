using API.ApiModels;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, DbSet<TransferAgreement> transferAgreements, DbSet<Subject> subjects)
            : base(options)
        {
            TransferAgreements = transferAgreements;
            Subjects = subjects;
        }

        public DbSet<TransferAgreement> TransferAgreements { get; set; }
        public DbSet<Subject> Subjects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subject>().HasKey(x => x.Id);
            modelBuilder.Entity<Subject>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<TransferAgreement>().HasKey(x => x.Id);
            modelBuilder.Entity<TransferAgreement>().Property(x => x.Id).ValueGeneratedNever();
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
}
