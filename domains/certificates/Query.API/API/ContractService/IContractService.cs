using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using DataContext.Models;

namespace API.ContractService;

public interface IContractService
{
    Task<CreateContractResult> Create(CreateContracts contracts, Guid meteringPointOwnerId, Guid subjectId, string subjectName, string organizationName,
        string organizationTin, bool isTrial, CancellationToken cancellationToken);

    Task<SetEndDateResult> SetEndDate(EditContracts contracts, Guid meteringPointOwnerId, Guid subjectId, string subjectName, string organizationName,
        string organizationTin, CancellationToken cancellationToken);

    Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(Guid meteringPointOwnerId, CancellationToken cancellationToken);
    Task<CertificateIssuingContract?> GetById(Guid id, IList<Guid> authorizedMeteringPointOwnerIds, CancellationToken cancellationToken);
}
