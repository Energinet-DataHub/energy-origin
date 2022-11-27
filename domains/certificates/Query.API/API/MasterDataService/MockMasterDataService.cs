using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.MasterDataService.AuthService;
using Microsoft.Extensions.Logging;

namespace API.MasterDataService;

internal class MockMasterDataService : IMasterDataService
{
    private readonly AuthServiceClientFactory clientFactory;
    private readonly ILogger<MockMasterDataService> logger;
    private readonly Dictionary<string, MockMasterData> data;

    public MockMasterDataService(MockMasterDataCollection collection, AuthServiceClientFactory clientFactory, ILogger<MockMasterDataService> logger)
    {
        this.clientFactory = clientFactory;
        this.logger = logger;

        data = collection.Data.ToDictionary(d => d.GSRN, d => d);
    }

    public async Task<MasterData?> GetMasterData(string gsrn)
    {
        var mockMasterData = data.ContainsKey(gsrn) ? data[gsrn] : null;
        if (mockMasterData == null)
            return null;

        string meteringPointOwner;

        try
        {
            meteringPointOwner = await GetMeteringPointOwner(mockMasterData);
        }
        catch (Exception e)
        {
            logger.LogWarning("Exception {e}", e);
            return null;
        }

        return new MasterData(mockMasterData.GSRN, mockMasterData.GridArea, mockMasterData.Type,
            mockMasterData.Technology, meteringPointOwner, mockMasterData.MeteringPointOnboardedStartDate);
    }

    private async Task<string> GetMeteringPointOwner(MockMasterData mockMasterData)
    {
        var authServiceClient = clientFactory.CreateClient();
        return await authServiceClient.GetUuid(mockMasterData.CVR);
    }
}
