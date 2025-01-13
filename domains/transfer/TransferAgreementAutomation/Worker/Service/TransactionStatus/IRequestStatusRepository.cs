using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.Domain.ValueObjects;

namespace TransferAgreementAutomation.Worker.Service.TransactionStatus;

public interface IRequestStatusRepository
{
    public Task Add(RequestStatus requestStatus, CancellationToken cancellationToken = default);

    public Task<RequestStatus> Get(Guid id, CancellationToken cancellationToken = default);

    public Task Update(RequestStatus requestStatus, CancellationToken cancellationToken = default);

    public Task Delete(Guid id, CancellationToken cancellationToken = default);

    public Task<IList<RequestStatus>> GetByOrganization(OrganizationId organizationId, CancellationToken cancellationToken = default);
}
