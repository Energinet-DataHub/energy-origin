using System.Threading.Tasks;
using API.GranularCertificateIssuer;
using CertificateEvents.Primitives;
using FluentAssertions;
using Marten;
using Xunit;

namespace API.AppTests;

public class AggregateTest
{
    [Fact]
    public async Task Bla()
    {
        var documentStore = DocumentStore.For("host=localhost;Port=5432;Database=marten;username=postgres;password=postgres;");

        var repo = new AggregateRepository(documentStore);

        var aggregate = new ProductionCertificateAggregate("gridArea", new Period(1, 42), new Technology("f00", "t00"),
            "owner1", "gsrn", 42);

        aggregate.Issue();

        await repo.StoreAsync(aggregate);

        var aggregateFromRepo = await repo.LoadAsync<ProductionCertificateAggregate>(aggregate.Id);

        aggregateFromRepo.IssuedState.Should().Be(IssuedState.Issued);
    }
}
