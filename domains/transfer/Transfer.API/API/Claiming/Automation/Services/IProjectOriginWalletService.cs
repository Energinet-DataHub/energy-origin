using ProjectOrigin.WalletSystem.V1;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace API.Claiming.Automation.Services;

public interface IProjectOriginWalletService
{
    Task<List<GranularCertificate>> GetGranularCertificates(Guid subjectId);
    Task ClaimCertificate(Guid ownerId, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity);
}
