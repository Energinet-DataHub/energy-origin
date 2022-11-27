namespace API.MasterDataService.MockInput;

public class MockMasterDataOptions
{
    public static string Prefix = "MockMasterData";

    public string JsonFilePath { get; init; } = string.Empty;
    public string AuthServiceUrl { get; init; } = string.Empty;
}
