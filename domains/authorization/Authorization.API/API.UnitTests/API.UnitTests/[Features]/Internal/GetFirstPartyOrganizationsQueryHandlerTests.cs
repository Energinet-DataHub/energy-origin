using API.Authorization._Features_.Internal;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;

namespace API.UnitTests._Features_.Internal;

public class GetFirstPartyOrganizationsQueryTests
{
    private readonly FakeOrganizationRepository _fakeOrganizationRepository;
    private readonly GetFirstPartyOrganizationsQueryHandler _handler;

    public GetFirstPartyOrganizationsQueryTests()
    {
        _fakeOrganizationRepository = new FakeOrganizationRepository();
        _handler = new GetFirstPartyOrganizationsQueryHandler(_fakeOrganizationRepository);
    }

    [Fact]
    public async Task GivenDatabaseContainsOrganizations_WhenQuerying_ThenReturnListOfAllOrganizations()
    {
        var query = new GetFirstPartyOrganizationsQuery();

        var normalOrganization = Any.Organization(Tin.Create("12345678"), OrganizationName.Create("Brian Bolighaj from Bolighaj A/S"));
        var trialOrganization = Any.TrialOrganization(Tin.Create("13371337"), OrganizationName.Create("Thomas Trial from TrialCorp"));
        var deactivatedOrganization = Any.DeactivatedOrganization(Tin.Create("90009000"), OrganizationName.Create("Deactivated Dario from Dario ApS"));

        await _fakeOrganizationRepository.AddAsync(normalOrganization, CancellationToken.None);
        await _fakeOrganizationRepository.AddAsync(trialOrganization, CancellationToken.None);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal("Normal", normalOrganization.Status.ToString());
        Assert.Equal("Trial", trialOrganization.Status.ToString());
        Assert.Equal("Deactivated", deactivatedOrganization.Status.ToString());


        Assert.Equal(new List<GetFirstPartyOrganizationsQueryResultItem>
        {
            new(normalOrganization.Id, normalOrganization.Name.Value, normalOrganization.Tin!.Value, normalOrganization.Status.ToString()),
            new(trialOrganization.Id, trialOrganization.Name.Value, trialOrganization.Tin!.Value, trialOrganization.Status.ToString())
        }, result.Result);
    }

    [Fact]
    public async Task GivenDatabaseContainsMultipleOrganizations_But_OnlyASingleOneWithATin_WhenQuerying_ThenResultContainsOnlyOrganizationWithTin()
    {
        var query = new GetFirstPartyOrganizationsQuery();

        var firstPartyOrganization = Any.Organization(Tin.Create("12345678"), OrganizationName.Create("Brian Bolighaj A/S"));
        var thirdPartyOrganization = Any.OrganizationWithClient(Tin.Empty());

        await _fakeOrganizationRepository.AddAsync(firstPartyOrganization, CancellationToken.None);
        await _fakeOrganizationRepository.AddAsync(thirdPartyOrganization, CancellationToken.None);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(new List<GetFirstPartyOrganizationsQueryResultItem>
        {
            new(firstPartyOrganization.Id, firstPartyOrganization.Name.Value, firstPartyOrganization.Tin!.Value, firstPartyOrganization.Status.ToString())
        }, result.Result);
    }

    [Fact]
    public async Task GivenNoOrganizations_WhenQuerying_ThenReturnEmptyList()
    {
        var query = new GetFirstPartyOrganizationsQuery();

        var queryProcess = await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(queryProcess.Result);
    }
}
