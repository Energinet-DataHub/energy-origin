using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;

namespace API.Services
{
    public interface ITransferAgreementService
    {
        Task<TransferAgreementResponseModel> CreateTransferAgreementAsync(TransferAgreementRequestModel request);
        Task<TransferAgreementResponseModel> UpdateTransferAgreementAsync(Guid id, TransferAgreementChangeRequestModel changeRequest);
        Task<IEnumerable<TransferAgreementResponseModel>> GetAllTransferAgreementsAsync(Guid participantId);
        Task<TransferAgreementResponseModel> GetTransferAgreementByIdAsync(Guid id, Guid participantId);
    }
}
