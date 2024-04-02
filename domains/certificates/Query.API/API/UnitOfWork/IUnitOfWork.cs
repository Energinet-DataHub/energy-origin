using API.ContractService.Repositories;
using DataContext;
using EnergyOrigin.ActivityLog.API;
using System.Threading.Tasks;

namespace API.UnitOfWork;

public interface IUnitOfWork
{
    ICertificateIssuingContractRepository CertificateIssuingContractRepo { get; }
    IActivityLogEntryRepository ActivityLogEntryRepo { get; }

    Task SaveAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private ApplicationDbContext context;
    private IActivityLogEntryRepository activityLogEntryRepo = null!;
    private ICertificateIssuingContractRepository certificateIssuingContractRepo = null!;

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
}
