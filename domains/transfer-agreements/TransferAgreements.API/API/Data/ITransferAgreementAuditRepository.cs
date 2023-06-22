using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Data;

public interface ITransferAgreementAuditRepository
{
    Task<List<TransferAgreementAudit>> GetAuditsForTransferAgreement(Guid transferAgreementId, string subject, string tin);

}
