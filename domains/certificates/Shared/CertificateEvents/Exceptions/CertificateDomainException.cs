using System;
using System.Runtime.Serialization;

namespace CertificateEvents.Exceptions;

[Serializable]
public class CertificateDomainException : Exception
{
    public CertificateDomainException(Guid certificateId, string message) : base(message)
    {
    }

    protected CertificateDomainException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
