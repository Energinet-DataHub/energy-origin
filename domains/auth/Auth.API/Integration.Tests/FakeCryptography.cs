using EnergyOrigin.TokenValidation.Utilities.Interfaces;

namespace Integration.Tests;

public class FakeCryptography : ICryptography
{
    public T Decrypt<T>(string encryptedState)
    {
        return default; // FIXME: CS8603
    }

    public string Encrypt<T>(T state)
    {
        return string.Empty;
    }
}
