using System;

namespace ClaimAutomation.Worker.Automation;

public class ClaimCertificatesException : Exception
{
    public ClaimCertificatesException(string message, Exception ex) : base(message, ex)
    {
    }
    public ClaimCertificatesException(string message) : base(message, null)
    {
    }
}
