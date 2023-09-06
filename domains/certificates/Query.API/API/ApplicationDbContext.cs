using API.ContractService;
using API.DataSyncSyncer.Persistence;
using CertificateValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace API;

//TODO: What about location of migration scripts?
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CertificateIssuingContract>().HasIndex(c => new { c.GSRN, c.ContractNumber }).IsUnique();

        modelBuilder.Entity<ProductionCertificate>().OwnsOne(c => c.Technology);
        modelBuilder.Entity<ProductionCertificate>().HasIndex(c => new { c.Gsrn, c.DateFrom, c.DateTo }).IsUnique();

        modelBuilder.Entity<SynchronizationPosition>().HasKey(s => s.GSRN);
    }

    public DbSet<CertificateIssuingContract> Contracts { get; set; }
    public DbSet<ProductionCertificate> ProductionCertificates { get; set; }
    public DbSet<SynchronizationPosition> SynchronizationPositions { get; set; }
}

public interface IProductionCertificateRepository
{
    Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default);
    Task<ProductionCertificate?> Get(Guid id, CancellationToken cancellationToken = default);
}

public class ProductionCertificateRepository : IProductionCertificateRepository
{
    private readonly ApplicationDbContext dbContext;

    public ProductionCertificateRepository(ApplicationDbContext dbContext) => this.dbContext = dbContext;

    public Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default)
    {
        dbContext.Update(productionCertificate);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ProductionCertificate?> Get(Guid id, CancellationToken cancellationToken = default)
        => dbContext.ProductionCertificates.FindAsync(new object?[] { id }, cancellationToken).AsTask();
}

public class ProductionCertificate
{
    private ProductionCertificate()
    {
    }

    public ProductionCertificate(string gridArea, Period period, Technology technology, string meteringPointOwner, string gsrn, long quantity, byte[] blindingValue)
    {
        IssuedState = IssuedState.Creating;
        GridArea = gridArea;
        DateFrom = period.DateFrom;
        DateTo = period.DateTo;
        Technology = technology;
        MeteringPointOwner = meteringPointOwner;
        Gsrn = gsrn;
        Quantity = quantity;
        BlindingValue = blindingValue;
    }

    public Guid Id { get; private set; }

    public IssuedState IssuedState { get; private set; }
    public string GridArea { get; private set; } = "";
    public long DateFrom { get; private set; }
    public long DateTo { get; private set; }
    public Technology Technology { get; private set; } = new("unknown", "unknown");
    public string MeteringPointOwner { get; private set; } = "";
    public string Gsrn { get; private set; } = "";
    public long Quantity { get; private set; } //TODO: long?
    public byte[] BlindingValue { get; private set; } = Array.Empty<byte>();

    public string? RejectionReason { get; private set; }
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
