using EnergyOrigin.TokenValidation.Values;

namespace API.Utilities.Interfaces;

public interface IMetrics
{
    void Login(Guid userId, Guid? companyId, ProviderType providerType);
    void Logout(Guid userId, Guid? companyId, ProviderType providerType);
    void TokenRefresh(Guid userId, Guid? companyId, ProviderType providerType);
}
