namespace API.Utilities;

public interface ICryptographyFactory
{
    ICryptography StateCryptography();
    ICryptography IdTokenCryptography();
}
