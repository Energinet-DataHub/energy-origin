using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.Domain.ValueObjects;

namespace TransferAgreementAutomation.Worker.Service.TransactionStatus
{
    public class InMemoryRequestStatusRepository : IRequestStatusRepository
    {
        private readonly Dictionary<Guid, RequestStatus> _requestStatusMap = new Dictionary<Guid, RequestStatus>();

        public Task Add(RequestStatus requestStatus, CancellationToken cancellationToken = default)
        {
            _requestStatusMap[requestStatus.Id] = requestStatus;
            return Task.CompletedTask;
        }

        public Task<RequestStatus> Get(Guid id, CancellationToken cancellationToken = default)
        {
            if (_requestStatusMap.TryGetValue(id, out var requestStatus))
            {
                return Task.FromResult(requestStatus);
            }

            throw new Exception($"Unknown RequestStatus with id {id}");
        }

        public Task Update(RequestStatus requestStatus, CancellationToken cancellationToken = default)
        {
            if (_requestStatusMap.Remove(requestStatus.Id))
            {
                _requestStatusMap.Add(requestStatus.Id, requestStatus);
                return Task.CompletedTask;
            }

            throw new Exception($"Unknown RequestStatus with id {requestStatus.Id}");
        }

        public Task Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var removed = _requestStatusMap.Remove(id);
            if (!removed)
            {
                throw new Exception($"Unknown RequestStatus with id {id}");
            }

            return Task.CompletedTask;
        }

        public Task<IList<RequestStatus>> GetByOrganization(OrganizationId organizationId, CancellationToken cancellationToken = default)
        {
            IList<RequestStatus> result = _requestStatusMap.Values
                .Where(rq => rq.SenderId == organizationId || rq.ReceiverId == organizationId)
                .ToList();

            return Task.FromResult(result);
        }
    }
}
