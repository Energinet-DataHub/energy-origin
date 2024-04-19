using DataContext;
using DataContext.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace API.ContractService.Repositories;

internal class CertificateIssuingContractRepository : ICertificateIssuingContractRepository
{
    private readonly ApplicationDbContext dbContext;

    public CertificateIssuingContractRepository(ApplicationDbContext dbContext)
        => this.dbContext = dbContext;

    public ValueTask<EntityEntry<CertificateIssuingContract>> Save(
        CertificateIssuingContract certificateIssuingContract)
    {
        return dbContext.AddAsync(certificateIssuingContract);
    }

    public void Update(CertificateIssuingContract certificateIssuingContract)
    {
        dbContext.Update(certificateIssuingContract);
    }

    public void UpdateRange(List<CertificateIssuingContract> certificateIssuingContracts)
    {
        dbContext.UpdateRange(certificateIssuingContracts);
    }

    public async Task<IReadOnlyList<CertificateIssuingContract>> GetByGsrn(string gsrn, CancellationToken cancellationToken) =>
        await dbContext.Contracts
            .Where(c => c.GSRN == gsrn)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CertificateIssuingContract>> GetByGsrn(List<string> gsrn, CancellationToken cancellationToken) =>
        await dbContext.Contracts
            .Where(c => gsrn.Contains(c.GSRN))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CertificateIssuingContract>> GetAllMeteringPointOwnerContracts(
        string meteringPointOwner, CancellationToken cancellationToken) =>
        await dbContext.Contracts
            .Where(x => x.MeteringPointOwner == meteringPointOwner)
            .ToListAsync(cancellationToken);

    public Task<CertificateIssuingContract?> GetById(Guid id, CancellationToken cancellationToken) =>
        dbContext.Contracts.FindAsync(new object?[] { id }, cancellationToken).AsTask();

    public Task<List<CertificateIssuingContract>> GetAllByIds(List<Guid> ids, CancellationToken cancellationToken) =>
        dbContext.Contracts.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
}
