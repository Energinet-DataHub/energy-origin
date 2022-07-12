using System.Security.Cryptography;

namespace Mock.Oidc;

public static class RSAProvider
{
    private static readonly RSA Rsa = RSA.Create();
    public static RSA RSA => Rsa;
}