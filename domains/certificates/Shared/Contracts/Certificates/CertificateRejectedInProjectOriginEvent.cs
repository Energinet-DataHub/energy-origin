using System;

namespace Contracts.Certificates
{
    public record CertificateRejectedInProjectOriginEvent(Guid CertificateId, string Reason);
}
