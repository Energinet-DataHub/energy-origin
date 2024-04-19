using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace API.ContractService.Repositories;

public interface ICertificateIssuingContractRepository
{
    ValueTask<EntityEntry<CertificateIssuingContract>> Save(CertificateIssuingContract certificateIssuingContract);
    void Update(CertificateIssuingContract certificateIssuingContract);
    void UpdateRange(List<CertificateIssuingContract> certificateIssuingContracts);
    Task<IReadOnlyList<CertificateIssuingContract>> GetByGsrn(string gsrn, CancellationToken cancellationToken);
    Task<IReadOnlyList<CertificateIssuingContract>> GetByGsrn(List<string> gsrn, CancellationToken cancellationToken);
    Task<IReadOnlyList<CertificateIssuingContract>> GetAllMeteringPointOwnerContracts(string meteringPointOwner, CancellationToken cancellationToken);
    Task<CertificateIssuingContract?> GetById(Guid id, CancellationToken cancellationToken);
    Task<List<CertificateIssuingContract>> GetAllByIds(List<Guid> ids, CancellationToken cancellationToken);
}
