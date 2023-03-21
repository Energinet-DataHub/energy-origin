using System.Security.Claims;

namespace EnergyOriginTokenValidation.Utilities;

public interface IUserDescriptMapperBase
{
    public UserDescriptor? Map(ClaimsPrincipal? user);
}
