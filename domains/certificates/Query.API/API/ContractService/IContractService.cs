using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.TokenValidation.Utilities;

namespace API.ContractService;

public interface IContractService
{
    Task<CreateContractResult> Create(CreateContracts contracts, UserDescriptor user, CancellationToken cancellationToken);
    Task<SetEndDateResult> SetEndDate(EditContracts contracts, UserDescriptor user, CancellationToken cancellationToken);
    Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken);
    Task<CertificateIssuingContract?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken);
}
