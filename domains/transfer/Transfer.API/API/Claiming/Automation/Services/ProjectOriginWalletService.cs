using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectOrigin.WalletSystem.V1;

namespace API.Claiming.Automation.Services;

public class ProjectOriginWalletService : Shared.Services.ProjectOriginWalletService, IProjectOriginWalletService
{
    private readonly WalletService.WalletServiceClient walletServiceClient;

    public ProjectOriginWalletService(WalletService.WalletServiceClient walletServiceClient)
    {
        this.walletServiceClient = walletServiceClient;
    }

    public async Task<List<GranularCertificate>> GetGranularCertificates(Guid subjectId)
    {
        var header = SetupDummyAuthorizationHeader(subjectId.ToString());
        var response = await walletServiceClient.QueryGranularCertificatesAsync(new QueryRequest(), header);
        return response.GranularCertificates.ToList();
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
