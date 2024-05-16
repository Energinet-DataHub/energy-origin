namespace Proxy.Services;

public interface IWalletProxyService
{
    Task<TResponse?> GetAsync<TResponse>(string path, string orgId);
    Task<TResponse?> PostAsync<TResponse, TRequest>(string apiUrl, TRequest data, string orgId);
}
