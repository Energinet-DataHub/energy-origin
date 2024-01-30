using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace API.IntegrationTests.Testcontainers;

public class TelemetryStack : IAsyncLifetime
{
    private const string CollectorImage = "otel/opentelemetry-collector-contrib:0.91.0";
    private const string PrometheusImage = "prom/prometheus";
    private const int GrpcPort = 4317;
    private const int HttpPort = 4318;
    private const int PrometheusPort = 9090;

    private readonly Lazy<IContainer> collectorContainerLazy;
    private readonly Lazy<IContainer> prometheusContainerLazy;

    private IContainer collectorContainer => collectorContainerLazy.Value;
    private IContainer prometheusContainer => prometheusContainerLazy.Value;

    public string CollectorUri => $"http://{collectorContainer.Hostname}:{GrpcPort}";
    public string PrometheusUri => $"http://{prometheusContainer.Hostname}:{PrometheusPort}";

    public TelemetryStack()
    {
        collectorContainerLazy = new Lazy<IContainer>(() =>
            new ContainerBuilder()
                .WithImage(CollectorImage)
                .WithName("collector")
                .WithCommand("--config=/conf/collector-config.yaml")
                .WithPortBinding(GrpcPort, GrpcPort)
                .WithPortBinding(HttpPort, HttpPort)
                .WithBindMount("./collector-config.yaml", "/conf/collector-config.yaml")
                .Build()
        );

        prometheusContainerLazy = new Lazy<IContainer>(() =>
            new ContainerBuilder()
                .WithImage(PrometheusImage)
                .WithName("prometheus")
                .WithPortBinding(PrometheusPort, PrometheusPort)
                .WithBindMount("./prometheus-config.yaml", "/etc/prometheus/prometheus.yml")
                .Build()
        );
    }

    public virtual async Task InitializeAsync()
    {
        await collectorContainerLazy.Value.StartAsync()
            .ConfigureAwait(false);

        await prometheusContainerLazy.Value.StartAsync()
            .ConfigureAwait(false);
    }

    public virtual async Task DisposeAsync()
    {
        if (collectorContainerLazy.IsValueCreated)
            await prometheusContainerLazy.Value.StopAsync();
        await collectorContainer.StopAsync();
    }
}
