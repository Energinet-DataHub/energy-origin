using System;
using System.Threading.Tasks;
using API.ContractService.Repositories;
using API.Transfer.Api.Repository;
using DataContext;
using EnergyOrigin.ActivityLog.API;

namespace API.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    ITransferAgreementRepository TransferAgreementRepo { get; }
    IActivityLogEntryRepository ActivityLogEntryRepo { get; }
    ITransferAgreementProposalRepository TransferAgreementProposalRepo { get; }
    ITransferAgreementHistoryEntryRepository TransferAgreementHistoryEntryRepo { get; }
    ICertificateIssuingContractRepository CertificateIssuingContractRepo { get; }
    Task SaveAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private TransferDbContext transferDbContext;
    private ITransferAgreementRepository transferAgreementRepo = null!;
    private IActivityLogEntryRepository activityLogEntryRepo = null!;
    private ITransferAgreementProposalRepository transferAgreementProposalRepo = null!;
    private ITransferAgreementHistoryEntryRepository transferAgreementHistoryEntryRepo = null!;
    private CertificateDbContext certificateDbContext;
    private ICertificateIssuingContractRepository certificateIssuingContractRepo = null!;

    public UnitOfWork(TransferDbContext transferDbContext, CertificateDbContext certificateDbContext)
    {
        this.transferDbContext = transferDbContext;
        this.certificateDbContext = certificateDbContext;
    }

    public ITransferAgreementRepository TransferAgreementRepo
    {
        get => transferAgreementRepo ??= new TransferAgreementRepository(transferDbContext);
    }

    public IActivityLogEntryRepository ActivityLogEntryRepo
    {
        get => activityLogEntryRepo ??= new ActivityLogEntryRepository(transferDbContext);
    }

    public ITransferAgreementProposalRepository TransferAgreementProposalRepo
    {
        get => transferAgreementProposalRepo ??= new TransferAgreementProposalRepository(transferDbContext);
    }

    public ITransferAgreementHistoryEntryRepository TransferAgreementHistoryEntryRepo
    {
        get => transferAgreementHistoryEntryRepo ??= new TransferAgreementHistoryEntryRepository(transferDbContext);
    }

    public ICertificateIssuingContractRepository CertificateIssuingContractRepo
    {
        get
        {
            return certificateIssuingContractRepo ??= new CertificateIssuingContractRepository(certificateDbContext);
        }
    }

    public Task SaveAsync()
    {
        return transferDbContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await transferDbContext.DisposeAsync();
    }
}
