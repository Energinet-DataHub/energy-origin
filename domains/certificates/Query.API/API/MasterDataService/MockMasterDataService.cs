using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.MasterDataService.Clients;
using API.MasterDataService.MockInput;
using Microsoft.Extensions.Logging;

namespace API.MasterDataService;

internal class MockMasterDataService : IMasterDataService
{
    private readonly AuthServiceClientFactory clientFactoryChange;
    private readonly ILogger<MockMasterDataService> logger;
    private readonly Dictionary<string, MasterDataMockInput> mockInputs;
    private readonly ConcurrentDictionary<string, string> cvrToMeteringPointOwner = new();

    public MockMasterDataService(
        MasterDataMockInputCollection collection,
        AuthServiceClientFactory clientFactoryChange,
        ILogger<MockMasterDataService> logger)
    {
        this.clientFactoryChange = clientFactoryChange;
        this.logger = logger;

        mockInputs = collection.Inputs.ToDictionary(d => d.GSRN, d => d);
    }

    public async Task<MasterData?> GetMasterData(string gsrn)
    {
        var mockInput = mockInputs.ContainsKey(gsrn) ? mockInputs[gsrn] : null;
        if (mockInput == null)
            return null;

        var meteringPointOwner = await GetMeteringPointOwner(mockInput);

        if (string.IsNullOrWhiteSpace(meteringPointOwner))
        {
            logger.LogInformation("Could not determine meteringPointOwner for {input}", mockInput);
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
            return cvrToMeteringPointOwner[cvr];

        var meteringPointOwner = await clientFactoryChange.CreateClient().GetUuidForCompany(cvr);

        if (!string.IsNullOrWhiteSpace(meteringPointOwner))
            cvrToMeteringPointOwner.TryAdd(cvr, meteringPointOwner);

        return meteringPointOwner;
    }
}
