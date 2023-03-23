using System.Security.Claims;
using API.Models.Entities;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;

namespace API.Utilities.Interfaces;

public interface IUserDescriptorMapper
{
    public UserDescriptor Map(User user, ProviderType providerType, string accessToken, string identityToken);
    public UserDescriptor? Map(ClaimsPrincipal? user);
}
