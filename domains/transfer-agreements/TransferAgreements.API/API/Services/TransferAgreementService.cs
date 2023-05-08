using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using API.ApiModels;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using Marten;

namespace API.Services
{
    public class TransferAgreementService : ITransferAgreementService
    {
        private readonly IDocumentStore store;

        public TransferAgreementService(IDocumentStore store)
        {
            this.store = store;
        }

        public async Task<TransferAgreementResponseModel> CreateTransferAgreementAsync(TransferAgreementRequestModel request)
        {
            using var session = store.LightweightSession();

            var transferAgreement = new TransferAgreement
            {
                Id = Guid.NewGuid(),
                ProviderId = request.ProviderId,
                ReceiverId = request.ReceiverId,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            session.Store(transferAgreement);
            await session.SaveChangesAsync();

            return await GetByIdWithParticipantsAsync(transferAgreement.Id);
        }

        public async Task<TransferAgreementResponseModel> UpdateTransferAgreementAsync(Guid id, TransferAgreementChangeRequestModel changeRequest)
        {
            using var session = store.LightweightSession();

            var transferAgreement = await session.LoadAsync<TransferAgreement>(id);

            if (changeRequest.StartDate.HasValue)
            {
                transferAgreement.StartDate = changeRequest.StartDate.Value;
            }
            if (changeRequest.EndDate.HasValue)
            {
                transferAgreement.EndDate = changeRequest.EndDate.Value;
            }

            session.Update(transferAgreement);
            await session.SaveChangesAsync();

            return await GetByIdWithParticipantsAsync(id);
        }

        public async Task<IEnumerable<TransferAgreementResponseModel>> GetAllTransferAgreementsAsync(Guid participantId)
        {
            using var session = store.QuerySession();

            return await session.Query<TransferAgreement>()
                .Where(ta => ta.ProviderId == participantId || ta.ReceiverId == participantId)
                .Select(ta => new TransferAgreementResponseModel
                {
                    Id = ta.Id,
                    ProviderId = ta.ProviderId,
                    ReceiverId = ta.ReceiverId,
                    StartDate = ta.StartDate,
                    EndDate = ta.EndDate
                })
                .ToListAsync();
        }

        public async Task<TransferAgreementResponseModel> GetTransferAgreementByIdAsync(Guid id, Guid participantId)
        {
            using var session = store.QuerySession();

            var transferAgreement = await session.Query<TransferAgreement>()
                .FirstOrDefaultAsync(ta => ta.Id == id && (ta.ProviderId == participantId || ta.ReceiverId == participantId));

            if (transferAgreement == null)
            {
                return null;
            }

            var provider = await session.LoadAsync<Participant>(transferAgreement.ProviderId);
            var receiver = await session.LoadAsync<Participant>(transferAgreement.ReceiverId);

            return new TransferAgreementResponseModel
            {
                Id = transferAgreement.Id,
                ProviderName = provider.Name,
                ProviderTin = provider.Tin,
                ReceiverName = receiver.Name,
                ReceiverTin = receiver.Tin,
                StartDate = transferAgreement.StartDate,
                EndDate = transferAgreement.EndDate
            };
        }

        private async Task<TransferAgreementResponseModel> GetByIdWithParticipantsAsync(Guid id)
        {
            using var session = store.QuerySession();

            var transferAgreement = await session.LoadAsync<TransferAgreement>(id);

            var provider = await session.LoadAsync<Participant>(transferAgreement.ProviderId);
            var receiver = await session.LoadAsync<Participant>(transferAgreement.ReceiverId);

            return new TransferAgreementResponseModel
            {
                Id = transferAgreement.Id,
                ProviderName = provider.Name,
                ProviderTin = provider.Tin,
                ReceiverName = receiver.Name,
                ReceiverTin = receiver.Tin,
                StartDate = transferAgreement.StartDate,
                EndDate = transferAgreement.EndDate
            };
        }

        public async Task<bool> DeleteTransferAgreementAsync(Guid id)
        {
            using var session = store.LightweightSession();

            var transferAgreement = await session.LoadAsync<TransferAgreement>(id);

            if (transferAgreement == null)
            {
                return false;
            }

            session.Delete(transferAgreement);
            await session.SaveChangesAsync();

            return true;
        }

        public async Task<TransferAgreementResponseModel> CreateTransferAgreementWithParticipantsAsync(TransferAgreementRequestModel request)
        {
            using var session = store.LightweightSession();

            var provider = await session.LoadAsync<Participant>(request.ProviderId);
            var receiver = await session.LoadAsync<Participant>(request.ReceiverId);

            if (provider == null || receiver == null)
            {
                return null;
            }

            var transferAgreement = new TransferAgreement
            {
                Id = Guid.NewGuid(),
                ProviderId = request.ProviderId,
                ReceiverId = request.ReceiverId,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            session.Store(transferAgreement);
            await session.SaveChangesAsync();

            return await GetByIdWithParticipantsAsync(transferAgreement.Id);
        }

        public async Task<TransferAgreementResponseModel> UpdateTransferAgreementWithParticipantsAsync(Guid id, TransferAgreementChangeRequestModel changeRequest)
        {
            using var session = store.LightweightSession();

            var transferAgreement = await session.LoadAsync<TransferAgreement>(id);

            if (transferAgreement == null)
            {
                return null;
            }

            if (changeRequest.StartDate.HasValue)
            {
                transferAgreement.StartDate = changeRequest.StartDate.Value;
            }
            if (changeRequest.EndDate.HasValue)
            {
                transferAgreement.EndDate = changeRequest.EndDate.Value;
            }

            var provider = await session.LoadAsync<Participant>(transferAgreement.ProviderId);
            var receiver = await session.LoadAsync<Participant>(transferAgreement.ReceiverId);

            if (provider == null || receiver == null)
            {
                return null;
            }

            session.Update(transferAgreement);
            await session.SaveChangesAsync();

            return await GetByIdWithParticipantsAsync(id);
        }
    }
}
