using API.ContractService;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Threading;
using System;
using CertificateValueObjects;

namespace API;

//TODO: Should this be in a shared lib?
//TODO: What about location of migration scripts?
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CertificateIssuingContract>()
            .HasIndex(c => new { c.GSRN, c.ContractNumber })
            .IsUnique();

        modelBuilder.Entity<ProductionCertificate>()
            .OwnsOne(c => c.Period);
        modelBuilder.Entity<ProductionCertificate>()
            .OwnsOne(c => c.Technology);
    }

    public DbSet<CertificateIssuingContract> Contracts { get; set; }
    public DbSet<ProductionCertificate> ProductionCertificates { get; set; }
}

public interface IProductionCertificateRepository
{
    Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default);
    Task<ProductionCertificate?> Get(Guid id, int? version = null, CancellationToken cancellationToken = default);
}

class ProductionCertificateRepository : IProductionCertificateRepository
{
    private readonly ApplicationDbContext dbContext;

    public ProductionCertificateRepository(ApplicationDbContext dbContext) => this.dbContext = dbContext;

    public Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default)
    {
        dbContext.Update(productionCertificate);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ProductionCertificate?> Get(Guid id, int? version = null, CancellationToken cancellationToken = default)
        => dbContext.ProductionCertificates.FindAsync(id, cancellationToken).AsTask();
}

public class ProductionCertificate //TODO: Or just "Certificate"?
{
    public ProductionCertificate()
    {

    }

    public ProductionCertificate(string gridArea, Period period, Technology technology, string meteringPointOwner, string gsrn, long quantity, byte[] blindingValue)
    {
        IssuedState = IssuedState.Creating;
        GridArea = gridArea;
        Period = period;
        Technology = technology;
        MeteringPointOwner = meteringPointOwner;
        Gsrn = gsrn;
        Quantity = quantity;
        BlindingValue = blindingValue;
    }

    public Guid Id { get; set; }

    public IssuedState IssuedState { get; set; }
    public string GridArea { get; set; }
    public Period Period { get; set; }
    public Technology Technology { get; set; }
    public string MeteringPointOwner { get; set; }
    public string Gsrn { get; set; } //TODO: Use value type
    public long Quantity { get; set; } //TODO: long?
    public byte[] BlindingValue { get; set; }

    public string? RejectionReason { get; set; }
    //public byte[] WalletPublicKey { get; set; }
    //public string WalletUrl { get; set; }
    //public uint WalletDepositEndpointPosition { get; set; } //todo: Should this be saved...?

    public bool IsRejected => IssuedState == IssuedState.Rejected;
    public bool IsIssued => IssuedState == IssuedState.Issued;

    public void Reject(string reason)
    {
        IssuedState = IssuedState.Rejected;
        RejectionReason = reason;
    }

    public void Issue()
    {
        IssuedState = IssuedState.Issued;
    }
}

public enum IssuedState
{
    Creating = 1,
    Issued = 2,
    Rejected = 3
}
