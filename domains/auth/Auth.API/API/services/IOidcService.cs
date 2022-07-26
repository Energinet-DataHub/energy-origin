using API.Models;

namespace API.Services;

public interface IOidcService
{
    Task<LoginResponse> Login(string FeUrl, string ReturnUrl);
}
