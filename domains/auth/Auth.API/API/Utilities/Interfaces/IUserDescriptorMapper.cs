using API.Models.Entities;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;

namespace API.Utilities.Interfaces;

public interface IUserDescriptorMapper : IUserDescriptorMapperBase
{
    public UserDescriptor Map(User user, ProviderType providerType, IEnumerable<string> roles, string accessToken, string identityToken);
}
