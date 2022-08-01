using API.Models;

namespace API.Services;
public interface ISignaturGruppen
{
    Task<LoginResponse> CreateRedirecthUrl(string feUrl, string returnUrl);
}
