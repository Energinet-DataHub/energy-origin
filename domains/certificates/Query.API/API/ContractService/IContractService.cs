using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.TokenValidation.Utilities;

namespace API.ContractService;

public interface IContractService
{
    Task<CreateContractResult> Create(List<(string gsrn, UnixTimestamp startDate, UnixTimestamp? endDate)> contracts, UserDescriptor user, CancellationToken cancellationToken);
    Task<SetEndDateResult> SetEndDate(List<(Guid id, UnixTimestamp? newEndDate)> contracts, UserDescriptor user, CancellationToken cancellationToken);
    Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken);
    Task<CertificateIssuingContract?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken);
}
