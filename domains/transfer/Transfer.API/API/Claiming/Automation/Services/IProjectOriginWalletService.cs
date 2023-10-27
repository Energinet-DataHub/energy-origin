using Google.Protobuf.Collections;
using ProjectOrigin.WalletSystem.V1;
using System.Threading.Tasks;
using System;

namespace API.Claiming.Automation.Services;

public interface IProjectOriginWalletService
{
    Task<RepeatedField<GranularCertificate>> GetGranularCertificates(Guid subjectId);
    Task ClaimCertificate(Guid ownerId, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity);
}
