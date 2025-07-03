using DataContext.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TransferAgreementAutomation.Worker.Service;
using Xunit;

namespace Worker.UnitTests;

public class TransferEngineCoordinatorTests
{
    [Fact]
    public async Task TransferCertificate_Always_Calls_SetTrial()
    {
        var transferAgreement = new TransferAgreement();
        var fakeEngine = Substitute.For<ITransferEngine>();
        var fakeLogger = Substitute.For<ILogger<TransferEngineCoordinator>>();

        fakeEngine.IsSupported(Arg.Any<TransferAgreement>()).Returns(true);
        fakeEngine.TransferCertificates(Arg.Any<TransferAgreement>(), CancellationToken.None).Returns(Task.CompletedTask);

        var coordinator = new TransferEngineCoordinator([fakeEngine], fakeLogger);

        await coordinator.TransferCertificate(transferAgreement, CancellationToken.None);

        fakeEngine.Received(1).SetEngineTrialState(Arg.Any<TransferAgreement>());
    }
}
