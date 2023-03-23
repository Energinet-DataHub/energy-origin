using System.Security.Claims;
using API.Models.Entities;
using API.Values;

namespace API.Utilities.Interfaces;

public interface IUserDescriptorMapper
{
    public UserDescriptor Map(User user, ProviderType providerType, string accessToken, string identityToken);
    public UserDescriptor? Map(ClaimsPrincipal? user);
}
