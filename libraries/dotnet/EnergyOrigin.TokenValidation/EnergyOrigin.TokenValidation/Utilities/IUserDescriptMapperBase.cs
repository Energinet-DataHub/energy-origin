using System.Security.Claims;

namespace EnergyOrigin.TokenValidation.Utilities;

public interface IUserDescriptMapperBase
{
    public UserDescriptor? Map(ClaimsPrincipal? user);
}
