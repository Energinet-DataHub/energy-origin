using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.ApiModels.Responses;

namespace API.Data;

public interface ITransferAgreementRepository
{
    Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement);
    Task<List<TransferAgreementResponse>> GetTransferAgreementsBySubjectId(Guid subjectId);
}
