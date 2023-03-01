namespace Contracts.Transfer;

public abstract record TransferProductionCertificateResponse
{
    public record Success : TransferProductionCertificateResponse;

    public record Failure(string Reason) : TransferProductionCertificateResponse;

    private TransferProductionCertificateResponse()
    {
    }
}
