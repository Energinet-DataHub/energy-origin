using System;
using API.ContractService.Repositories;
using DataContext;
using EnergyOrigin.ActivityLog.API;
using System.Threading.Tasks;

namespace API.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    ICertificateIssuingContractRepository CertificateIssuingContractRepo { get; }
    IWalletRepository WalletRepo { get; }
    IActivityLogEntryRepository ActivityLogEntryRepo { get; }

    Task SaveAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private ApplicationDbContext context;
    private IActivityLogEntryRepository activityLogEntryRepo = null!;
    private ICertificateIssuingContractRepository certificateIssuingContractRepo = null!;
    private IWalletRepository walletRepo = null!;

    public UnitOfWork(ApplicationDbContext context)
    {
        this.context = context;
    }

    public ICertificateIssuingContractRepository CertificateIssuingContractRepo
    {
        get
        {
            return certificateIssuingContractRepo ??= new CertificateIssuingContractRepository(context);
        }
    }

    public IWalletRepository WalletRepo
    {
        get
        {
            return walletRepo ??= new WalletRepository(context);
        }
    }

    public IActivityLogEntryRepository ActivityLogEntryRepo
    {
        get
        {
            return activityLogEntryRepo ??= new ActivityLogEntryRepository(context);
        }
    }

    public Task SaveAsync()
    {
        return context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
    }
}
