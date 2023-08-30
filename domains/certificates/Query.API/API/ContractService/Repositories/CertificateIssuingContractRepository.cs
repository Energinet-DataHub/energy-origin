using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//using Marten;

namespace API.ContractService.Repositories;

internal class CertificateIssuingContractRepository : ICertificateIssuingContractRepository
{
    private readonly ApplicationDbContext dbContext;

    public CertificateIssuingContractRepository(ApplicationDbContext dbContext)
        => this.dbContext = dbContext;

    public Task Save(CertificateIssuingContract certificateIssuingContract)
    {
        dbContext.Add(certificateIssuingContract);
        return dbContext.SaveChangesAsync();
        //session.Insert(certificateIssuingContract);
        //return session.SaveChangesAsync();
    }

    public Task Update(CertificateIssuingContract certificateIssuingContract)
    {
        dbContext.Update(certificateIssuingContract);
        return dbContext.SaveChangesAsync();
        //session.Update(certificateIssuingContract);
        //return session.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<CertificateIssuingContract>> GetByGsrn(string gsrn, CancellationToken cancellationToken) =>
        await dbContext.Contracts
            .Where(c => c.GSRN == gsrn)
            .ToListAsync(cancellationToken);

    //session
        //.Query<CertificateIssuingContract>()
        //.Where(x => x.GSRN == gsrn)
        //.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CertificateIssuingContract>> GetAllMeteringPointOwnerContracts(
        string meteringPointOwner, CancellationToken cancellationToken) =>
        await dbContext.Contracts
            .Where(x => x.MeteringPointOwner == meteringPointOwner)
            .ToListAsync(cancellationToken);

    //session
    //.Query<CertificateIssuingContract>()
    //.Where(x => x.MeteringPointOwner == meteringPointOwner)
    //.ToListAsync(cancellationToken);

    public Task<CertificateIssuingContract?> GetById(Guid id, CancellationToken cancellationToken) =>
        dbContext.Contracts.FindAsync(id, cancellationToken).AsTask();


    //session
    //.LoadAsync<CertificateIssuingContract>(id, cancellationToken);
}
