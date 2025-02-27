using System;
using System.Threading.Tasks;
using API;
using API.MeteringPoints.Api;
using API.MeteringPoints.Api.Consumer;
using API.MeteringPoints.Api.Models;
using EnergyOrigin.IntegrationEvents.Events.Terms.V2;
using EnergyOrigin.Setup.Migrations;
using EnergyTrackAndTrace.Testing.Testcontainers;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Relation.V1;
using Tests.Extensions;
using Xunit;

namespace Tests.MeteringPoints.Api.Consumer;

public class TermsConsumerTest : IClassFixture<PostgresContainer>
{
    private readonly DatabaseInfo _databaseInfo;

    public TermsConsumerTest(PostgresContainer postgresContainer)
    {
        _databaseInfo = postgresContainer.CreateNewDatabase().GetAwaiter().GetResult();
        new DbMigrator(_databaseInfo.ConnectionString, typeof(Startup).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync().Wait();
    }

    [Fact]
    public async Task GivenTermsAcceptedEvent_WhenDataHubRelationIsCreated_RelationStatusIsCreated()
    {
        var relationMock = new CreateRelationResponse() { ErrorMessage = "", Success = true };
        var @event = new OrgAcceptedTerms(Guid.NewGuid(), Guid.NewGuid().ToString(), DateTimeOffset.UtcNow, Guid.NewGuid(), "22222222",
            Guid.NewGuid());
        var contextMock = Substitute.For<ConsumeContext<OrgAcceptedTerms>>();
        contextMock.Message.Returns(@event);

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_databaseInfo.ConnectionString).Options;
        var dbContext = new ApplicationDbContext(contextOptions);

        var clientMock = Substitute.For<Relation.V1.Relation.RelationClient>();
        clientMock.CreateRelationAsync(Arg.Any<CreateRelationRequest>()).Returns(relationMock);

        var consumer = new TermsConsumer(dbContext, clientMock);
        await consumer.Consume(contextMock);
        var relation = await dbContext.Relations.SingleOrDefaultAsync(x => x.SubjectId == @event.SubjectId);
        relation!.Status.Should().Be(RelationStatus.Created);
    }

    [Fact]
    public async Task GivenTermsAcceptedEvent_WhenRelationAlreadyExists_NoExceptionIsThrown()
    {
        var relationMock = new CreateRelationResponse() { ErrorMessage = "", Success = true };
        var @event = new OrgAcceptedTerms(Guid.NewGuid(), Guid.NewGuid().ToString(), DateTimeOffset.UtcNow, Guid.NewGuid(), "22222222",
            Guid.NewGuid());
        var contextMock = Substitute.For<ConsumeContext<OrgAcceptedTerms>>();
        contextMock.Message.Returns(@event);

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_databaseInfo.ConnectionString).Options;
        var dbContext = new ApplicationDbContext(contextOptions);

        var clientMock = Substitute.For<Relation.V1.Relation.RelationClient>();
        clientMock.CreateRelationAsync(Arg.Any<CreateRelationRequest>()).Returns(relationMock);

        var consumer = new TermsConsumer(dbContext, clientMock);
        await consumer.Consume(contextMock);
        await consumer.Consume(contextMock);

        var relation = await dbContext.Relations.SingleOrDefaultAsync(x => x.SubjectId == @event.SubjectId);
        relation!.Status.Should().Be(RelationStatus.Created);
    }

    [Fact]
    public async Task GivenTermsAcceptedEvent_WhenDataHubRelationIsNotCreated_RelationStatusIsPending()
    {
        var relationMock = new CreateRelationResponse() { ErrorMessage = "Error", Success = false };
        var @event = new OrgAcceptedTerms(Guid.NewGuid(), Guid.NewGuid().ToString(), DateTimeOffset.UtcNow, Guid.NewGuid(), "22222222",
            Guid.NewGuid());
        var contextMock = Substitute.For<ConsumeContext<OrgAcceptedTerms>>();
        contextMock.Message.Returns(@event);

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_databaseInfo.ConnectionString).Options;
        var dbContext = new ApplicationDbContext(contextOptions);

        var clientMock = Substitute.For<Relation.V1.Relation.RelationClient>();
        clientMock.CreateRelationAsync(Arg.Any<CreateRelationRequest>()).Returns(relationMock);

        var consumer = new TermsConsumer(dbContext, clientMock);
        await consumer.Consume(contextMock);
        var relation = await dbContext.Relations.SingleOrDefaultAsync(x => x.SubjectId == @event.SubjectId);
        relation!.Status.Should().Be(RelationStatus.Pending);
    }
}
