using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client;
using ProjectOrigin.Electricity.Client.Models;
using Xunit;

namespace API.IntegrationTests;

public sealed class RegistryTests :
    TestBase
{
    private readonly RegisterClient client;
    private readonly Key issuerKey;

    public RegistryTests()
    {
        // Connect to Registry running in Docker Compose setup
        client = new RegisterClient("http://localhost:8765");
        issuerKey = Key.Import(SignatureAlgorithm.Ed25519, Convert.FromBase64String("LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1DNENBUUF3QlFZREsyVndCQ0lFSUJhb2FjVHVWL05ub3ROQTBlVzJxbFJZZ3Q2WTRsaWlXSzV5VDRFZ3JKR20KLS0tLS1FTkQgUFJJVkFURSBLRVktLS0tLQo="), KeyBlobFormat.PkixPrivateKeyText);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(42)]
    public async Task can_handle_many_requests(int operationCount)
    {
        var events = new ConcurrentBag<CommandStatusEvent>();

        client.Events += events.Add;

        var tasks = Enumerable.Range(1, operationCount)
            .Select(_ => Execute());

        await Task.WhenAll(tasks);

        while (events.Count < operationCount)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        var failedEvents = events.Where(cse => cse.State == CommandState.Failed).ToArray();
        failedEvents.Should().BeEmpty();
    }

    private Task Execute() =>
        new ElectricityCommandBuilder()
            .IssueProductionCertificate(
                id: new FederatedCertifcateId(
                    "RegistryA",
                    Guid.NewGuid()),
                inteval: new DateInterval(DateTimeOffset.UtcNow, TimeSpan.FromHours(1)),
                gridArea: "DK1",
                gsrn: 42,
                quantity: new ShieldedValue(42),
                owner: Key.Create(SignatureAlgorithm.Ed25519).PublicKey,
                issuingBodySigner: issuerKey,
                fuelCode: "FuelCode",
                techCode: "TechCode")
            .Execute(client);
}
