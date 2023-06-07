using System.Diagnostics.Metrics;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;

namespace API.Utilities;

public class Metrics : IMetrics
{
    public const string Name = "Auth.API";

    private static readonly Meter meter = new(Name);

    private readonly Counter<int> loginCounter = meter.CreateCounter<int>("user-login");
    private readonly Counter<int> logoutCounter = meter.CreateCounter<int>("user-logout");
    private readonly Counter<int> tokenRefreshCounter = meter.CreateCounter<int>("token-refresh");

    private const string userIdKey = "UserId";
    private const string companyIdKey = "CompanyId";
    private const string identityProviderTypeKey = "IdentityProviderType";

    public void Login(Guid userId, Guid? companyId, ProviderType providerType) =>
        loginCounter.Add(
            1,
            GetKeyValuePair(userIdKey, userId),
            GetKeyValuePair(companyIdKey, companyId),
            GetKeyValuePair(identityProviderTypeKey, providerType)
        );

    public void Logout(Guid userId, Guid? companyId, ProviderType providerType) =>
        logoutCounter.Add(
            1,
            GetKeyValuePair(userIdKey, userId),
            GetKeyValuePair(companyIdKey, companyId),
            GetKeyValuePair(identityProviderTypeKey, providerType)
        );

    public void TokenRefresh(Guid userId, Guid? companyId, ProviderType providerType) =>
        tokenRefreshCounter.Add(
            1,
            GetKeyValuePair(userIdKey, userId),
            GetKeyValuePair(companyIdKey, companyId),
            GetKeyValuePair(identityProviderTypeKey, providerType)
        );

    private static KeyValuePair<string, object?> GetKeyValuePair(string key, object? value) => new(key, value);
}
