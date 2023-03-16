using System.Security.Claims;
using API.Models.Entities;

namespace API.Utilities;

public interface IClaimsWrapperMapper
{
    public ClaimsWrapper Map(User user, string accessToken, string identityToken);
    public ClaimsWrapper? Map(ClaimsPrincipal? user);
}
