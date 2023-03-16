using System.Security.Claims;
using API.Models.Entities;
using AuthLibrary.Utilities;

namespace API.Utilities;

public interface IUserDescriptMapper : IUserDescriptMapperBase
{
    public UserDescriptor Map(User user, string accessToken, string identityToken);
}
