using System;
using System.Threading;
using System.Threading.Tasks;
using API.Metrics;
using Microsoft.Extensions.Hosting;

namespace API.BackgroundServices
{
    public class DummyMetricsWorker : BackgroundService
    {
        private readonly ITransferAgreementAutomationMetrics metrics;

        public DummyMetricsWorker(ITransferAgreementAutomationMetrics metrics)
        {
            this.metrics = metrics;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var certId1 = Guid.NewGuid();
            var certId2 = Guid.NewGuid();
            while (!stoppingToken.IsCancellationRequested)
            {
                var rand = new Random();
                metrics.SetErrorsOnLastRun(rand.Next(0, 100));
                metrics.SetCertificatesTransferredOnLastRun(rand.Next(0, 100));
                metrics.SetNumberOfTransferAgreementsOnLastRun(rand.Next(0, 100));
                metrics.TransferError(certId1);
                metrics.TransferError(certId2);

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
