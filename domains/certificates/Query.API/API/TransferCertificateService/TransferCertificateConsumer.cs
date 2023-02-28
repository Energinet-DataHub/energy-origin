using System.Threading.Tasks;
using Contracts.Transfer;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.TransferCertificateService;

public class TransferCertificateConsumer : IConsumer<TransferProductionCertificateRequest>
{
    private readonly ILogger<TransferCertificateConsumer> logger;

    public TransferCertificateConsumer(ILogger<TransferCertificateConsumer> logger)
    {
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<TransferProductionCertificateRequest> context)
    {
        logger.LogInformation($"Current owner: {context.Message.CurrentOwner} \nl" +
                              $"New owner: {context.Message.NewOwner} \nl" +
                              $"CertificateID: {context.Message.CertificateId}");

        await context.RespondAsync(new TransferProductionCertificateResponse(Status: "OK"));
    }

}
