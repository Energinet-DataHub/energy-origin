using System.Threading.Tasks;
using CertificateEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.TransferCertificateService;

public class TransferCertificateConsumer : IConsumer<TransferProductionCertificate>
{
    private readonly ILogger<TransferCertificateConsumer> logger;

    public TransferCertificateConsumer(ILogger<TransferCertificateConsumer> logger)
    {
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<TransferProductionCertificate> context)
    {
        logger.LogInformation($"Current owner: {context.Message.CurrentOwner} \nl" +
                              $"New owner: {context.Message.NewOwner} \nl" +
                              $"CertificateID: {context.Message.CertificateId}");

        await context.RespondAsync(new { Message = "OK" });
    }

}
