using System;
using System.Threading.Tasks;
using API.Shared.Services;
using Google.Protobuf.Collections;
using ProjectOrigin.WalletSystem.V1;

namespace API.Claiming.Automation.Services;

public class ProjectOriginWalletService : ProjectOriginService, IProjectOriginWalletService
{
    private readonly WalletService.WalletServiceClient walletServiceClient;

    public ProjectOriginWalletService(WalletService.WalletServiceClient walletServiceClient)
    {
        this.walletServiceClient = walletServiceClient;
    }

    public async Task<RepeatedField<GranularCertificate>> GetGranularCertificates(Guid subjectId)
    {
        var header = SetupDummyAuthorizationHeader(subjectId.ToString());
        var response = await walletServiceClient.QueryGranularCertificatesAsync(new QueryRequest(), header);
        return response.GranularCertificates;
    }

    public async Task ClaimCertificate(Guid ownerId, GranularCertificate consumptionCertificate, GranularCertificate productionCertificate, uint quantity)
    {
        var header = SetupDummyAuthorizationHeader(ownerId.ToString());
        var request = new ClaimRequest
        {
            ConsumptionCertificateId = consumptionCertificate.FederatedId,
            ProductionCertificateId = productionCertificate.FederatedId,
            Quantity = quantity
        };

        await walletServiceClient.ClaimCertificatesAsync(request, header);
    }
}
