using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class TransferAgreementAuditRepository : ITransferAgreementAuditRepository
    {
        private readonly ApplicationDbContext context;

        public TransferAgreementAuditRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<List<TransferAgreementAudit>> GetAuditsForTransferAgreement(Guid transferAgreementId, string subject, string tin)
        {
            return await context.TransferAgreementAudits
                .Where(audit =>
                    audit.TransferAgreement.Id == transferAgreementId &&
                    (audit.TransferAgreement.SenderId == Guid.Parse(subject) || audit.TransferAgreement.ReceiverTin.Equals(tin)))
                .ToListAsync();
        }

    }
}
