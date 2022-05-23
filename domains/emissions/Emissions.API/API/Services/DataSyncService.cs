namespace API.Services;

public class DataSyncService : IDataSyncService
{
    readonly HttpClient _httpClient;
    
    public DataSyncService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
}