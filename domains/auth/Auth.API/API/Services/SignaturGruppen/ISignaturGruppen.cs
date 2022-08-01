using API.Models;

namespace API.Services;
public interface ISignaturGruppen
{
    LoginResponse CreateRedirecthUrl(string feUrl, string returnUrl);
}
