namespace API.DataSyncSyncer.Client;

public interface IDataSyncClientFactory
{
    IDataSyncClient CreateClient();
}
