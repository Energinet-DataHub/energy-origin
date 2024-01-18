using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectOrigin.WalletSystem.V1;

namespace ClaimAutomation.Worker.Automation.Services;

public interface IProjectOriginWalletService
{
    Task<List<GranularCertificate>> GetGranularCertificates(Guid subjectId);
    Task ClaimCertificates(Guid ownerId, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity);
}
