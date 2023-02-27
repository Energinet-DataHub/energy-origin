using System;

namespace CertificateEvents.Exceptions;

public class CertificateDomainException : Exception
{
    public CertificateDomainException(Guid certificateId, string message) : base(message)
    {
    }
}
