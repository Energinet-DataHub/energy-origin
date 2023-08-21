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
                metrics.ResetCertificatesTransferred();
                var rand = new Random();
                metrics.AddCertificatesTransferred(rand.Next(0, 100));
                metrics.SetNumberOfTransferAgreementsOnLastRun(rand.Next(0, 100));
                metrics.AddTransferAttempt("SomeReg", certId1);
                metrics.AddTransferAttempt("SomeReg", certId2);

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
