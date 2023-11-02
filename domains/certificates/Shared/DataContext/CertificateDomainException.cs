using System;
using System.Runtime.Serialization;

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

    protected CertificateDomainException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
