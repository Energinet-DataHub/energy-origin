using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.MasterDataService.AuthService;
using API.MasterDataService.MockInput;
using Microsoft.Extensions.Logging;

namespace API.MasterDataService;

internal class MockMasterDataService : IMasterDataService
{
    private readonly AuthServiceClientFactory clientFactory;
    private readonly ILogger<MockMasterDataService> logger;
    private readonly Dictionary<string, MasterDataMockInput> mockInputs;
    private readonly ConcurrentDictionary<string, string> cvrToMeteringPointOwner = new();

    public MockMasterDataService(MasterDataMockInputCollection collection, AuthServiceClientFactory clientFactory, ILogger<MockMasterDataService> logger)
    {
        this.clientFactory = clientFactory;
        this.logger = logger;

        mockInputs = collection.Data.ToDictionary(d => d.GSRN, d => d);
    }

    public async Task<MasterData?> GetMasterData(string gsrn)
    {
        var mockInput = mockInputs.ContainsKey(gsrn) ? mockInputs[gsrn] : null;
        if (mockInput == null)
            return null;

        string meteringPointOwner;

        try
        {
            meteringPointOwner = await GetMeteringPointOwner(mockInput);
        }
        catch (Exception e)
        {
            logger.LogWarning("Exception {e}", e);
            return null;
        }

        return new MasterData(
            mockInput.GSRN,
            mockInput.GridArea,
            mockInput.Type,
            mockInput.Technology,
            meteringPointOwner,
            mockInput.MeteringPointOnboardedStartDate);
    }

    private async Task<string> GetMeteringPointOwner(MasterDataMockInput masterDataMockInput)
    {
        var cvr = masterDataMockInput.CVR;
        if (cvrToMeteringPointOwner.ContainsKey(cvr))
        {
            logger.LogInformation("Result from cache. {cvr} -> {meteringPointOwner}", cvr, cvrToMeteringPointOwner[cvr]);
            return cvrToMeteringPointOwner[cvr];
        }

        var authServiceClient = clientFactory.CreateClient();
        var meteringPointOwner = await authServiceClient.GetUuid(cvr);

        cvrToMeteringPointOwner.TryAdd(cvr, meteringPointOwner);

        logger.LogInformation("Result from auth service. {cvr} -> {meteringPointOwner}", cvr, cvrToMeteringPointOwner[cvr]);

        return meteringPointOwner;
    }
}
