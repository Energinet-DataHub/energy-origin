using System;
using Contracts.Certificates;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RegistryConnector.Worker.Cache;
using Xunit;

namespace RegistryConnector.Worker.UnitTests;

public class CertificateEventsInMemoryCacheTests
{
    private readonly Mock<ILogger<CertificateEventsInMemoryCache>> fakeLogger = new();

    [Fact]
    public void AddCertificateWithCommandId_ExpectMsgInCache()
    {
        var cache = new CertificateEventsInMemoryCache(fakeLogger.Object);
        var commandId = Some.CommandId;
        var msg = Some.ProductionCertificateCreatedEvent;
        var wrappedMsg = new MessageWrapper<ProductionCertificateCreatedEvent>(msg, Guid.NewGuid(), Guid.NewGuid());

        cache.AddCertificateWithCommandId(commandId, wrappedMsg);

        var stored = cache.PopCertificateWithCommandId(commandId);

        wrappedMsg.Should().Be(stored);
    }

    [Fact]
    public void PopCertificateWithCommandId_WhenFound_RemovesCertificateWithCommandId()
    {
        var cache = new CertificateEventsInMemoryCache(fakeLogger.Object);
        var commandId = Some.CommandId;
        var msg = Some.ProductionCertificateCreatedEvent;
        var wrappedMsg = new MessageWrapper<ProductionCertificateCreatedEvent>(msg, Guid.NewGuid(), Guid.NewGuid());

        cache.AddCertificateWithCommandId(commandId, wrappedMsg);

        var stored1 = cache.PopCertificateWithCommandId(commandId);
        var stored2 = cache.PopCertificateWithCommandId(commandId);

        wrappedMsg.Should().Be(stored1);
        stored2.Should().BeNull();
    }
}