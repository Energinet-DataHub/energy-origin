using System.Security.Claims;
using API.Models.Entities;

namespace API.Utilities;

public interface IUserDescriptMapper
{
    public UserDescriptor Map(User user, string accessToken, string identityToken, bool encrypt = true);
    public UserDescriptor? Map(ClaimsPrincipal? user);
}
