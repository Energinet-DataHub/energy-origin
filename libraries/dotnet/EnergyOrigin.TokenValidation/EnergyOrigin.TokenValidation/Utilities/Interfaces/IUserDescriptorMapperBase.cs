using System.Security.Claims;

namespace EnergyOrigin.TokenValidation.Utilities.Interfaces;

public interface IUserDescriptorMapperBase
{
    public UserDescriptor? Map(ClaimsPrincipal? user);
}
