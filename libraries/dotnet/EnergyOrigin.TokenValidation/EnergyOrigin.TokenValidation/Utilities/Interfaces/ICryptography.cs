namespace EnergyOrigin.TokenValidation.Utilities.Interfaces;

public interface ICryptography
{
    string Encrypt<T>(T state);
    T Decrypt<T>(string encryptedState);
}
