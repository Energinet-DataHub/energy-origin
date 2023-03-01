using System.Threading.Tasks;
using API.GranularCertificateIssuer.Repositories;
using Contracts.Transfer;
using MassTransit;
using Microsoft.Extensions.Logging;
using static MassTransit.ValidationResultExtensions;

namespace API.TransferCertificateService;

public class TransferCertificateConsumer : IConsumer<TransferProductionCertificateRequest>
{
    private readonly ILogger<TransferCertificateConsumer> logger;
    private readonly IProductionCertificateRepository repository;

    public TransferCertificateConsumer(ILogger<TransferCertificateConsumer> logger, IProductionCertificateRepository repository)
    {
        this.logger = logger;
        this.repository = repository;
    }

    public async Task Consume(ConsumeContext<TransferProductionCertificateRequest> context)
    {
        logger.LogInformation($"Current owner: {context.Message.CurrentOwner} \nl" +
                              $"New owner: {context.Message.NewOwner} \nl" +
                              $"CertificateID: {context.Message.CertificateId}");

        var productionCertificate = await repository.Get(context.Message.CertificateId, cancellationToken: context.CancellationToken);

        if (productionCertificate == null)
        {
            await context.RespondAsync(
                new TransferProductionCertificateFailureResponse(
                    $"No production certificate by Id {context.Message.CertificateId}"));
            return;
        }

        productionCertificate.Transfer(context.Message.CurrentOwner, context.Message.NewOwner);

        await context.RespondAsync(new TransferProductionCertificateResponse("todo"));
    }
}
