using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;
using Microsoft.Extensions.Logging;

namespace TransferAgreementAutomation.Worker.Service;

public interface ITransferEngineCoordinator
{
    Task TransferCertificate(TransferAgreement transferAgreement, CancellationToken cancellationToken);
}

public class TransferEngineCoordinator : ITransferEngineCoordinator
{
    private readonly IEnumerable<ITransferEngine> _engines;
    private readonly ILogger<TransferEngineCoordinator> _logger;

    public TransferEngineCoordinator(IEnumerable<ITransferEngine> engines, ILogger<TransferEngineCoordinator> logger)
    {
        _engines = engines;
        _logger = logger;
    }

    public async Task TransferCertificate(TransferAgreement transferAgreement, CancellationToken cancellationToken)
    {
        foreach (var engine in _engines)
        {
            if (engine.IsSupported(transferAgreement))
            {
                _logger.LogInformation("Handling transfer agreement with id {Id}", transferAgreement.Id);
                await engine.TransferCertificates(transferAgreement, cancellationToken);
                return;
            }
        }

        _logger.LogError("Skipping transfer agreement with id {Id} and type {Type}, because no handler was found", transferAgreement.Id,
            transferAgreement.Type);
    }
}
