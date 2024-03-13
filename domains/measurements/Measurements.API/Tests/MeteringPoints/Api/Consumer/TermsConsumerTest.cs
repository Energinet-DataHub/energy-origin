using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API;
using API.MeteringPoints.Api;
using API.MeteringPoints.Api.Consumer;
using API.MeteringPoints.Api.Models;
using EnergyOrigin.IntegrationEvents.Events.Terms.V1;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Relation.V1;
using Tests.Extensions;
using Tests.Fixtures;
using Tests.TestContainers;
using Xunit;

namespace Tests.MeteringPoints.Api.Consumer;

public class TermsConsumerTest : MeasurementsTestBase, IDisposable
{
    public TermsConsumerTest(TestServerFixture<Startup> serverFixture, PostgresContainer dbContainer,
        RabbitMqContainer rabbitMqContainer) : base(serverFixture, dbContainer, rabbitMqContainer, null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbContainer.ConnectionString)
            .Options;
        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _serverFixture.RefreshHostAndGrpcChannelOnNextClient();
    }

    [Fact]
    public async Task when_accepted_terms_relation_is_pending()
    {
        var @event = new OrgAcceptedTerms(Guid.NewGuid(), "11111111", Guid.NewGuid());
        var relationMock = new CreateRelationResponse() { ErrorMessage = "Error", Success = false };


        var clientMock = Substitute.For<Relation.V1.Relation.RelationClient>();
        clientMock.CreateRelationAsync(Arg.Any<CreateRelationRequest>()).Returns(relationMock);

        _serverFixture.ConfigureTestServices += services =>
        {
            var mpClient = services.Single(d =>
                d.ServiceType == typeof(Relation.V1.Relation.RelationClient));
            services.Remove(mpClient);
            services.AddSingleton(clientMock);
        };

        using var scope = _serverFixture.ServiceScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(@event);

        var relation = await dbContext.Relations.FirstOrDefaultAsync(it => it.SubjectId == @event.SubjectId);
        relation!.Status.Should().Be(RelationStatus.Pending);
    }

    [Fact]
    public async Task when_datahub_relation_is_created_relationstatus_is_created()
    {
        var relationMock = new CreateRelationResponse() { ErrorMessage = "", Success = true };


        var clientMock = Substitute.For<Relation.V1.Relation.RelationClient>();
        clientMock.CreateRelationAsync(Arg.Any<CreateRelationRequest>()).Returns(relationMock);

        using var scope = _serverFixture.ServiceScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var @event = new OrgAcceptedTerms(Guid.NewGuid(), "22222222", Guid.NewGuid());

        await scope.ServiceProvider.GetRequiredService<IBus>().Publish(@event);
        await Task.Delay(10000);

        var relation = await dbContext.RepeatedlyFirstOrDefaultAsyncUntil<RelationDto>(it => it.Status == RelationStatus.Created)!;

        relation.Status.Should().Be(RelationStatus.Created);
    }
}
