namespace API.Options;

public class DataSyncOptions
{
    public const string Prefix = "DataSync";

    public Uri Uri { get; init; } = null!;
}
