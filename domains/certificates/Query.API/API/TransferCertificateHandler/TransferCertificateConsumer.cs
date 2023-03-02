using System.Threading.Tasks;
using API.GranularCertificateIssuer.Repositories;
using CertificateEvents.Exceptions;
using Contracts.Transfer;
using MassTransit;
using static Contracts.Transfer.TransferProductionCertificateResponse;

namespace API.TransferCertificateHandler;

public class TransferCertificateConsumer : IConsumer<TransferProductionCertificateRequest>
{
    private readonly IProductionCertificateRepository repository;

    public TransferCertificateConsumer(IProductionCertificateRepository repository) => this.repository = repository;

    public async Task Consume(ConsumeContext<TransferProductionCertificateRequest> context)
    {
        var productionCertificate = await repository.Get(context.Message.CertificateId, cancellationToken: context.CancellationToken);

        if (productionCertificate == null)
        {
            await context.RespondAsync(new Failure($"No production certificate by Id {context.Message.CertificateId}"));
            return;
        }

        try
        {
            productionCertificate.Transfer(context.Message.CurrentOwner, context.Message.NewOwner);

            await repository.Save(productionCertificate, context.CancellationToken);

            await context.RespondAsync(new Success());
        }
        catch (CertificateDomainException e)
        {
            await context.RespondAsync(new Failure(e.Message));
        }
    }
}
