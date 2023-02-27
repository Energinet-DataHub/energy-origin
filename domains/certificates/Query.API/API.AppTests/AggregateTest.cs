using System.Threading.Tasks;
using API.GranularCertificateIssuer;
using CertificateEvents.Primitives;
using FluentAssertions;
using Marten;
using Marten.Exceptions;
using Xunit;

namespace API.AppTests;

public class AggregateTest
{
    [Fact]
    public async Task Bla()
    {
        var documentStore = DocumentStore.For("host=localhost;Port=5432;Database=marten;username=postgres;password=postgres;");

        var repo = new AggregateRepository(documentStore);

        var aggregate = new ProductionCertificate("gridArea", new Period(1, 42), new Technology("f00", "t00"),
            "owner1", "gsrn", 42);

        aggregate.Issue();
        aggregate.Transfer("owner1", "owner2");

        await repo.Save(aggregate);

        var aggregateFromRepo = await repo.Get<ProductionCertificate>(aggregate.Id);

        aggregateFromRepo.Transfer("owner2", "owner3");

        aggregateFromRepo.CertificateOwner.Should().Be("owner3");
    }

    [Fact]
    public async Task ConcurrencyTest()
    {
        var documentStore = DocumentStore.For("host=localhost;Port=5432;Database=marten;username=postgres;password=postgres;");

        var repo = new AggregateRepository(documentStore);

        var aggregate = new ProductionCertificate("gridArea", new Period(1, 42), new Technology("f00", "t00"),
            "owner1", "gsrn", 42);

        await repo.Save(aggregate);

        var aggregateFromRepo1 = await repo.Get<ProductionCertificate>(aggregate.Id);
        var aggregateFromRepo2 = await repo.Get<ProductionCertificate>(aggregate.Id);

        aggregateFromRepo1.Issue();
        aggregateFromRepo2.Reject("foo");

        var storeAction1 = () => repo.Save(aggregateFromRepo1);
        var storeAction2 = () => repo.Save(aggregateFromRepo2);

        await storeAction1.Should().NotThrowAsync();
        await storeAction2.Should().ThrowAsync<EventStreamUnexpectedMaxEventIdException>();
    }
}
