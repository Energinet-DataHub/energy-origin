using API.Models;

namespace API.Services;
public interface IOidcProviders
{
    NextStep CreateRedirecthUrl(AuthState state);
}
