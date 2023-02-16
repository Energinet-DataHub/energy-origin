using System;

namespace API.DemoWorkflow;

public class DemoSendEndpointConfiguration
{
    public string RegistryConnector { get; set; } = "registry-connector-demo";
    public Uri RegistryConnectorQueue => new($"queue:{RegistryConnector}");
}
