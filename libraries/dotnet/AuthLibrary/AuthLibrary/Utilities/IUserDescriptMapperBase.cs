using System.Security.Claims;

namespace AuthLibrary.Utilities;

public interface IUserDescriptMapperBase
{
    public UserDescriptor? Map(ClaimsPrincipal? user);
}
