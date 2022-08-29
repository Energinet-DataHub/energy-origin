namespace API.Services
{
    public interface ITokenStorage
    {
        void DeleteByOpaqueToken(string token);
        string GetIdTokenByOpaqueToken(string token);
    }
}
