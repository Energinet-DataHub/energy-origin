using System;

namespace TransferAgreementAutomation.Worker;

[Serializable]
public class TransferCertificatesException : Exception
{
    public TransferCertificatesException(string message, Exception ex) : base(message, ex)
    {
    }
    public TransferCertificatesException(string message) : base(message, null)
    {
    }
}

//only modify this
