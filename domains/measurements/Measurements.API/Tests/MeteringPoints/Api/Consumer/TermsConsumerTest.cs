using System;
using System.Linq;
using System.Threading.Tasks;
using API;
using API.MeteringPoints.Api;
using API.MeteringPoints.Api.Consumer;
using API.MeteringPoints.Api.Models;
using Contracts;
using EnergyOrigin.IntegrationEvents.Events.Terms.V2;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Relation.V1;
using Tests.Extensions;
using Tests.TestContainers;
using Xunit;

namespace Tests.MeteringPoints.Api.Consumer;

public class TermsConsumerTest : IClassFixture<CustomWebApplicationFactory<Program>>, IClassFixture<PostgresContainer>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public TermsConsumerTest(CustomWebApplicationFactory<Program> factory, PostgresContainer postgresContainer)
    {
        factory.ConnectionString = postgresContainer.ConnectionString;
        _factory = factory;
    }


    [Fact]
    public async Task when_datahub_relation_is_created_relationstatus_is_created()
    {
        var relationMock = new CreateRelationResponse() { ErrorMessage = "", Success = true };
        var @event = new OrgAcceptedTerms(Guid.NewGuid(), Guid.NewGuid().ToString(), DateTimeOffset.UtcNow, Guid.NewGuid(), "22222222", Guid.NewGuid());
        var contextMock = Substitute.For<ConsumeContext<OrgAcceptedTerms>>();
        contextMock.Message.Returns(@event);

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_factory.ConnectionString)
            .Options;
        var dbContext = new ApplicationDbContext(contextOptions);
        dbContext.Database.EnsureCreated();

        var clientMock = Substitute.For<Relation.V1.Relation.RelationClient>();
        clientMock.CreateRelationAsync(Arg.Any<CreateRelationRequest>()).Returns(relationMock);

        var consumer = new TermsConsumer(dbContext, clientMock);
        await consumer.Consume(contextMock);
        var relation = await dbContext.Relations.SingleOrDefaultAsync(x => x.SubjectId == @event.SubjectId);
        relation!.Status.Should().Be(RelationStatus.Created);
    }

    [Fact]
    public async Task when_datahub_relation_is_already_excisting_no_exception()
    {
        var relationMock = new CreateRelationResponse() { ErrorMessage = "", Success = true };
        var @event = new OrgAcceptedTerms(Guid.NewGuid(), Guid.NewGuid().ToString(), DateTimeOffset.UtcNow, Guid.NewGuid(), "22222222", Guid.NewGuid());
        var contextMock = Substitute.For<ConsumeContext<OrgAcceptedTerms>>();
        contextMock.Message.Returns(@event);

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_factory.ConnectionString)
            .Options;
        var dbContext = new ApplicationDbContext(contextOptions);
        dbContext.Database.EnsureCreated();

        var clientMock = Substitute.For<Relation.V1.Relation.RelationClient>();
        clientMock.CreateRelationAsync(Arg.Any<CreateRelationRequest>()).Returns(relationMock);

        var consumer = new TermsConsumer(dbContext, clientMock);
        await consumer.Consume(contextMock);
        await consumer.Consume(contextMock);

        var relation = await dbContext.Relations.SingleOrDefaultAsync(x => x.SubjectId == @event.SubjectId);
        relation!.Status.Should().Be(RelationStatus.Created);
    }

    [Fact]
    public async Task when_datahub_relation_is_not_created_relationstatus_is_pending()
    {
        var relationMock = new CreateRelationResponse() { ErrorMessage = "Error", Success = false };
        var @event = new OrgAcceptedTerms(Guid.NewGuid(), Guid.NewGuid().ToString(), DateTimeOffset.UtcNow, Guid.NewGuid(), "22222222", Guid.NewGuid());
        var contextMock = Substitute.For<ConsumeContext<OrgAcceptedTerms>>();
        contextMock.Message.Returns(@event);

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_factory.ConnectionString)
            .Options;
        var dbContext = new ApplicationDbContext(contextOptions);
        dbContext.Database.EnsureCreated();

        var clientMock = Substitute.For<Relation.V1.Relation.RelationClient>();
        clientMock.CreateRelationAsync(Arg.Any<CreateRelationRequest>()).Returns(relationMock);

        var consumer = new TermsConsumer(dbContext, clientMock);
        await consumer.Consume(contextMock);
        var relation = await dbContext.Relations.SingleOrDefaultAsync(x => x.SubjectId == @event.SubjectId);
        relation!.Status.Should().Be(RelationStatus.Pending);
    }
}

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    public string ConnectionString { get; set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
    }
}
