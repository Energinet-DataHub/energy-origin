using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;

namespace API.ContractService.Repositories;

public interface ICertificateIssuingContractRepository
{
    Task SaveRange(List<CertificateIssuingContract> certificateIssuingContracts);
    void UpdateRange(List<CertificateIssuingContract> certificateIssuingContracts);
    void Update(CertificateIssuingContract certificateIssuingContract);
    void RemoveRange(IEnumerable<CertificateIssuingContract> certificateIssuingContracts);
    Task<IReadOnlyList<CertificateIssuingContract>> GetByGsrn(List<string> gsrn, CancellationToken cancellationToken);
    Task<IReadOnlyList<CertificateIssuingContract>> GetAllMeteringPointOwnerContracts(string meteringPointOwner, CancellationToken cancellationToken);
    Task<CertificateIssuingContract?> GetById(Guid id, CancellationToken cancellationToken);
    Task<List<CertificateIssuingContract>> GetAllByIds(List<Guid> ids, CancellationToken cancellationToken);
    IQueryable<CertificateIssuingContract> Query();
}
