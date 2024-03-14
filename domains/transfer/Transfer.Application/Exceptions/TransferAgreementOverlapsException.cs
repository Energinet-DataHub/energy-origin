using System.Runtime.Serialization;

namespace Transfer.Application.Exceptions;

public class TransferAgreementOverlapsException : ValidationException
{
    public TransferAgreementOverlapsException() : base("There is already a Transfer Agreement with this company tin within the selected date range")
    {
    }

    protected TransferAgreementOverlapsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public TransferAgreementOverlapsException(string? message) : base(message)
    {
    }

    public TransferAgreementOverlapsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
