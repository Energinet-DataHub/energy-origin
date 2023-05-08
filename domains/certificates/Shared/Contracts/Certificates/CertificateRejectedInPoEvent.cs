using System;

namespace Contracts.Certificates
{
    public record CertificateRejectedInPoEvent(Guid CertificateId, string Reason);
}
