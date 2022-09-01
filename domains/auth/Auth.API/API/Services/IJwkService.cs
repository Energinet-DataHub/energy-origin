using Jose;

namespace API.Services
{
    public interface IJwkService
    {
        Task<Jwk> GetJwkAsync();
    }
}
