using System;

namespace DataContext;

[Serializable]
public class CertificateDomainException : Exception
{
    public CertificateDomainException(Guid certificateId, string message) : base(message)
    {
    }

    public CertificateDomainException(string message) : base(message)
    {
    }
}
