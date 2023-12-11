using System;
using Google.Protobuf;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProjectOrigin.Common.V1;
using ProjectOrigin.Registry.V1;
using RegistryConnector.Worker.RoutingSlips;
using Xunit;

namespace RegistryConnector.Worker.UnitTests.RoutingSlips;

public class IssueToRegistryActivityTests
{
    [Fact]
    public void ShouldIssueToRegistry()
    {
        var clientMock = Substitute.For<RegistryService.RegistryServiceClient>();
        var loggerMock = Substitute.For<ILogger<IssueToRegistryActivity>>();
        var contextMock = Substitute.For<ExecuteContext<IssueToRegistryArguments>>();

        contextMock.Arguments.Returns(new IssueToRegistryArguments(new Transaction
            {
                Header = new TransactionHeader
                {
                    FederatedStreamId = new FederatedStreamId
                    {
                        Registry = "SomeRegistry",
                        StreamId = new Uuid
                        {
                            Value = Guid.NewGuid().ToString()
                        }
                    },
                    Nonce = "Dunno what this is",
                    PayloadSha512 = ByteString.CopyFrom(new ReadOnlySpan<byte>()),
                    PayloadType = "Dunno"
                },
                HeaderSignature = ByteString.CopyFrom(new ReadOnlySpan<byte>()),
                Payload = ByteString.CopyFrom(new ReadOnlySpan<byte>())
            },
            Guid.NewGuid()));

        var slip = new IssueToRegistryActivity(clientMock, loggerMock);

        slip.Execute(contextMock);

        clientMock.Received(1).SendTransactionsAsync(Arg.Any<SendTransactionsRequest>());
    }
}
